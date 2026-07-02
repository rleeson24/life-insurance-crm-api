namespace LifeInsuranceCRM.Utilities;

public sealed class ProcessResponse<T>
{
    private ProcessResponse(UseCaseStatus status, T? result, string? message, string? errorCode)
    {
        Status = status;
        Result = result;
        Message = message;
        ErrorCode = errorCode;
    }

    public UseCaseStatus Status { get; }

    public T? Result { get; }

    public string? Message { get; }

    public string? ErrorCode { get; }

    public bool IsSuccess => Status == UseCaseStatus.Success;

    public static ProcessResponse<T> Succeeded(T result) =>
        new(UseCaseStatus.Success, result, null, null);

    public static ProcessResponse<T> WithStatus(UseCaseStatus status, string? message = null, string? errorCode = null) =>
        new(status, default, message, errorCode);

    public static ProcessResponse<T> InvalidRequestResponse(string message, string? errorCode = null) =>
        WithStatus(UseCaseStatus.InvalidRequest, message, errorCode);

    public ProcessResponse<TTarget> MapResult<TTarget>() =>
        new(Status, default, Message, ErrorCode);
}
