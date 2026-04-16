namespace DriverHealthChecker.App;

internal static class DriverRules
{
    public const string NvidiaAppUrl = "https://www.nvidia.com/en-us/software/nvidia-app/";
    public const string AmdDriversUrl = "https://www.amd.com/en/support/download/drivers.html";
    public const string IntelSupportAssistantUrl = "https://www.intel.com/content/www/us/en/support/detect.html";
    public const string GoogleSearchUrlPrefix = "https://www.google.com/search?q=";
    public const string OfficialDriverSiteSearchSuffix = " official driver site";

    public static readonly string[] BlacklistedTerms =
    {
        "virtual audio", "nvidia virtual audio", "audio endpoint", "endpoint", "wan miniport",
        "miniport", "kernel debug", "debug network", "ndis virtual", "storage spaces",
        "pci express root port", "root port", "host bridge", "programmable interrupt",
        "standard system", "hid-", "composite", "gpio", "spi", "i2c", "usb xhci",
        "processor", "pci standard", "usb input", "human interface"
    };

    public static readonly string[] ExternalAudioBrands =
    {
        "focusrite", "sound blaster", "creative", "xonar", "steinberg",
        "motu", "audient", "rme", "universal audio", "presonus", "scarlett"
    };
}
