namespace CodeAgent.Domain.Exceptions;

public class FileOperationException : CodeAgentException
{
    public string FilePath { get; }
    public FileOperationType OperationType { get; }

    public FileOperationException(string message, string filePath, FileOperationType operationType)
        : base(message, "FILE_OPERATION_ERROR", ErrorSeverity.Error)
    {
        FilePath = filePath;
        OperationType = operationType;
        WithContext("FilePath", filePath)
            .WithContext("OperationType", operationType.ToString());
    }

    public FileOperationException(string message, string filePath, FileOperationType operationType, Exception innerException)
        : base(message, innerException, "FILE_OPERATION_ERROR", ErrorSeverity.Error)
    {
        FilePath = filePath;
        OperationType = operationType;
        WithContext("FilePath", filePath)
            .WithContext("OperationType", operationType.ToString());
    }
}

public enum FileOperationType
{
    Read,
    Write,
    Delete,
    Create,
    Move,
    Copy
}