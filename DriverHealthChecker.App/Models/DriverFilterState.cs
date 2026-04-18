namespace DriverHealthChecker.App;

internal sealed class DriverFilterState
{
    public string SelectedCategory { get; init; } = "Все";
    public string SelectedStatus { get; init; } = "Все";
    public string Search { get; init; } = string.Empty;
    public bool ShowHidden { get; init; }
}
