using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService
{
  private readonly TodoDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public TodoService(TodoDbContext dbContext, IMapper mapper, IHttpContextAccessor httpContextAccessor)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _httpContextAccessor = httpContextAccessor;
  }

  private string? GetCurrentUserId()
  {
    return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
  }

  public async Task<List<Todo>> GetAllAsync()
  {
    var currentUserId = GetCurrentUserId();

    return await _dbContext.TodoSet.Where(t => t.UserId == currentUserId).ToListAsync();
  }

  public async Task<Todo?> GetAsync(int id)
  {
    var currentUserId = GetCurrentUserId();
    return await _dbContext.TodoSet.FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);
  }

  public async Task<Todo> CreateAsync(TodoDTO todoDTO)
  {
    var currentUserId = GetCurrentUserId();

    if (string.IsNullOrEmpty(currentUserId))
    {
      throw new InvalidOperationException("Не удалось определить пользователя для создания задачи");
    }


    var todo = _mapper.Map<Todo>(todoDTO);
    todo.UserId = currentUserId;
    await _dbContext.AddAsync(todo);
    await _dbContext.SaveChangesAsync();

    return todo;
  }

  public async Task UpdateAsync(int id, TodoDTO newTodoDTO)
  {
    var oldTodo = await GetAsync(id);

    if (oldTodo is null) return;

    _mapper.Map(newTodoDTO, oldTodo);
    await _dbContext.SaveChangesAsync();
  }

  public async Task DeleteAsync(int id)
  {
    var todo = await GetAsync(id);

    if (todo is null) return;

    _dbContext.Remove(todo);
    await _dbContext.SaveChangesAsync();
  }
}