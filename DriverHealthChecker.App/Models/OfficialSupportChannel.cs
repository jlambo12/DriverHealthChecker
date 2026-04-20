namespace DriverHealthChecker.App;

internal sealed class OfficialSupportChannel
{
    public OfficialSupportChannelType Type { get; init; } = OfficialSupportChannelType.ManualExplanation;
    public string Target { get; init; } = string.Empty;
    public bool IsInstalled { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
