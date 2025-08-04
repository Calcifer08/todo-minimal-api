using TodoApi.Models;
using TodoApi.Services;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () =>
{
    var todos = TodoService.GetAll();
    return Results.Ok(todos);

});

app.MapGet("/{id:int}", (int id) =>
{
    var todo = TodoService.Get(id);

    if (todo is not null)
        return Results.Ok(todo);
    else
        return Results.NotFound();
});

app.MapPost("/", (Todo todo) =>
{
    if (todo is null || string.IsNullOrWhiteSpace(todo.Name))
    {
        return Results.BadRequest("Неверные данные для задачи.");
    }

    TodoService.Create(todo);
    return Results.Created($"/{todo.Id}", todo);
});

app.MapPut("/{id:int}", (int id, Todo inputTodo) =>
{
    if (inputTodo is null || string.IsNullOrWhiteSpace(inputTodo.Name))
    {
        return Results.BadRequest("Неверные данные для задачи.");
    }


    var todo = TodoService.Get(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    TodoService.Update(id, inputTodo);
    return Results.NoContent();
});

app.MapDelete("/{id:int}", (int id) =>
{
    var todo = TodoService.Get(id);
    if (todo is null)
    {
        return Results.NotFound();
    }

    TodoService.Delete(id);
    return Results.NoContent();
});

app.Run();
