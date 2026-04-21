using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal sealed class DriverScanBuildResult
{
    public List<DriverItem> SelectedDrivers { get; init; } = new();
    public List<DriverItem> HiddenDrivers { get; init; } = new();
    public List<DriverVerificationObservation> VerificationObservations { get; init; } = new();
}
