using TodoApi.Data;
using TodoApi.MappingProfiles;
using TodoApi.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoApi.Services;
using TodoApi.DTOs;
using FluentValidation;
using TodoApi.Models;
using Microsoft.AspNetCore.Identity;

namespace TodoApi.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        string? connection = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TodoDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<TodoService>();
        services.AddAutoMapper(mp =>
        {
            mp.AddProfile<TodoProfile>();
        });
        services.AddScoped<IValidator<TodoDTO>, TodoDtoValidator>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddCors(options =>
        {
            options.AddPolicy("Policy", policy =>
            {
                policy.WithOrigins("http://localhost:5109", "https://localhost:7261");
                policy.WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization);
                policy.WithMethods(
                    HttpMethods.Get,
                    HttpMethods.Post,
                    HttpMethods.Put,
                    HttpMethods.Delete);
            });
        });
        services.AddIdentity<ApiUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<TodoDbContext>();

        var jwtIssuer = configuration["JWT:Issuer"];
        var jwtAudience = configuration["JWT:Audience"];
        var jwtKey = configuration["JWT:Key"];

        if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("Ключевые параметры JWT (Issuer, Audience, Key) не настроены в конфигурации");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            };
        });
        services.AddAuthorization();
        services.AddScoped<TokenService>();
        services.AddHttpContextAccessor();
    }
}