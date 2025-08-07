using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;
using TodoApi.Tests.Integration.Support;

namespace TodoApi.Tests.Integration.Endpoints;

public class TodosEndpointsTests : IClassFixture<TodoApiFactory>
{
    private readonly TodoApiFactory _factory;
    private readonly HttpClient _client;

    public TodosEndpointsTests(TodoApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(string UserId, string Token)> CreateUserAndGetTokenAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();

        var user = new ApiUser { UserName = email, Email = email };

        var result = await userManager.CreateAsync(user, "Password123");
        if (!result.Succeeded)
        {
            throw new Exception($"Не удалось создать пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var token = tokenService.CreateToken(user);
        return (user.Id, token);
    }

    [Fact]
    public async Task GetAll_ReturnsOwnedUserTodos()
    {
        // Arrange
        var (user1Id, user1Token) = await CreateUserAndGetTokenAsync($"user1_{Guid.NewGuid()}@test.com");
        var (user2Id, _) = await CreateUserAndGetTokenAsync($"user2_{Guid.NewGuid()}@test.com");

        List<int> user1TodoIdsInDb;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            var user1Task1 = new Todo { Name = "Задача пользователя 1", UserId = user1Id };
            var user1Task2 = new Todo { Name = "Задача другого пользователя 1", UserId = user1Id };

            dbContext.TodoSet.Add(user1Task1);
            dbContext.TodoSet.Add(user1Task2);
            dbContext.TodoSet.Add(new Todo { Name = "Задача пользователя 2", UserId = user2Id });
            await dbContext.SaveChangesAsync();

            user1TodoIdsInDb = new List<int> { user1Task1.Id, user1Task2.Id };
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.GetAsync("/api/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var returnedTodos = await response.Content.ReadFromJsonAsync<List<TodoViewDto>>();

        returnedTodos.Should().HaveCount(2);

        var returnedIds = returnedTodos.Select(t => t.Id);
        returnedIds.Should().BeEquivalentTo(user1TodoIdsInDb);
    }

    [Fact]
    public async Task GetAll_GetTodosWithoutAuthorization()
    {
        // Act
        var response = await _client.GetAsync("/api/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostTodo_CreateTodoAuthorizedUser()
    {
        // Arrange
        var (userId, token) = await CreateUserAndGetTokenAsync($"user1_{Guid.NewGuid()}@test.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newTodoDto = new TodoDTO { Name = "Новая задача" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", newTodoDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTodoDto = await response.Content.ReadFromJsonAsync<TodoViewDto>();
        createdTodoDto.Should().NotBeNull();
        createdTodoDto.Name.Should().Be(newTodoDto.Name);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

        var todoInDb = await dbContext.TodoSet.FindAsync(createdTodoDto.Id);

        todoInDb.Should().NotBeNull();
        todoInDb.UserId.Should().Be(userId);
        todoInDb.Name.Should().Be(newTodoDto.Name);
    }


    [Fact]
    public async Task DeleteTodo_DeleteNotOwnedUserTodo()
    {
        // Arrange
        var (_, user1Token) = await CreateUserAndGetTokenAsync($"user1_{Guid.NewGuid()}@test.com");
        var (user2Id, _) = await CreateUserAndGetTokenAsync($"user2_{Guid.NewGuid()}@test.com");

        Todo user2Todo;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            user2Todo = new Todo { Name = "User 2 Secret Task", UserId = user2Id };
            dbContext.TodoSet.Add(user2Todo);
            await dbContext.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user1Token);

        // Act
        var response = await _client.DeleteAsync($"/api/todos/{user2Todo.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
