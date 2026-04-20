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
        Assert.Equal(DriverHealthStatus.NeedsReview, status);
    }

    [Fact]
    public void EvaluateStatus_RecentDate_ReturnsActual()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd"));
        Assert.Equal(DriverHealthStatus.UpToDate, status);
    }

    [Fact]
    public void EvaluateStatus_MidAgeDate_ReturnsCheckStatus()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Today.AddDays(-700).ToString("yyyy-MM-dd"));
        Assert.Equal(DriverHealthStatus.NeedsReview, status);
    }

    [Fact]
    public void EvaluateStatus_OldDate_ReturnsAttention()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Today.AddDays(-1500).ToString("yyyy-MM-dd"));
        Assert.Equal(DriverHealthStatus.NeedsAttention, status);
    }

    [Fact]
    public void EvaluateStatus_ExactlyOneYearOld_ReturnsUpToDate()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Today.AddDays(-365).ToString("yyyy-MM-dd"));
        Assert.Equal(DriverHealthStatus.UpToDate, status);
    }

    [Fact]
    public void EvaluateStatus_ExactlyThreeYearsOld_ReturnsNeedsReview()
    {
        var status = _evaluator.EvaluateStatus(DateTime.Today.AddDays(-1095).ToString("yyyy-MM-dd"));
        Assert.Equal(DriverHealthStatus.NeedsReview, status);
    }
}
