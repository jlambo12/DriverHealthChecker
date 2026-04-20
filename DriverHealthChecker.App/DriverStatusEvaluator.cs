using System;

namespace DriverHealthChecker.App;

internal interface IDriverStatusEvaluator
{
    DriverHealthStatus EvaluateStatus(string formattedDate);
}

internal sealed class DriverStatusEvaluator : IDriverStatusEvaluator
{
    public DriverHealthStatus EvaluateStatus(string formattedDate)
    {
        if (!DateTime.TryParse(formattedDate, out var driverDate))
            return DriverHealthStatus.NeedsReview;

        var ageInDays = (DateTime.Today - driverDate.Date).TotalDays;

        if (ageInDays <= 365)
            return DriverHealthStatus.UpToDate;

        if (ageInDays <= 1095)
            return DriverHealthStatus.NeedsReview;

        return DriverHealthStatus.NeedsAttention;
    }
}
