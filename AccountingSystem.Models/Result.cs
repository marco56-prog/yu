namespace AccountingSystem.Models;

public static class Result
{
    public static Result<T> Success<T>(T data) => new Result<T> { IsSuccess = true, Data = data };
    public static Result<T> Failure<T>(string message) => new Result<T> { IsSuccess = false, Data = default, Message = message };
}

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
}
