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
  private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
  private readonly TodoService _sut;

  public TodoServiceTests()
  {
    var options = new DbContextOptionsBuilder<TodoDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    _dbContext = new TodoDbContext(options);

    _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

    _sut = new TodoService(_dbContext, _httpContextAccessorMock.Object);
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

    var newTodo = new Todo() { Name = "Новая задача user-1", IsComplete = false };

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
    var newTodo = new Todo() { Name = "Новая задача", IsComplete = false };

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(newTodo));
  }

  [Fact]
  public async Task Update_ExistingTodoOwnedUser()
  {
    // Arrange
    SeedDatabase();

    var todoToUpdate = await _dbContext.TodoSet.FindAsync(1);
    Assert.NotNull(todoToUpdate);

    var originalName = todoToUpdate.Name;
    var newName = "Обновленная задачв";
    todoToUpdate.Name = newName;

    // Act
    await _sut.UpdateAsync(todoToUpdate);

    // Assert
    var todoFromDb = await _dbContext.TodoSet.AsNoTracking().FirstOrDefaultAsync(t => t.Id == 1);
    Assert.NotNull(todoFromDb);
    Assert.Equal(newName, todoFromDb.Name);
  }

  [Fact]
  public async Task Delete_ExistingTodoOwnedUser()
  {
    // Arrange
    SeedDatabase();
    var initialCountTodo = await _dbContext.TodoSet.CountAsync();
    var todoToDelete = await _dbContext.TodoSet.FindAsync(1);
    Assert.NotNull(todoToDelete);

    // Act
    await _sut.DeleteAsync(todoToDelete!);

    // Assert
    Assert.Equal(initialCountTodo - 1, _dbContext.TodoSet.Count());
    var result = await _dbContext.TodoSet.FindAsync(1);
    Assert.Null(result);
  }
}
