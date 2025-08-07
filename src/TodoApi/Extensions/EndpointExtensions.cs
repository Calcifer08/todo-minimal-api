using AutoMapper;
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

        todosEndpoints.MapGet("/", async (TodoService todoService, IMapper mapper) =>
        {
            var todos = await todoService.GetAllAsync();
            var todosViewDTO = mapper.Map<List<TodoViewDto>>(todos);
            return Results.Ok(todosViewDTO);

        });

        todosEndpoints.MapGet("/{id:int}", async (int id, TodoService todoService, IMapper mapper) =>
        {
            var todo = await todoService.GetAsync(id);

            if (todo is null)
                return Results.NotFound();

            var todoViewDto = mapper.Map<TodoViewDto>(todo);
            return Results.Ok(todoViewDto);
        });

        todosEndpoints.MapPost("/", async (TodoDTO todoDTO, TodoService todoService, IValidator<TodoDTO> validator,
            IMapper mapper) =>
        {
            var validResult = validator.Validate(todoDTO);
            if (!validResult.IsValid)
            {
                return Results.ValidationProblem(validResult.ToDictionary());
            }

            var newTodo = mapper.Map<Todo>(todoDTO);

            var createdTodo = await todoService.CreateAsync(newTodo);
            var todoViewDto = mapper.Map<TodoViewDto>(createdTodo);
            return Results.Created($"/{todoViewDto.Id}", todoViewDto);
        });

        todosEndpoints.MapPut("/{id:int}", async (int id, TodoDTO newTodoDTO, TodoService todoService,
            IValidator<TodoDTO> validator, IMapper mapper) =>
        {
            var todoToUpdate = await todoService.GetAsync(id);
            if (todoToUpdate is null)
            {
                return Results.NotFound();
            }

            var validResult = validator.Validate(newTodoDTO);
            if (!validResult.IsValid)
            {
                return Results.ValidationProblem(validResult.ToDictionary());
            }

            mapper.Map(newTodoDTO, todoToUpdate);

            await todoService.UpdateAsync(todoToUpdate);
            return Results.NoContent();
        });

        todosEndpoints.MapDelete("/{id:int}", async (int id, TodoService todoService, IMapper mapper) =>
        {
            var todoToDelete = await todoService.GetAsync(id);
            if (todoToDelete is null)
            {
                return Results.NotFound();
            }

            await todoService.DeleteAsync(todoToDelete);
            return Results.NoContent();
        });
    }
}