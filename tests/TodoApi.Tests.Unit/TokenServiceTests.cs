using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests.Unit;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();

        _configMock.Setup(c => c["JWT:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["JWT:Audience"]).Returns("TestAudience");
        _configMock.Setup(c => c["JWT:Key"]).Returns("Какой-то-очень-длинный-ключ-для-токена");

        _sut = new TokenService(_configMock.Object);
    }

    [Fact]
    public void CreateToken_ReturnsValidJwtToken()
    {
        // Arrange
        var user = new ApiUser()
        {
            Id = "a1b2c3d4-e5f6-7890-1234-567890abcdef",
            Email = "testuser@example.com",
            UserName = "testuser@example.com"
        };

        // Act
        var tokenString = _sut.CreateToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId);
        Assert.NotNull(userIdClaim);
        Assert.Equal(user.Id, userIdClaim.Value);

        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);

        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Contains("TestAudience", token.Audiences);
    }
}