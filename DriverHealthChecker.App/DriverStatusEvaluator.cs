using System;

namespace DriverHealthChecker.App;

internal interface IDriverStatusEvaluator
{
    string EvaluateStatus(string formattedDate);
}

internal sealed class DriverStatusEvaluator : IDriverStatusEvaluator
{
    public string EvaluateStatus(string formattedDate)
    {
        if (!DateTime.TryParse(formattedDate, out var driverDate))
            return "Стоит проверить";

        var ageInDays = (DateTime.Now - driverDate).TotalDays;

        if (ageInDays <= 365)
            return "Актуален";

        if (ageInDays <= 1095)
            return "Стоит проверить";

        return "Требует внимания";
    }
}
