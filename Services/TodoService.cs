namespace TodoApi.Services;

using TodoApi.Models;

public static class TodoService
{
  private static List<Todo> _todoList;
  private static int _nextId = 3;

  static TodoService()
  {
    _todoList = new List<Todo>()
    {
        new Todo() { Id = 1, Name = "Todo1", IsComplete = false },
        new Todo() { Id = 2, Name = "Todo2", IsComplete = false },
    };
  }

  public static List<Todo> GetAll() => _todoList;

  public static Todo? Get(int id)
  {
    return _todoList.FirstOrDefault(t => t.Id == id);
  }

  public static void Create(Todo todo)
  {
    if (todo is not null)
    {
      _nextId++;
      todo.Id = _nextId;
      _todoList.Add(todo);

    }
  }

  public static void Update(int id, Todo newTodo)
  {
    var todo = _todoList.FirstOrDefault(t => t.Id == id);

    if (todo is not null)
    {
      todo.Name = newTodo.Name;
      todo.IsComplete = newTodo.IsComplete;
    }
  }

  public static void Delete(int id)
  {
    var todo = Get(id);

    if (todo is not null)
    {
      _todoList.Remove(todo);
    }
  }
}