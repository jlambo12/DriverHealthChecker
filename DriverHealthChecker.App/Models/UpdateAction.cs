namespace DriverHealthChecker.App;

internal sealed class UpdateAction
{
    public OfficialSupportChannel Channel { get; init; } = new();
    public UpdateActionType ActionType { get; init; } = UpdateActionType.ShowExplanation;
    public string DisplayText { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
}
