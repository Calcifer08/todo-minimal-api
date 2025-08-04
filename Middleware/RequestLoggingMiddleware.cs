using System.Diagnostics;

namespace TodoApi.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        stopwatch.Start();

        try
        {
            Console.WriteLine($"Входящий запрос: {context.Request.Method} {context.Request.Path}");

            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;

            Console.WriteLine($"Исходящий ответ: {context.Request.Method} {context.Request.Path} " +
                                  $"- Статус: {context.Response.StatusCode} " +
                                  $"- Время: {elapsed}мс");
        }
    }
}