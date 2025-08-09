using CodeAgent.Projects.Interfaces;
using CodeAgent.Projects.Models;
using CodeAgent.Projects.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodeAgent.Projects.Tests;

public class CostTrackerTests
{
    private readonly Mock<ILogger<CostTracker>> _loggerMock;
    private readonly Mock<IProjectService> _projectServiceMock;
    private readonly CostTracker _sut;
    private readonly Guid _projectId = Guid.NewGuid();

    public CostTrackerTests()
    {
        _loggerMock = new Mock<ILogger<CostTracker>>();
        _projectServiceMock = new Mock<IProjectService>();
        _sut = new CostTracker(_loggerMock.Object, _projectServiceMock.Object);
    }

    [Fact]
    public async Task CalculateCostAsync_OpenAIGPT4_ShouldCalculateCorrectly()
    {
        var result = await _sut.CalculateCostAsync("openai", "gpt-4", 1000, 500);

        result.TotalCost.Should().BeApproximately(0.06m, 0.001m); // (1000/1000 * 0.03) + (500/1000 * 0.06)
        result.InputTokens.Should().Be(1000);
        result.OutputTokens.Should().Be(500);
        result.ProviderCosts.Should().ContainKey("openai");
    }

    [Fact]
    public async Task CalculateCostAsync_UnknownModel_ShouldUseDefaultRate()
    {
        var result = await _sut.CalculateCostAsync("unknown", "unknown-model", 1000, 1000);

        result.TotalCost.Should().BeApproximately(0.03m, 0.001m); // Default: 0.01 + 0.02 per 1k tokens
    }

    [Fact]
    public async Task CheckBudgetAsync_UnderLimit_ShouldReturnTrue()
    {
        var project = CreateProjectWithLimits(maxPerRun: 10m);
        _projectServiceMock.Setup(x => x.GetProjectAsync(_projectId, default))
            .ReturnsAsync(project);
        _projectServiceMock.Setup(x => x.GetProjectStateAsync(_projectId, default))
            .ReturnsAsync(new ProjectState());

        var result = await _sut.CheckBudgetAsync(_projectId, 5m);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckBudgetAsync_ExceedsRunLimit_ShouldReturnFalse()
    {
        var project = CreateProjectWithLimits(maxPerRun: 10m);
        _projectServiceMock.Setup(x => x.GetProjectAsync(_projectId, default))
            .ReturnsAsync(project);
        _projectServiceMock.Setup(x => x.GetProjectStateAsync(_projectId, default))
            .ReturnsAsync(new ProjectState());

        bool alertRaised = false;
        _sut.CostAlertRaised += (s, e) => alertRaised = true;

        var result = await _sut.CheckBudgetAsync(_projectId, 15m);

        result.Should().BeFalse();
        alertRaised.Should().BeTrue();
    }

    [Fact]
    public async Task CheckBudgetAsync_ExceedsDailyLimit_ShouldReturnFalse()
    {
        var project = CreateProjectWithLimits(maxPerDay: 20m);
        var state = new ProjectState
        {
            CostSummary = new CostSummary 
            { 
                TodayCost = 15m,
                LastUpdated = DateTime.UtcNow
            }
        };
        
        _projectServiceMock.Setup(x => x.GetProjectAsync(_projectId, default))
            .ReturnsAsync(project);
        _projectServiceMock.Setup(x => x.GetProjectStateAsync(_projectId, default))
            .ReturnsAsync(state);

        var result = await _sut.CheckBudgetAsync(_projectId, 10m);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckBudgetAsync_ApproachingLimit_ShouldRaiseWarning()
    {
        var project = CreateProjectWithLimits(maxPerDay: 100m);
        var state = new ProjectState
        {
            CostSummary = new CostSummary 
            { 
                TodayCost = 75m,
                LastUpdated = DateTime.UtcNow
            }
        };
        
        _projectServiceMock.Setup(x => x.GetProjectAsync(_projectId, default))
            .ReturnsAsync(project);
        _projectServiceMock.Setup(x => x.GetProjectStateAsync(_projectId, default))
            .ReturnsAsync(state);

        CostAlertEventArgs? alertArgs = null;
        _sut.CostAlertRaised += (s, e) => alertArgs = e;

        var result = await _sut.CheckBudgetAsync(_projectId, 10m);

        result.Should().BeTrue();
        alertArgs.Should().NotBeNull();
        alertArgs!.Level.Should().Be(CostAlertLevel.Warning);
    }

    [Fact]
    public async Task RecordCostAsync_ShouldUpdateProjectState()
    {
        var state = new ProjectState();
        _projectServiceMock.Setup(x => x.GetProjectStateAsync(_projectId, default))
            .ReturnsAsync(state);

        var cost = new RunCost
        {
            TotalCost = 5m,
            InputTokens = 1000,
            OutputTokens = 500
        };

        await _sut.RecordCostAsync(_projectId, Guid.NewGuid(), cost);

        _projectServiceMock.Verify(x => x.UpdateProjectStateAsync(
            _projectId, 
            It.Is<ProjectState>(s => 
                s.CostSummary.TotalCost == 5m &&
                s.CostSummary.TotalTokens == 1500 &&
                s.CostSummary.TodayCost == 5m),
            default), 
            Times.Once);
    }

    [Fact]
    public async Task GetCostSummaryAsync_NewDay_ShouldResetDailyCosts()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var state = new ProjectState
        {
            CostSummary = new CostSummary
            {
                TotalCost = 100m,
                TodayCost = 50m,
                MonthCost = 75m,
                LastUpdated = yesterday
            }
        };
        
        _projectServiceMock.Setup(x => x.GetProjectStateAsync(_projectId, default))
            .ReturnsAsync(state);

        var result = await _sut.GetCostSummaryAsync(_projectId);

        result.TodayCost.Should().Be(0);
        result.TotalCost.Should().Be(100m);
        result.MonthCost.Should().Be(75m);
    }

    [Fact]
    public async Task UpdateProviderRatesAsync_ShouldUpdateRates()
    {
        var newRates = new Dictionary<string, Dictionary<string, decimal>>
        {
            ["custom"] = new Dictionary<string, decimal>
            {
                ["model1"] = 0.05m
            }
        };

        await _sut.UpdateProviderRatesAsync(newRates);
        var cost = await _sut.CalculateCostAsync("custom", "model1", 1000, 1000);

        cost.TotalCost.Should().BeApproximately(0.1m, 0.001m); // 2000 tokens * 0.05
    }

    private Project CreateProjectWithLimits(decimal? maxPerRun = null, decimal? maxPerDay = null, decimal? maxPerMonth = null)
    {
        return new Project
        {
            Id = _projectId,
            Configuration = new ProjectConfiguration
            {
                CostLimits = new CostConfiguration
                {
                    EnableCostTracking = true,
                    MaxCostPerRun = maxPerRun,
                    MaxCostPerDay = maxPerDay,
                    MaxCostPerMonth = maxPerMonth,
                    AlertLevel = CostAlertLevel.Warning
                }
            }
        };
    }
}