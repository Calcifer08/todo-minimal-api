using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService
{
  private readonly TodoDbContext _dbContext;

  public TodoService(TodoDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public List<Todo> GetAll() => _dbContext.TodoSet.ToList();

  public Todo? Get(int id)
  {
    return _dbContext.TodoSet.Find(id);
  }

  public void Create(Todo todo)
  {
    _dbContext.Add(todo);
    _dbContext.SaveChanges();
  }

  public void Update(int id, Todo newTodo)
  {
    var todo = _dbContext.TodoSet.Find(id);

    if (todo is null) return;

    todo.Name = newTodo.Name;
    todo.IsComplete = newTodo.IsComplete;
    _dbContext.SaveChanges();
  }

  public void Delete(int id)
  {
    var todo = Get(id);

    if (todo is null) return;

    _dbContext.Remove(todo);
    _dbContext.SaveChanges();
  }
}