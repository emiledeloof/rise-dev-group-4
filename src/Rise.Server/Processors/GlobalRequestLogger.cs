using System.Diagnostics;

namespace Rise.Server.Processors;

/// <summary>
/// runs before each endpoint, keeps the start time of the request and logs the request at debug level
/// </summary>
public class GlobalRequestLogger : IGlobalPreProcessor
{
    /// <summary>
    /// key of the start time in HttpContext.Items, read again by GlobalResponseLogger to compute the duration
    /// </summary>
    public const string StartTimestampKey = "RequestStartTimestamp";

    /// <summary>
    /// called by fastendpoints before the endpoint runs
    /// </summary>
    public Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        context.HttpContext.Items[StartTimestampKey] = Stopwatch.GetTimestamp();

        // ! debug level, the full request can be big and is only useful when debugging
        Log.Debug("Requesting '{RequestPath}' with parameters {@RequestParameters}", context.HttpContext.Request.Path, context.Request);

        return Task.CompletedTask;
    }
}
