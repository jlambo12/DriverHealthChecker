namespace DriverHealthChecker.App;

internal enum DriverActionFeedbackKind
{
    None,
    Info,
    Warning
}

internal sealed class DriverActionFeedback
{
    public DriverActionFeedbackKind Kind { get; init; } = DriverActionFeedbackKind.None;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public static DriverActionFeedback None() => new();
}
