using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Data;

namespace TodoApi.Tests.Integration.Support
{
    public class TodoApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {

                services.RemoveAll<IDbContextOptionsConfiguration<TodoDbContext>>();

                services.AddDbContext<TodoDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        }
    }
}