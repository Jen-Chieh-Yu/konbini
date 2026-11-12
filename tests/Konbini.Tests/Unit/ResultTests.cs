using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Konbini.Tests.Unit;

public class ResultTests
{
    [Fact]
    public void ValueConvertsToSuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ErrorConvertsToFailure_AndValueThrows()
    {
        Result<int> result = Error.NotFound("找不到商品");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
        Assert.Equal("找不到商品", result.Error!.Errors[Error.GeneralKey]);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void EmptyValidationErrors_Throw()
    {
        Assert.Throws<ArgumentException>(() => Error.Validation(new Dictionary<string, string>()));
    }

    [Fact]
    public void ValidationErrors_AreCopied()
    {
        var errors = new Dictionary<string, string> { ["email"] = "請填寫Email" };

        var error = Error.Validation(errors);
        errors["phoneNumber"] = "請填寫手機號碼";

        Assert.Single(error.Errors);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    public void ToProblem_MapsTypeToStatusCode(ErrorType type, int expectedStatus)
    {
        var error = type switch
        {
            ErrorType.Validation => Error.Validation("email", "訊息"),
            ErrorType.Unauthorized => Error.Unauthorized("訊息"),
            ErrorType.Forbidden => Error.Forbidden("訊息"),
            ErrorType.NotFound => Error.NotFound("訊息"),
            ErrorType.Conflict => Error.Conflict("items", "訊息"),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblem());

        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal(expectedStatus, problem.ProblemDetails.Status);
        Assert.False(string.IsNullOrEmpty(problem.ProblemDetails.Title));
    }

    [Fact]
    public void ToProblem_PutsFieldErrorsInErrorsExtension()
    {
        var error = Error.Validation(new Dictionary<string, string>
        {
            ["email"] = "請填寫Email",
            ["phoneNumber"] = "請填寫手機號碼",
        });

        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblem());

        var errors = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(problem.ProblemDetails.Extensions["errors"]);
        Assert.Equal("請填寫Email", errors["email"]);
        Assert.Equal("請填寫手機號碼", errors["phoneNumber"]);
    }
}
