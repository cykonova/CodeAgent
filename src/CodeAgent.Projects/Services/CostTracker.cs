using System.Collections.Concurrent;
using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Models;
using Microsoft.Extensions.Logging;

namespace CodeAgent.Projects.Services;

public class CostTracker : ICostTracker
{
    private readonly ILogger<CostTracker> _logger;
    private readonly IProjectService _projectService;
    private readonly ConcurrentDictionary<Guid, List<RunCost>> _projectCosts = new();
    private readonly Dictionary<string, Dictionary<string, ProviderRate>> _providerRates;

    public event EventHandler<CostAlertEventArgs>? CostAlertRaised;

    public CostTracker(ILogger<CostTracker> logger, IProjectService projectService)
    {
        _logger = logger;
        _projectService = projectService;
        _providerRates = InitializeProviderRates();
    }

    public Task<RunCost> CalculateCostAsync(string providerId, string model, int inputTokens, int outputTokens)
    {
        var rate = GetRateForModel(providerId, model);
        
        var inputCost = (inputTokens / 1000.0m) * rate.InputPricePerThousand;
        var outputCost = (outputTokens / 1000.0m) * rate.OutputPricePerThousand;
        var totalCost = inputCost + outputCost;

        var runCost = new RunCost
        {
            TotalCost = totalCost,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            ProviderCosts = new Dictionary<string, ProviderCost>
            {
                [providerId] = new ProviderCost
                {
                    ProviderId = providerId,
                    Model = model,
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    Cost = totalCost
                }
            }
        };

        return Task.FromResult(runCost);
    }

    public async Task<bool> CheckBudgetAsync(Guid projectId, decimal estimatedCost)
    {
        var project = await _projectService.GetProjectAsync(projectId);
        if (project == null)
        {
            return false;
        }

        var config = project.Configuration.CostLimits;
        if (!config.EnableCostTracking)
        {
            return true;
        }

        var summary = await GetCostSummaryAsync(projectId);

        // Check run limit
        if (config.MaxCostPerRun.HasValue && estimatedCost > config.MaxCostPerRun.Value)
        {
            RaiseCostAlert(projectId, CostAlertLevel.Error, 
                $"Estimated cost ${estimatedCost:F2} exceeds run limit ${config.MaxCostPerRun.Value:F2}",
                estimatedCost, config.MaxCostPerRun.Value, "run");
            return false;
        }

        // Check daily limit
        if (config.MaxCostPerDay.HasValue && (summary.TodayCost + estimatedCost) > config.MaxCostPerDay.Value)
        {
            RaiseCostAlert(projectId, CostAlertLevel.Error,
                $"Adding ${estimatedCost:F2} would exceed daily limit ${config.MaxCostPerDay.Value:F2}",
                summary.TodayCost + estimatedCost, config.MaxCostPerDay.Value, "day");
            return false;
        }

        // Check monthly limit
        if (config.MaxCostPerMonth.HasValue && (summary.MonthCost + estimatedCost) > config.MaxCostPerMonth.Value)
        {
            RaiseCostAlert(projectId, CostAlertLevel.Error,
                $"Adding ${estimatedCost:F2} would exceed monthly limit ${config.MaxCostPerMonth.Value:F2}",
                summary.MonthCost + estimatedCost, config.MaxCostPerMonth.Value, "month");
            return false;
        }

        // Check warning thresholds (80% of limits)
        if (config.AlertLevel >= CostAlertLevel.Warning)
        {
            if (config.MaxCostPerDay.HasValue && 
                (summary.TodayCost + estimatedCost) > config.MaxCostPerDay.Value * 0.8m)
            {
                RaiseCostAlert(projectId, CostAlertLevel.Warning,
                    $"Approaching daily limit (80% threshold)",
                    summary.TodayCost + estimatedCost, config.MaxCostPerDay.Value, "day");
            }

            if (config.MaxCostPerMonth.HasValue && 
                (summary.MonthCost + estimatedCost) > config.MaxCostPerMonth.Value * 0.8m)
            {
                RaiseCostAlert(projectId, CostAlertLevel.Warning,
                    $"Approaching monthly limit (80% threshold)",
                    summary.MonthCost + estimatedCost, config.MaxCostPerMonth.Value, "month");
            }
        }

        return true;
    }

    public async Task RecordCostAsync(Guid projectId, Guid runId, RunCost cost, CancellationToken cancellationToken = default)
    {
        if (!_projectCosts.ContainsKey(projectId))
        {
            _projectCosts[projectId] = new List<RunCost>();
        }

        _projectCosts[projectId].Add(cost);

        // Update project state
        var state = await _projectService.GetProjectStateAsync(projectId, cancellationToken);
        state.CostSummary.TotalCost += cost.TotalCost;
        state.CostSummary.TotalTokens += cost.InputTokens + cost.OutputTokens;
        
        var today = DateTime.UtcNow.Date;
        if (state.CostSummary.LastUpdated.Date != today)
        {
            state.CostSummary.TodayCost = cost.TotalCost;
            state.CostSummary.TodayTokens = cost.InputTokens + cost.OutputTokens;
        }
        else
        {
            state.CostSummary.TodayCost += cost.TotalCost;
            state.CostSummary.TodayTokens += cost.InputTokens + cost.OutputTokens;
        }

        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        if (state.CostSummary.LastUpdated < firstOfMonth)
        {
            state.CostSummary.MonthCost = cost.TotalCost;
            state.CostSummary.MonthTokens = cost.InputTokens + cost.OutputTokens;
        }
        else
        {
            state.CostSummary.MonthCost += cost.TotalCost;
            state.CostSummary.MonthTokens += cost.InputTokens + cost.OutputTokens;
        }

        state.CostSummary.LastUpdated = DateTime.UtcNow;
        
        await _projectService.UpdateProjectStateAsync(projectId, state, cancellationToken);

        _logger.LogInformation("Recorded cost ${Cost:F2} for project {ProjectId}, run {RunId}", 
            cost.TotalCost, projectId, runId);
    }

