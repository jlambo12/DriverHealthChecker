namespace DriverHealthChecker.App;

public class DriverItem
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string CategoryDisplay { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Version { get; set; } = "";
    public string Date { get; set; } = "";
    public string Status { get; set; } = "";
    public string DetectionReason { get; set; } = "";
    public string ButtonText { get; set; } = "Открыть";
    public string ButtonTooltip { get; set; } = "Открыть действие";
    public OfficialAction OfficialAction { get; set; } = OfficialAction.ForMessage("Открыть", "Источник не задан.", "Открыть действие");
}
