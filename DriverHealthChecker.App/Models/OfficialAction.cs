namespace DriverHealthChecker.App;

public enum OfficialActionKind
{
    None,
    Url,
    LocalApp,
    WindowsUpdate
}

public class OfficialAction
{
    public OfficialActionKind Kind { get; set; }
    public string Target { get; set; } = "";
    public string Message { get; set; } = "";
    public string ButtonText { get; set; } = "Открыть";
    public string Tooltip { get; set; } = "Открыть действие";

    public static OfficialAction ForUrl(string url, string buttonText, string tooltip)
    {
        return new OfficialAction
        {
            Kind = OfficialActionKind.Url,
            Target = url,
            Message = buttonText,
            ButtonText = buttonText,
            Tooltip = tooltip
        };
    }

    public static OfficialAction ForLocalApp(string path, string buttonText, string tooltip)
    {
        return new OfficialAction
        {
            Kind = OfficialActionKind.LocalApp,
            Target = path,
            Message = buttonText,
            ButtonText = buttonText,
            Tooltip = tooltip
        };
    }

    public static OfficialAction ForWindowsUpdate(string buttonText, string message, string tooltip)
    {
        return new OfficialAction
        {
            Kind = OfficialActionKind.WindowsUpdate,
            Message = message,
            ButtonText = buttonText,
            Tooltip = tooltip
        };
    }

    public static OfficialAction ForMessage(string buttonText, string message, string tooltip)
    {
        return new OfficialAction
        {
            Kind = OfficialActionKind.None,
            Message = message,
            ButtonText = buttonText,
            Tooltip = tooltip
        };
    }
}