    public async Task<CostSummary> GetCostSummaryAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var state = await _projectService.GetProjectStateAsync(projectId, cancellationToken);
        
        // Reset daily costs if needed
        var today = DateTime.UtcNow.Date;
        if (state.CostSummary.LastUpdated.Date != today)
        {
            state.CostSummary.TodayCost = 0;
            state.CostSummary.TodayTokens = 0;
        }

        // Reset monthly costs if needed
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        if (state.CostSummary.LastUpdated < firstOfMonth)
        {
            state.CostSummary.MonthCost = 0;
            state.CostSummary.MonthTokens = 0;
        }

        return state.CostSummary;
    }

    public Task<IEnumerable<RunCost>> GetRunCostsAsync(Guid projectId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        if (!_projectCosts.TryGetValue(projectId, out var costs))
        {
            return Task.FromResult(Enumerable.Empty<RunCost>());
        }

        // In a real implementation, costs would be filtered by date
        return Task.FromResult(costs.AsEnumerable());
    }

    public Task<decimal> GetProviderRateAsync(string providerId, string model)
    {
        var rate = GetRateForModel(providerId, model);
        return Task.FromResult((rate.InputPricePerThousand + rate.OutputPricePerThousand) / 2);
    }

    public Task UpdateProviderRatesAsync(Dictionary<string, Dictionary<string, decimal>> rates)
    {
        foreach (var provider in rates)
        {
            if (!_providerRates.ContainsKey(provider.Key))
            {
                _providerRates[provider.Key] = new Dictionary<string, ProviderRate>();
            }

            foreach (var model in provider.Value)
            {
                _providerRates[provider.Key][model.Key] = new ProviderRate
                {
                    InputPricePerThousand = model.Value,
                    OutputPricePerThousand = model.Value
                };
            }
        }

        _logger.LogInformation("Updated provider rates for {ProviderCount} providers", rates.Count);
        return Task.CompletedTask;
    }

    public async Task<bool> EnforceBudgetLimitsAsync(Guid projectId, CostConfiguration limits)
    {
        var project = await _projectService.GetProjectAsync(projectId);
        if (project == null)
        {
            return false;
        }

        project.Configuration.CostLimits = limits;
        await _projectService.UpdateProjectAsync(projectId, project.Configuration);

        _logger.LogInformation("Updated budget limits for project {ProjectId}", projectId);
        return true;
    }

    private Dictionary<string, Dictionary<string, ProviderRate>> InitializeProviderRates()
    {
        return new Dictionary<string, Dictionary<string, ProviderRate>>
        {
            ["openai"] = new Dictionary<string, ProviderRate>
            {
                ["gpt-4"] = new ProviderRate { InputPricePerThousand = 0.03m, OutputPricePerThousand = 0.06m },
                ["gpt-4-32k"] = new ProviderRate { InputPricePerThousand = 0.06m, OutputPricePerThousand = 0.12m },
                ["gpt-3.5-turbo"] = new ProviderRate { InputPricePerThousand = 0.0015m, OutputPricePerThousand = 0.002m },
                ["gpt-3.5-turbo-16k"] = new ProviderRate { InputPricePerThousand = 0.003m, OutputPricePerThousand = 0.004m }
            },
            ["anthropic"] = new Dictionary<string, ProviderRate>
            {
                ["claude-3-opus"] = new ProviderRate { InputPricePerThousand = 0.015m, OutputPricePerThousand = 0.075m },
                ["claude-3-sonnet"] = new ProviderRate { InputPricePerThousand = 0.003m, OutputPricePerThousand = 0.015m },
                ["claude-3-haiku"] = new ProviderRate { InputPricePerThousand = 0.00025m, OutputPricePerThousand = 0.00125m },
                ["claude-2.1"] = new ProviderRate { InputPricePerThousand = 0.008m, OutputPricePerThousand = 0.024m }
            },
            ["ollama"] = new Dictionary<string, ProviderRate>
            {
                ["llama2"] = new ProviderRate { InputPricePerThousand = 0m, OutputPricePerThousand = 0m },
                ["mistral"] = new ProviderRate { InputPricePerThousand = 0m, OutputPricePerThousand = 0m },
                ["codellama"] = new ProviderRate { InputPricePerThousand = 0m, OutputPricePerThousand = 0m }
            }
        };
    }

    private ProviderRate GetRateForModel(string providerId, string model)
    {
        if (_providerRates.TryGetValue(providerId.ToLower(), out var providerRates))
        {
            if (providerRates.TryGetValue(model.ToLower(), out var rate))
            {
                return rate;
            }
        }

        // Default rate if not found
        _logger.LogWarning("No rate found for provider {Provider} model {Model}, using default", providerId, model);
        return new ProviderRate { InputPricePerThousand = 0.01m, OutputPricePerThousand = 0.02m };
    }

    private void RaiseCostAlert(Guid projectId, CostAlertLevel level, string message, decimal currentCost, decimal? limit, string limitType)
    {
        CostAlertRaised?.Invoke(this, new CostAlertEventArgs
        {
            ProjectId = projectId,
            Level = level,
            Message = message,
            CurrentCost = currentCost,
            Limit = limit,
            LimitType = limitType
        });

        _logger.LogWarning("Cost alert for project {ProjectId}: {Level} - {Message}", projectId, level, message);
    }

    private class ProviderRate
    {
        public decimal InputPricePerThousand { get; set; }
        public decimal OutputPricePerThousand { get; set; }
    }
}