namespace CodeAgent.Domain.Exceptions;

public class LLMProviderException : CodeAgentException
{
    public string Provider { get; }
    public bool IsRetryable { get; }

    public LLMProviderException(string message, string provider, bool isRetryable = false)
        : base(message, "LLM_PROVIDER_ERROR", ErrorSeverity.Error)
    {
        Provider = provider;
        IsRetryable = isRetryable;
        WithContext("Provider", provider)
            .WithContext("IsRetryable", isRetryable);
    }

    public LLMProviderException(string message, string provider, Exception innerException, bool isRetryable = false)
        : base(message, innerException, "LLM_PROVIDER_ERROR", ErrorSeverity.Error)
    {
        Provider = provider;
        IsRetryable = isRetryable;
        WithContext("Provider", provider)
            .WithContext("IsRetryable", isRetryable);
    }
}