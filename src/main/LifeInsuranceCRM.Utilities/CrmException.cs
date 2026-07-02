namespace LifeInsuranceCRM.Utilities;

public sealed class CrmException : Exception
{
    public CrmException(UseCaseStatus status, string message, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
        ErrorCode = errorCode;
    }

    public UseCaseStatus Status { get; }

    public string? ErrorCode { get; }
}
