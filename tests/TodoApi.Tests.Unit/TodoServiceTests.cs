using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests.Unit;

public class TodoServiceTests
{
  private readonly TodoDbContext _dbContext;
  private readonly Mock<IMapper> _mapperMock;
  private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
  private readonly TodoService _sut;

  public TodoServiceTests()
  {
    var options = new DbContextOptionsBuilder<TodoDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    _dbContext = new TodoDbContext(options);

    _mapperMock = new Mock<IMapper>();
    _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

    _sut = new TodoService(_dbContext, _mapperMock.Object, _httpContextAccessorMock.Object);
  }

  private void SeedDatabase()
  {
    _dbContext.TodoSet.AddRange(
        new Todo { Id = 1, Name = "Задача 1 от user-1", UserId = "user-1" },
        new Todo { Id = 2, Name = "Задача 2 от user-1", UserId = "user-1" },
        new Todo { Id = 3, Name = "Задача 1 от user-2", UserId = "user-2" }
    );
    _dbContext.SaveChanges();
  }

  private void SetupHttpContextForUser(string userId)
  {
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
    var identity = new ClaimsIdentity(claims);
    var claimsPrincipal = new ClaimsPrincipal(identity);

    var httpContextMock = new Mock<HttpContext>();
    httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);

    _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
  }


  [Fact]
  public async Task GetAll_ReturnsOnlyUserTodos()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);

    // Act
    var result = await _sut.GetAllAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.All(result, todo => Assert.Equal(currentUserId, todo.UserId));
  }

  [Fact]
  public async Task GetById_ReturnsTodoIfUserOwnsIt()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoIdToFind = 1;
    SetupHttpContextForUser(currentUserId);

    // Act
    var result = await _sut.GetAsync(todoIdToFind);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(todoIdToFind, result.Id);
    Assert.Equal(currentUserId, result.UserId);
  }

  [Fact]
  public async Task GetById_ReturnsNullIfNotUserTodo()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var otherUserTodoId = 3;
    SetupHttpContextForUser(currentUserId);

    // Act
    var result = await _sut.GetAsync(otherUserTodoId);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task Create_TodoAuthorizedUser()
  {
    // Arrange
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);

    var newTodo = new TodoDTO() { Name = "Новая задача user-1", IsComplete = false };

    _mapperMock.Setup(m => m.Map<Todo>(It.IsAny<TodoDTO>()))
               .Returns((TodoDTO dto) => new Todo { Name = dto.Name, IsComplete = dto.IsComplete });

    var initialCountTodo = _dbContext.TodoSet.Count();

    // Act
    var result = await _sut.CreateAsync(newTodo);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(currentUserId, result.UserId);
    Assert.Equal(initialCountTodo + 1, _dbContext.TodoSet.Count());
    Assert.Equal(newTodo.Name, result.Name);
    Assert.Equal(newTodo.IsComplete, result.IsComplete);
  }

  [Fact]
  public async Task Create_TodoAuthorizedNotUser()
  {
    // Arrange
    var newTodo = new TodoDTO() { Name = "Новая задача", IsComplete = false };

    _mapperMock.Setup(m => m.Map<Todo>(It.IsAny<TodoDTO>()))
               .Returns((TodoDTO dto) => new Todo { Name = dto.Name, IsComplete = dto.IsComplete });

    var initialCountTodo = _dbContext.TodoSet.Count();

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(newTodo));
  }

  [Fact]
  public async Task Update_ExistingTodoOwnedUser()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoId = 1;
    SetupHttpContextForUser(currentUserId);

    var updateTodo = new TodoDTO() { Name = "Задача 1 от user-1 - выполнена", IsComplete = true };

    _mapperMock.Setup(m => m.Map(It.IsAny<TodoDTO>(), It.IsAny<Todo>()))
               .Callback((TodoDTO dto, Todo todo) =>
               {
                 todo.Name = dto.Name;
                 todo.IsComplete = dto.IsComplete;
               });

    // Act
    await _sut.UpdateAsync(todoId, updateTodo);

    // Assert
    var result = await _dbContext.TodoSet.FindAsync(todoId);
    Assert.NotNull(result);
    Assert.Equal(updateTodo.Name, result.Name);
    Assert.Equal(updateTodo.IsComplete, result.IsComplete);
  }

  [Fact]
  public async Task Update_ExistingTodoOwnedNotUser()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoId = 3;
    SetupHttpContextForUser(currentUserId);

    var updateTodo = new TodoDTO() { Name = "Задача 3 от user-3 - выполнена", IsComplete = true };

    _mapperMock.Setup(m => m.Map(It.IsAny<TodoDTO>(), It.IsAny<Todo>()))
               .Callback((TodoDTO dto, Todo todo) =>
               {
                 todo.Name = dto.Name;
                 todo.IsComplete = dto.IsComplete;
               });

    // Act
    await _sut.UpdateAsync(todoId, updateTodo);

    // Assert
    var result = await _dbContext.TodoSet.FindAsync(todoId);
    Assert.NotNull(result);
    Assert.NotEqual(updateTodo.Name, result.Name);
    Assert.NotEqual(updateTodo.IsComplete, result.IsComplete);
  }

  [Fact]
  public async Task Update_NotExistingTodo()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoId = 999;
    SetupHttpContextForUser(currentUserId);

    var updateTodo = new TodoDTO() { Name = "Задача 999 от user-1 - выполнена", IsComplete = true };

    _mapperMock.Setup(m => m.Map(It.IsAny<TodoDTO>(), It.IsAny<Todo>()))
               .Callback((TodoDTO dto, Todo todo) =>
               {
                 todo.Name = dto.Name;
                 todo.IsComplete = dto.IsComplete;
               });

    // Act
    await _sut.UpdateAsync(todoId, updateTodo);

    // Assert
    var result = await _dbContext.TodoSet.FindAsync(todoId);
    Assert.Null(result);
  }

  [Fact]
  public async Task Delete_ExistingTodoOwnedUser()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoId = 1;
    SetupHttpContextForUser(currentUserId);

    var initialCountTodo = _dbContext.TodoSet.Count();

    // Act
    await _sut.DeleteAsync(todoId);

    // Assert
    Assert.Equal(initialCountTodo - 1, _dbContext.TodoSet.Count());

    var result = await _dbContext.TodoSet.FindAsync(todoId);
    Assert.Null(result);
  }

  [Fact]
  public async Task Delete_ExistingTodoOwnedNotUser()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoId = 3;
    SetupHttpContextForUser(currentUserId);

    var initialCountTodo = _dbContext.TodoSet.Count();

    // Act
    await _sut.DeleteAsync(todoId);

    // Assert
    Assert.Equal(initialCountTodo, _dbContext.TodoSet.Count());

    var result = await _dbContext.TodoSet.FindAsync(todoId);
    Assert.NotNull(result);
  }
}
