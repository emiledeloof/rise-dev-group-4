using System.Diagnostics;
using Serilog.Events;

namespace Rise.Server.Processors;

/// <summary>
/// runs after the response is sent, writes one line per request with the method, the path, the status code and the duration
/// </summary>
public class GlobalResponseLogger : IGlobalPostProcessor
{
    /// <summary>
    /// called by fastendpoints after the response is sent
    /// </summary>
    public Task PostProcessAsync(IPostProcessorContext context, CancellationToken ct)
    {
        // read the start time saved by GlobalRequestLogger in context.HttpContext.Items
        // ? the value is stored as object, it must be turned back into a long
        if (context.HttpContext.Items.TryGetValue(GlobalRequestLogger.StartTimestampKey, out var startValue) && startValue is long start)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            var path = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;
            var statusCode = context.HttpContext.Response.StatusCode;

            var level = statusCode switch
            {
                >= 500 => LogEventLevel.Error,
                >= 400 => LogEventLevel.Warning,
                _ => LogEventLevel.Information
            };

            // ! never log context.Response, it can contain whole lists and personal data
            Log.Write(level, "{Method} {Path} responded {StatusCode} in {ElapsedMs:0} ms", method, path, statusCode, elapsedMs);
        }

        return Task.CompletedTask;
    }
}
