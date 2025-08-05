using FluentValidation;
using Microsoft.AspNetCore.Identity;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Extensions;

public static class EndpointExtensions
{
    public static void MapErrorEndpoints(this WebApplication app)
    {
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
    }

    public static void MapAuthEndpoints(this WebApplication app)
    {
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
    }

    public static void MapTodoEndpoints(this WebApplication app)
    {
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
    }
}