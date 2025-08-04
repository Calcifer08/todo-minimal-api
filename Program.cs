using TodoApi.Models;
using TodoApi.DTOs;
using TodoApi.Services;
using TodoApi.Data;
using Microsoft.EntityFrameworkCore;
using TodoApi.MappingProfiles;
using FluentValidation;
using TodoApi.Validators;

var builder = WebApplication.CreateBuilder(args);

string? connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TodoDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<TodoService>();
builder.Services.AddAutoMapper(mp =>
{
    mp.AddProfile<TodoProfile>();
});
builder.Services.AddScoped<IValidator<TodoDTO>, TodoDtoValidator>();

var app = builder.Build();

app.MapGet("/", (TodoService todoService) =>
{
    var todos = todoService.GetAll();
    return Results.Ok(todos);

});

app.MapGet("/{id:int}", (int id, TodoService todoService) =>
{
    var todo = todoService.Get(id);

    if (todo is not null)
        return Results.Ok(todo);
    else
        return Results.NotFound();
});

app.MapPost("/", (TodoDTO todoDTO, TodoService todoService, IValidator<TodoDTO> validator) =>
{
    var validResult = validator.Validate(todoDTO);
    if (!validResult.IsValid)
    {
        return Results.ValidationProblem(validResult.ToDictionary());
    }

    var todo = todoService.Create(todoDTO);
    return Results.Created($"/{todo.Id}", todo);
});

app.MapPut("/{id:int}", (int id, TodoDTO newTodoDTO, TodoService todoService, IValidator<TodoDTO> validator) =>
{
    var validResult = validator.Validate(newTodoDTO);
    if (!validResult.IsValid)
    {
        return Results.ValidationProblem(validResult.ToDictionary());
    }


    var todo = todoService.Get(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    todoService.Update(id, newTodoDTO);
    return Results.NoContent();
});

app.MapDelete("/{id:int}", (int id, TodoService todoService) =>
{
    var todo = todoService.Get(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    todoService.Delete(id);
    return Results.NoContent();
});

app.Run();
