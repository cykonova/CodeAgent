namespace CodeAgent.Domain.Exceptions;

public class CodeAgentException : Exception
{
    public string ErrorCode { get; }
    public ErrorSeverity Severity { get; }
    public Dictionary<string, object> Context { get; }

    public CodeAgentException(string message, string errorCode = "GENERAL_ERROR", ErrorSeverity severity = ErrorSeverity.Error) 
        : base(message)
    {
        ErrorCode = errorCode;
        Severity = severity;
        Context = new Dictionary<string, object>();
    }

    public CodeAgentException(string message, Exception innerException, string errorCode = "GENERAL_ERROR", ErrorSeverity severity = ErrorSeverity.Error)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Severity = severity;
        Context = new Dictionary<string, object>();
    }

    public CodeAgentException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}