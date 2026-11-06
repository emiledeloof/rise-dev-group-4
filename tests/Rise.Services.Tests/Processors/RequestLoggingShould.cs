using System.Diagnostics;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Rise.Server.Processors;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Rise.Services.Tests.Processors;

/// <summary>
/// tests for <see cref="GlobalRequestLogger"/> and <see cref="GlobalResponseLogger"/>,
/// the response line must have the right level so grafana can filter errors
/// </summary>
// ! Log.Logger is static, the collection stops these tests from running at the same time as each other
[Collection(nameof(RequestLoggingShould))]
public class RequestLoggingShould : IDisposable
{
    private readonly CapturedEvents events = new();
    private readonly ILogger previousLogger = Log.Logger;

    public RequestLoggingShould()
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(events).CreateLogger();
    }

    public void Dispose() => Log.Logger = previousLogger;

    [Fact]
    public async Task KeepTheStartTimeOfTheRequest()
    {
        var httpContext = new DefaultHttpContext();
        var context = Substitute.For<IPreProcessorContext>();
        context.HttpContext.Returns(httpContext);

        await new GlobalRequestLogger().PreProcessAsync(context, CancellationToken.None);

        httpContext.Items[GlobalRequestLogger.StartTimestampKey].ShouldBeOfType<long>();
    }

    [Theory]
    [InlineData(200, LogEventLevel.Information)]
    [InlineData(201, LogEventLevel.Information)]
    [InlineData(400, LogEventLevel.Warning)]
    [InlineData(404, LogEventLevel.Warning)]
    [InlineData(500, LogEventLevel.Error)]
    [InlineData(503, LogEventLevel.Error)]
    public async Task PickTheLevelFromTheStatusCode(int statusCode, LogEventLevel expected)
    {
        var context = PostContext("GET", "/api/rooms", statusCode, Stopwatch.GetTimestamp());

        await new GlobalResponseLogger().PostProcessAsync(context, CancellationToken.None);

        ResponseLine().Level.ShouldBe(expected);
    }

    [Fact]
    public async Task WriteTheMethodPathAndStatusAsSeparateFields()
    {
        var context = PostContext("POST", "/api/bookings", 201, Stopwatch.GetTimestamp());

        await new GlobalResponseLogger().PostProcessAsync(context, CancellationToken.None);

        // * structured fields, not one big string, so grafana can filter on them
        var line = ResponseLine();
        line.Properties["Method"].ToString().ShouldContain("POST");
        line.Properties["Path"].ToString().ShouldContain("/api/bookings");
        line.Properties["StatusCode"].ToString().ShouldBe("201");
        line.Properties.ShouldContainKey("ElapsedMs");
    }

    [Fact]
    public async Task WriteNothingWithoutAStartTime()
    {
        // ? happens when the request failed before the pre processor ran
        var context = PostContext("GET", "/api/rooms", 200, startTimestamp: null);

        await new GlobalResponseLogger().PostProcessAsync(context, CancellationToken.None);

        events.All.ShouldNotContain(e => e.MessageTemplate.Text.Contains("responded"));
    }

    // builds a finished request, startTimestamp is what GlobalRequestLogger would have saved
    private static IPostProcessorContext PostContext(string method, string path, int statusCode, long? startTimestamp)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.Response.StatusCode = statusCode;
        if (startTimestamp is not null)
        {
            httpContext.Items[GlobalRequestLogger.StartTimestampKey] = startTimestamp.Value;
        }

        var context = Substitute.For<IPostProcessorContext>();
        context.HttpContext.Returns(httpContext);
        return context;
    }

    private LogEvent ResponseLine() => events.All.Single(e => e.MessageTemplate.Text.Contains("responded"));

    // keeps every log event in memory instead of writing it to the console
    private sealed class CapturedEvents : ILogEventSink
    {
        public List<LogEvent> All { get; } = [];

        public void Emit(LogEvent logEvent) => All.Add(logEvent);
    }
}
