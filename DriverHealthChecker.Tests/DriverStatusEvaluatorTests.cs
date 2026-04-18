using System;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverStatusEvaluatorTests
{
    private readonly IDriverStatusEvaluator _evaluator = new DriverStatusEvaluator();

    [Fact]
    public void EvaluateStatus_InvalidDate_ReturnsCheckStatus()
    {
        var status = _evaluator.EvaluateStatus("not-a-date");
        Assert.Equal("Стоит проверить", status);
    }

    [Fact]
    public void EvaluateStatus_RecentDate_ReturnsActual()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"));
        Assert.Equal("Актуален", status);
    }

    [Fact]
    public void EvaluateStatus_MidAgeDate_ReturnsCheckStatus()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Now.AddDays(-700).ToString("yyyy-MM-dd"));
        Assert.Equal("Стоит проверить", status);
    }

    [Fact]
    public void EvaluateStatus_OldDate_ReturnsAttention()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Now.AddDays(-1500).ToString("yyyy-MM-dd"));
        Assert.Equal("Требует внимания", status);
    }
}
