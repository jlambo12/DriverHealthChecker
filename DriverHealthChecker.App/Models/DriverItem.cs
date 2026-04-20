namespace DriverHealthChecker.App;

public class DriverItem
{
    private DriverCategory _categoryKind;
    private DriverHealthStatus _statusKind;
    private string _category = string.Empty;
    private string _categoryDisplay = string.Empty;
    private string _status = string.Empty;

    public string Name { get; set; } = "";

    public DriverCategory CategoryKind
    {
        get => _categoryKind;
        set
        {
            _categoryKind = value;
            _category = DriverTextMapper.ToCategoryCode(value);
            _categoryDisplay = DriverTextMapper.ToCategoryDisplay(value);
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            _category = value ?? string.Empty;
            var parsed = DriverTextMapper.ParseCategoryCode(_category);
            _categoryKind = parsed;
            if (parsed != DriverCategory.Unknown)
                _categoryDisplay = DriverTextMapper.ToCategoryDisplay(parsed);
        }
    }

    public string CategoryDisplay
    {
        get => _categoryDisplay;
        set => _categoryDisplay = value ?? string.Empty;
    }

    public string Manufacturer { get; set; } = "";
    public string Version { get; set; } = "";
    public string Date { get; set; } = "";

    public DriverHealthStatus StatusKind
    {
        get => _statusKind;
        set
        {
            _statusKind = value;
            _status = DriverTextMapper.ToStatusDisplay(value);
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value ?? string.Empty;
            _statusKind = DriverTextMapper.ParseStatusDisplay(_status);
        }
    }

    public string DetectionReason { get; set; } = "";
    public string ButtonText { get; set; } = "Открыть";
    public string ButtonTooltip { get; set; } = "Открыть действие";
    public OfficialAction OfficialAction { get; set; } = OfficialAction.ForMessage("Открыть", "Источник не задан.", "Открыть действие");
}
