using TodoApi.Tests.Integration.Support;
using TodoApi.DTOs;
using System.Net.Http.Json;
using FluentAssertions;
using System.Net;

namespace TodoApi.Tests.Integration.Endpoints;

public class AuthEndpointsTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(TodoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private class TokenResponse { public string Token { get; set; } = ""; }

    [Fact]
    public async Task Register_WithValidData()
    {
        // Arrange
        var regisDTO = new RegisterDto()
        {
            Email = $"test{Guid.NewGuid()}@email.com",
            Password = "Password1",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", regisDTO);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_WithInvalidData()
    {
        // Arrange
        var regisDTO = new RegisterDto()
        {
            Email = $"test{Guid.NewGuid()}@email.com",
            Password = "Pass",
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", regisDTO);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    }

    [Fact]
    public async Task Login_WithValidData()
    {
        // Arrange
        var regisDTO = new RegisterDto()
        {
            Email = $"test{Guid.NewGuid()}@email.com",
            Password = "Password1",
        };

        await _client.PostAsJsonAsync("/api/auth/register", regisDTO);

        var loginDTO = new LoginDto() { Email = regisDTO.Email, Password = regisDTO.Password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDTO);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        token!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidData()
    {
        // Arrange
        var regisDTO = new RegisterDto()
        {
            Email = $"test{Guid.NewGuid()}@email.com",
            Password = "Password1",
        };

        await _client.PostAsJsonAsync("/api/auth/register", regisDTO);

        var loginDTO = new LoginDto() { Email = regisDTO.Email, Password = "WrongPassword1" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDTO);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}