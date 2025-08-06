namespace CodeAgent.Providers.Ollama;

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string? DefaultModel { get; set; } = "llama3.2";
}