using TodoApi.Models;
using TodoApi.Services;
using TodoApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string? connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TodoDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<TodoService>();
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

app.MapPost("/", (Todo todo, TodoService todoService) =>
{
    if (todo is null || string.IsNullOrWhiteSpace(todo.Name))
    {
        return Results.BadRequest("Неверные данные для задачи.");
    }

    todoService.Create(todo);
    return Results.Created($"/{todo.Id}", todo);
});

app.MapPut("/{id:int}", (int id, Todo inputTodo, TodoService todoService) =>
{
    if (inputTodo is null || string.IsNullOrWhiteSpace(inputTodo.Name))
    {
        return Results.BadRequest("Неверные данные для задачи.");
    }


    var todo = todoService.Get(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    todoService.Update(id, inputTodo);
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
