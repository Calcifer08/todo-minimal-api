using TodoApi.Middleware;

namespace TodoApi.Extensions;

public static class MiddlewareExtensions
{
    public static void ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseDeveloperExceptionPage();

            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler("/error");
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseCors("Policy");

        app.UseAuthentication();
        app.UseAuthorization();
    }
}