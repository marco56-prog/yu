namespace AccountingSystem.Models;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;

    public static Result<T> Success(T data) => new Result<T> { IsSuccess = true, Data = data };
    public static Result<T> Failure(string message) => new Result<T> { IsSuccess = false, Data = default, Message = message };
}
