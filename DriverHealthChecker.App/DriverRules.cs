namespace DriverHealthChecker.App;

internal static class DriverRules
{
    // Official vendor endpoints used by "official action" buttons.
    public const string NvidiaAppUrl = "https://www.nvidia.com/en-us/software/nvidia-app/";
    public const string AmdDriversUrl = "https://www.amd.com/en/support/download/drivers.html";
    public const string IntelSupportAssistantUrl = "https://www.intel.com/content/www/us/en/support/detect.html";
    public const string GoogleSearchUrlPrefix = "https://www.google.com/search?q=";
    public const string OfficialDriverSiteSearchSuffix = " official driver site";
    public const string LenovoVantageUrl = "https://apps.microsoft.com/detail/9wzdncrfj4mv";
    public const string AsusMyAsusUrl = "https://apps.microsoft.com/detail/9n7r5s6b0zzh";
    public const string HpSupportAssistantUrl = "https://support.hp.com/us-en/help/hp-support-assistant";
    public const string DellSupportAssistUrl = "https://www.dell.com/support/home/en-us/product-support/product/supportassist-pcs-tablets/drivers";
    public const string AcerCareCenterUrl = "https://www.acer.com/us-en/support";
    public const string MsiCenterUrl = "https://www.msi.com/Landing/MSI-Center";
    public const string HuaweiPcManagerUrl = "https://consumer.huawei.com/en/support/pc-manager/";

    // Blacklist is intentionally split into groups so it is easy to maintain and reason about
    // why a device was hidden from the main user-facing list.
    public static readonly BlacklistGroup[] BlacklistGroups =
    {
        new(
            "системный шум",
            [
                "wan miniport", "miniport", "pci express root port", "root port", "host bridge",
                "programmable interrupt", "standard system", "processor", "pci standard", "storage spaces"
            ]),
        new(
            "виртуальные устройства",
            [
                "virtual audio", "nvidia virtual audio", "ndis virtual", "audio endpoint", "endpoint"
            ]),
        new(
            "служебные компоненты",
            [
                "kernel debug", "debug network", "hid-", "composite", "gpio", "spi", "i2c",
                "usb xhci", "usb input", "human interface"
            ])
    };

    // Brand anchors for external USB/audio interfaces.
    public static readonly string[] ExternalAudioBrands =
    {
        "focusrite", "sound blaster", "creative", "xonar", "steinberg",
        "motu", "audient", "rme", "universal audio", "presonus", "scarlett"
    };

    // GPU category terms are centralized here so classifier logic stays deterministic.
    public static readonly KeywordRule[] GpuKeywordRules =
    {
        new("nvidia", "GPU: найдено совпадение по NVIDIA"),
        new("geforce", "GPU: найдено совпадение по GeForce"),
        new("radeon", "GPU: найдено совпадение по Radeon"),
        new("intel(r) uhd", "GPU: найдено совпадение по Intel graphics"),
        new("intel(r) iris", "GPU: найдено совпадение по Intel graphics"),
        new("intel arc", "GPU: найдено совпадение по Intel graphics")
    };

    // Network-category anchors (Ethernet / Wi-Fi / Bluetooth and vendor hints).
    public static readonly string[] NetworkTerms =
    {
        "ethernet", "wi-fi", "wireless", "wlan", "bluetooth", "killer",
        "gigabit", "gbe family", "802.11", "mediatek", "qualcomm",
        "intel(r) ethernet", "lan", "2.5gbe", "5gbe", "10gbe",
        "marvell", "aquantia", "broadcom"
    };

    // Storage-controller anchors (NVMe/SATA/RAID/VMD etc.).
    public static readonly string[] StorageTerms =
    {
        "nvme", "sata ahci", "raid", "rst", "vmd", "storage controller",
        "scsiadapter", "sas", "ahci", "u.2", "emmc", "ufs"
    };

    // Built-in/main audio anchors.
    public static readonly string[] MainAudioTerms =
    {
        "realtek audio", "intel smart sound", "high definition audio",
        "audio codec", "cirrus logic", "conexant", "nahimic"
    };

    // OEM laptop mapping for recommendation banner/action.
    public static readonly LaptopOemRule[] LaptopOemRules =
    {
        new(["lenovo"], "Lenovo", LenovoVantageUrl, "Lenovo Vantage"),
        new(["asus", "asustek"], "ASUS", AsusMyAsusUrl, "MyASUS"),
        new(["hp", "hewlett"], "HP", HpSupportAssistantUrl, "HP Support Assistant"),
        new(["dell", "alienware"], "Dell", DellSupportAssistUrl, "Dell SupportAssist"),
        new(["acer"], "Acer", AcerCareCenterUrl, "Acer Care Center"),
        new(["msi", "micro-star"], "MSI", MsiCenterUrl, "MSI Center"),
        new(["huawei", "honor"], "Huawei/Honor", HuaweiPcManagerUrl, "Huawei PC Manager")
    };
}

internal sealed record BlacklistGroup(string GroupName, string[] Terms);
internal sealed record KeywordRule(string Term, string Reason);
internal sealed record LaptopOemRule(string[] ManufacturerKeywords, string DisplayVendor, string Url, string ButtonText);
