namespace CodeAgent.Providers.Models;

public class Usage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public decimal? PromptCost { get; set; }
    public decimal? CompletionCost { get; set; }
    public decimal? TotalCost { get; set; }
}