namespace DriverHealthChecker.App;

internal sealed class DriverUpdateContext
{
    public DriverIdentity Identity { get; init; } = new();
    public OfficialSupportChannel Channel { get; init; } = new();
    public bool HasVerifiedExactTarget { get; init; }
}
