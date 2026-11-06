using System.Net;
using System.Net.Http.Json;
using System.Text;
using Ardalis.Result;
using Shouldly;

namespace Rise.Client.Common;

/// <summary>
/// tests for <see cref="ResultReader"/>, every client service reads the api answers with it
/// </summary>
public class ResultReaderShould
{
    private const string FailedMessage = "The server could not handle the request, please try again.";

    private static HttpResponseMessage Response(HttpStatusCode status, HttpContent? content = null) => new(status)
    {
        Content = content ?? new StringContent("", Encoding.UTF8, "application/json"),
    };

    [Fact]
    public async Task ReadTheValueOfASuccess()
    {
        var response = Response(HttpStatusCode.OK, JsonContent.Create(new { value = 5, status = ResultStatus.Ok }));

        var result = await ResultReader.ReadAsync<int>(response);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(5);
    }

    [Fact]
    public async Task KeepTheReasonGivenByTheApi()
    {
        var response = Response(HttpStatusCode.Conflict,
            JsonContent.Create(new { status = ResultStatus.Conflict, errors = new[] { "Room 101 already exists." } }));

        var result = await ResultReader.ReadAsync<int>(response);

        result.Status.ShouldBe(ResultStatus.Conflict);
        result.Errors.ShouldContain("Room 101 already exists.");
    }

    /// <summary>
    /// a 401 has no body, it must not look like a success
    /// </summary>
    [Fact]
    public async Task GiveAnErrorForAnEmptyBody()
    {
        var result = await ResultReader.ReadAsync<int>(Response(HttpStatusCode.Unauthorized));

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(FailedMessage);
    }

    /// <summary>
    /// the 400 of fastendpoints has its own body, read as a result its status would be ok
    /// </summary>
    [Fact]
    public async Task GiveAnErrorWhenTheBodyDoesNotMatchTheHttpStatus()
    {
        var response = Response(HttpStatusCode.BadRequest,
            JsonContent.Create(new { statusCode = 400, message = "One or more errors occurred!" }));

        var result = await ResultReader.ReadAsync(response);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(FailedMessage);
    }
}
