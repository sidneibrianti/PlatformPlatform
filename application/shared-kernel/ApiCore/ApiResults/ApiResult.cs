using System.Net;
using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiCore.ApiResults;

public class ApiResult(ResultBase result, string? routePrefix = null) : IResult
{
    protected string? RoutePrefix { get; } = routePrefix;
    
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return ConvertResult().ExecuteAsync(httpContext);
    }
    
    protected virtual IResult ConvertResult()
    {
        if (!result.IsSuccess) return GetProblemDetailsAsJson();
        
        return RoutePrefix is null
            ? Results.Ok()
            : Results.Created($"{RoutePrefix}/{result}", null);
    }
    
    protected IResult GetProblemDetailsAsJson()
    {
        return Results.Problem(
            title: GetHttpStatusDisplayName(result.StatusCode),
            statusCode: (int)result.StatusCode,
            detail: result.ErrorMessage?.Message,
            extensions: result.Errors?.Length > 0
                ? new Dictionary<string, object?> { { nameof(result.Errors), result.Errors } }
                : null
        );
    }
    
    public static string GetHttpStatusDisplayName(HttpStatusCode statusCode)
    {
        return Regex.Replace(statusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.None, TimeSpan.FromSeconds(1));
    }
    
    public static implicit operator ApiResult(Result result)
    {
        return new ApiResult(result);
    }
}

public sealed class ApiResult<T>(Result<T> result, string? routePrefix = null) : ApiResult(result, routePrefix)
{
    protected override IResult ConvertResult()
    {
        if (!result.IsSuccess) return GetProblemDetailsAsJson();
        
        return RoutePrefix is null
            ? Results.Ok(result.Value!.Adapt<T>())
            : Results.Created($"{RoutePrefix}/{result.Value}", null);
    }
    
    public static implicit operator ApiResult<T>(Result<T> result)
    {
        return new ApiResult<T>(result);
    }
}
