using TodoApi.DTOs;
using TodoApi.Services;
using TodoApi.Data;
using Microsoft.EntityFrameworkCore;
using TodoApi.MappingProfiles;
using FluentValidation;
using TodoApi.Validators;
using TodoApi.Middleware;
using Microsoft.Net.Http.Headers;
using TodoApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string? connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TodoDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<TodoService>();
builder.Services.AddAutoMapper(mp =>
{
    mp.AddProfile<TodoProfile>();
});
builder.Services.AddScoped<IValidator<TodoDTO>, TodoDtoValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
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
builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<TodoDbContext>();

var jwtIssuer = builder.Configuration["JWT:Issuer"];
var jwtAudience = builder.Configuration["JWT:Audience"];
var jwtKey = builder.Configuration["JWT:Key"];

if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Ключевые параметры JWT (Issuer, Audience, Key) не настроены в конфигурации");
}

builder.Services.AddAuthentication(options =>
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
builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseCors("Policy");
app.UseAuthentication();
app.UseAuthorization();


if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
}

app.MapGet("/error", (HttpContext context) =>
{
    var exceptionHandlerPathFeature =
        context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

    Console.WriteLine($"Ошибка на пути: {exceptionHandlerPathFeature?.Path}");
    Console.WriteLine($"Сообщение: {exceptionHandlerPathFeature?.Error.Message}");

    return Results.Problem(
        detail: "Произошла непредвиденная ошибка",
        statusCode: StatusCodes.Status500InternalServerError
    );
});

app.MapGet("/", () => "Hello, World! Welcome to TodoApi.");


var authEndpoints = app.MapGroup("/api/auth");

authEndpoints.MapPost("/register", async (RegisterDto registerDTO, UserManager<ApiUser> userManager, TokenService tokenService) =>
{
    var user = new ApiUser() { UserName = registerDTO.Email, Email = registerDTO.Email };
    var result = await userManager.CreateAsync(user, registerDTO.Password);

    if (!result.Succeeded)
    {
        return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
    }

    var token = tokenService.CreateToken(user);
    return Results.Ok(new { Token = token });
});

authEndpoints.MapPost("/login", async (LoginDto loginDto, UserManager<ApiUser> userManager, TokenService tokenService) =>
{
    var user = await userManager.FindByEmailAsync(loginDto.Email);

    if (user == null || !await userManager.CheckPasswordAsync(user, loginDto.Password))
    {
        return Results.Unauthorized();
    }

    var token = tokenService.CreateToken(user);
    return Results.Ok(new { Token = token });
});


var todosEndpoints = app.MapGroup("/api/todos").RequireAuthorization();

todosEndpoints.MapGet("/", async (TodoService todoService) =>
{
    var todos = await todoService.GetAllAsync();
    return Results.Ok(todos);

});

todosEndpoints.MapGet("/{id:int}", async (int id, TodoService todoService) =>
{
    var todo = await todoService.GetAsync(id);

    if (todo is not null)
        return Results.Ok(todo);
    else
        return Results.NotFound();
});

todosEndpoints.MapPost("/", async (TodoDTO todoDTO, TodoService todoService, IValidator<TodoDTO> validator) =>
{
    var validResult = validator.Validate(todoDTO);
    if (!validResult.IsValid)
    {
        return Results.ValidationProblem(validResult.ToDictionary());
    }

    var todo = await todoService.CreateAsync(todoDTO);
    return Results.Created($"/{todo.Id}", todo);
});

todosEndpoints.MapPut("/{id:int}", async (int id, TodoDTO newTodoDTO, TodoService todoService, IValidator<TodoDTO> validator) =>
{
    var todo = await todoService.GetAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    var validResult = validator.Validate(newTodoDTO);
    if (!validResult.IsValid)
    {
        return Results.ValidationProblem(validResult.ToDictionary());
    }

    await todoService.UpdateAsync(id, newTodoDTO);
    return Results.NoContent();
});

todosEndpoints.MapDelete("/{id:int}", async (int id, TodoService todoService) =>
{
    var todo = await todoService.GetAsync(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    await todoService.DeleteAsync(id);
    return Results.NoContent();
});

app.Run();
