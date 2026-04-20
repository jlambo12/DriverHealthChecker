namespace DriverHealthChecker.App;

internal sealed class OperationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }

    private OperationResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Success(T value)
    {
        return new OperationResult<T>(true, value, null);
    }

    public static OperationResult<T> Failure(string errorMessage)
    {
        return new OperationResult<T>(false, default, errorMessage);
    }
}
