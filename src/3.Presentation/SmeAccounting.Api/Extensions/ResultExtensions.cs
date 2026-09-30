using Microsoft.AspNetCore.Mvc;
using SmeAccounting.Application.Common;

namespace SmeAccounting.Api.Extensions;

public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(new { data = result.Value });
        }

        var problem = new ProblemDetails
        {
            Title = result.ErrorCode,
            Detail = result.ErrorMessage,
            Extensions = { ["errorCode"] = result.ErrorCode ?? "UNKNOWN" }
        };

        return result.ErrorCode switch
        {
            "VALIDATION.FAILED" => new BadRequestObjectResult(problem),
            not null when result.ErrorCode.StartsWith("VALIDATION", StringComparison.Ordinal) => new BadRequestObjectResult(problem),
            "NOT_FOUND" => new NotFoundObjectResult(problem),
            _ => new UnprocessableEntityObjectResult(problem)
        };
    }
}
