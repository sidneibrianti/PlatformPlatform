using System.Net;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

public abstract class ResultBase
{
    protected ResultBase(HttpStatusCode httpStatusCode)
    {
        IsSuccess = true;
        StatusCode = httpStatusCode;
    }
    
    protected ResultBase(HttpStatusCode statusCode, ErrorMessage errorMessage, bool commitChanges, ErrorDetail[] errors)
    {
        IsSuccess = false;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        CommitChangesOnFailure = commitChanges;
        Errors = errors;
    }
    
    public bool IsSuccess { get; }
    
    public HttpStatusCode StatusCode { get; }
    
    public ErrorMessage? ErrorMessage { get; }
    
    public bool CommitChangesOnFailure { get; }
    
    public ErrorDetail[]? Errors { get; }
    
    public string GetErrorSummary()
    {
        return ErrorMessage?.Message ?? string.Join(Environment.NewLine, Errors!.Select(ed => $"{ed.Code}: {ed.Message}"));
    }
}

/// <summary>
///     The Result class is used when a successful result is not returning any value (e.g. in the case of an Update or
///     Delete). On success the HttpStatusCode NoContent will be returned. In the case of a failure, the result will
///     contain either an <see cref="ErrorMessage" /> or a collection of a <see cref="ErrorMessage" />.
/// </summary>
public sealed class Result : ResultBase
{
    private Result(HttpStatusCode httpStatusCode) : base(httpStatusCode)
    {
    }
    
    public Result(HttpStatusCode statusCode, ErrorMessage errorMessage, bool commitChanges, ErrorDetail[] errors)
        : base(statusCode, errorMessage, commitChanges, errors)
    {
    }
    
    public static Result Success()
    {
        return new Result(HttpStatusCode.NoContent);
    }
    
    public static Result BadRequest(string message, bool commitChanges = false)
    {
        return new Result(HttpStatusCode.BadRequest, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result Unauthorized(string message, bool commitChanges = false)
    {
        return new Result(HttpStatusCode.Unauthorized, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result Forbidden(string message, bool commitChanges = false)
    {
        return new Result(HttpStatusCode.Forbidden, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result NotFound(string message, bool commitChanges = false)
    {
        return new Result(HttpStatusCode.NotFound, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result Conflict(string message, bool commitChanges = false)
    {
        return new Result(HttpStatusCode.Conflict, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result TooManyRequests(string message, bool commitChanges = false)
    {
        return new Result(HttpStatusCode.TooManyRequests, new ErrorMessage(message), commitChanges, []);
    }
}

/// <summary>
///     The ResultT class is used when a successful command or query is returning value (e.g. in the case of an Get or
///     Create). On success the HttpStatusCode OK will be returned. In the case of a failure, the result will
///     contain either an <see cref="ErrorMessage" /> or a collection of a <see cref="ErrorMessage" />.
/// </summary>
public sealed class Result<T> : ResultBase
{
    private Result(T value, HttpStatusCode httpStatusCode) : base(httpStatusCode)
    {
        Value = value;
    }
    
    public Result(HttpStatusCode statusCode, ErrorMessage errorMessage, bool commitChanges, ErrorDetail[] errors)
        : base(statusCode, errorMessage, commitChanges, errors)
    {
    }
    
    public T? Value { get; }
    
    /// <summary>
    ///     Use this to indicate a successful command. There is a implicit conversion from T to
    ///     <see cref="Result{T}" />, so you can also just return T from a command handler.
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new Result<T>(value, HttpStatusCode.OK);
    }
    
    public static Result<T> BadRequest(string message, bool commitChanges = false)
    {
        return new Result<T>(HttpStatusCode.BadRequest, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result<T> Unauthorized(string message, bool commitChanges = false)
    {
        return new Result<T>(HttpStatusCode.Unauthorized, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result<T> Forbidden(string message, bool commitChanges = false)
    {
        return new Result<T>(HttpStatusCode.Forbidden, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result<T> NotFound(string message, bool commitChanges = false)
    {
        return new Result<T>(HttpStatusCode.NotFound, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result<T> Conflict(string message, bool commitChanges = false)
    {
        return new Result<T>(HttpStatusCode.Conflict, new ErrorMessage(message), commitChanges, []);
    }
    
    public static Result<T> TooManyRequests(string message, bool commitChanges = false)
    {
        return new Result<T>(HttpStatusCode.TooManyRequests, new ErrorMessage(message), commitChanges, []);
    }
    
    /// <summary>
    ///     This is an implicit conversion from T to <see cref="Result{T}" />. This is used to easily return a
    ///     successful <see cref="Result{T}" /> from a command handler.
    /// </summary>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }
}
