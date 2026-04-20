namespace DriverHealthChecker.App;

internal sealed class DriverUpdateAction
{
    public OfficialSupportChannel Channel { get; init; } = new();
    public DriverUpdateActionType ActionType { get; init; } = DriverUpdateActionType.ShowExplanation;
    public string DisplayText { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}
