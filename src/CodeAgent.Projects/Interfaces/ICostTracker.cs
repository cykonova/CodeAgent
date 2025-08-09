using CodeAgent.Projects.Models;

namespace CodeAgent.Projects.Interfaces;

public interface ICostTracker
{
    Task<RunCost> CalculateCostAsync(string providerId, string model, int inputTokens, int outputTokens);
    Task<bool> CheckBudgetAsync(Guid projectId, decimal estimatedCost);
    Task RecordCostAsync(Guid projectId, Guid runId, RunCost cost, CancellationToken cancellationToken = default);
    Task<CostSummary> GetCostSummaryAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RunCost>> GetRunCostsAsync(Guid projectId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<decimal> GetProviderRateAsync(string providerId, string model);
    Task UpdateProviderRatesAsync(Dictionary<string, Dictionary<string, decimal>> rates);
    Task<bool> EnforceBudgetLimitsAsync(Guid projectId, CostConfiguration limits);
    event EventHandler<CostAlertEventArgs> CostAlertRaised;
}

public class CostAlertEventArgs : EventArgs
{
    public Guid ProjectId { get; set; }
    public CostAlertLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal CurrentCost { get; set; }
    public decimal? Limit { get; set; }
    public string LimitType { get; set; } = string.Empty; // "run", "day", "month"
}