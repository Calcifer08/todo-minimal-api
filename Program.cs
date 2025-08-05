using TodoApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();

app.ConfigureMiddleware();

app.MapErrorEndpoints();
app.MapAuthEndpoints();
app.MapTodoEndpoints();

app.Run();
