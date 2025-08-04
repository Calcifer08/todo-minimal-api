using AutoMapper;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService
{
  private readonly TodoDbContext _dbContext;
  private readonly IMapper _mapper;

  public TodoService(TodoDbContext dbContext, IMapper mapper)
  {
    _dbContext = dbContext;
    _mapper = mapper;
  }

  public List<Todo> GetAll() => _dbContext.TodoSet.ToList();

  public Todo? Get(int id)
  {
    return _dbContext.TodoSet.Find(id);
  }

  public Todo Create(TodoDTO todoDTO)
  {
    var todo = _mapper.Map<Todo>(todoDTO);
    _dbContext.Add(todo);
    _dbContext.SaveChanges();

    return todo;
  }

  public void Update(int id, TodoDTO newTodoDTO)
  {
    var oldTodo = _dbContext.TodoSet.Find(id);

    if (oldTodo is null) return;

    _mapper.Map(newTodoDTO, oldTodo);
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