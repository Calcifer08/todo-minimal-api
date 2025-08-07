using System.Net;
using TodoApi.Tests.Integration.Support;

namespace TodoApi.Tests.Integration
{
  public class GeneralTests : IClassFixture<TodoApiFactory>
  {
    private readonly HttpClient _client;

    public GeneralTests(TodoApiFactory factory)
    {
      _client = factory.CreateClient();
    }

    [Fact]
    public async Task ErrorEndpoint_ReturnInternalServerError()
    {
      // Arrange

      // Act
      var response = await _client.GetAsync("/error");

      // Assert
      Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
  }
}
