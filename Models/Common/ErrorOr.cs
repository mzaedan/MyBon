using System.Text.Json.Serialization;

using MyBon.Domain.Enums;

namespace MyBon.Models.Common;

/// <summary>
/// Base class untuk response tanpa data
/// </summary>
public class ErrorOr
{
    public string? Message { get; set; }
    public bool Success { get; set; }
    public ErrorType ErrorType { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; set; }

    // Parameterless constructor untuk serialization
    public ErrorOr() { }

    public ErrorOr(string message, bool success, ErrorType type = ErrorType.None)
    {
        Message = message;
        Success = success;
        ErrorType = type;
    }
    
    /// <summary>
    /// Membuat instance ErrorOr untuk response sukses tanpa data
    /// </summary>
    public static ErrorOr CreateSuccess(string message) => new(message, true);
    
    /// <summary>
    /// Membuat instance ErrorOr untuk response error tanpa data
    /// </summary>
    public static ErrorOr CreateFailure(string message, ErrorType type = ErrorType.None) => new(message, false, type);

    /// <summary>
    /// Creates a validation failure response with errors grouped by property.
    /// </summary>
    public static ErrorOr CreateValidationFailure(Dictionary<string, string[]> errors)
        => new()
        {
            Message = "Validation failed.",
            Success = false,
            ErrorType = ErrorType.Validation,
            Errors = errors
        };
}

/// <summary>
/// Generic class untuk response dengan data
/// </summary>
public class ErrorOr<T> : ErrorOr
{
    public T? Data { get; set; }

    // Parameterless constructor untuk serialization
    public ErrorOr() { }

    public ErrorOr(string message, bool success, T? data = default) : base(message, success)
    {
        Data = data;
    }
    
    /// <summary>
    /// Membuat instance ErrorOr untuk response sukses dengan data
    /// </summary>
    public static ErrorOr<T> CreateSuccess(string message, T data) => new(message, true, data);

    /// <summary>
    /// Membuat instance ErrorOr untuk response error dengan tipe data (data akan null)
    /// </summary>
    public new static ErrorOr<T> CreateFailure(string message, ErrorType type = ErrorType.None) 
        => new()
        {
            Message = message,
            Success = false,
            ErrorType = type
        };

    /// <summary>
    /// Creates a typed validation failure response with errors grouped by property.
    /// </summary>
    public new static ErrorOr<T> CreateValidationFailure(Dictionary<string, string[]> errors)
        => new()
        {
            Message = "Validation failed.",
            Success = false,
            ErrorType = ErrorType.Validation,
            Errors = errors
        };
}
