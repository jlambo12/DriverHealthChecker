using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace DriverHealthChecker.App;

internal interface IOfficialActionResolver
{
    OfficialAction Resolve(string name, string? manufacturer, string category, string? oemManufacturer = null, bool isLaptop = false);
}

internal sealed class OfficialActionResolver : IOfficialActionResolver
{
    public OfficialAction Resolve(string name, string? manufacturer, string category, string? oemManufacturer = null, bool isLaptop = false)
    {
        var n = name.ToLowerInvariant();
        var m = (manufacturer ?? string.Empty).ToLowerInvariant();
        var oem = (oemManufacturer ?? string.Empty).ToLowerInvariant();

        if (category == "GPU" && (n.Contains("nvidia") || n.Contains("geforce")))
        {
            var appPath = FindNvidiaApp();
            if (!string.IsNullOrWhiteSpace(appPath))
                return OfficialAction.ForLocalApp(appPath, "NVIDIA App", "Открыть установленное приложение NVIDIA");

            return OfficialAction.ForUrl(
                DriverRules.NvidiaAppUrl,
                "Скачать NVIDIA App",
                "Открыть официальный сайт для установки NVIDIA App");
        }

        if (category == "GPU" && n.Contains("radeon"))
        {
            return OfficialAction.ForUrl(
                DriverRules.AmdDriversUrl,
                "Сайт AMD",
                "Открыть официальный сайт AMD");
        }

        if (category == "Network" && m.Contains("intel"))
        {
            if (n.Contains("bluetooth"))
                return OfficialAction.ForUrl(DriverRules.IntelBluetoothDriversUrl, "Intel Bluetooth", "Открыть официальный драйвер Intel Bluetooth");

            if (n.Contains("wi-fi") || n.Contains("wireless") || n.Contains("wlan"))
                return OfficialAction.ForUrl(DriverRules.IntelWirelessDriversUrl, "Intel Wi-Fi", "Открыть официальный драйвер Intel Wi-Fi");

            return OfficialAction.ForUrl(
                DriverRules.IntelSupportAssistantUrl,
                "Intel Tool",
                "Открыть Intel Driver & Support Assistant");
        }

        if (category == "Network" && (m.Contains("realtek") || n.Contains("realtek")))
        {
            return OfficialAction.ForUrl(
                DriverRules.RealtekDownloadsUrl,
                "Realtek Downloads",
                "Открыть официальный центр загрузок Realtek");
        }

        if (category == "Storage")
        {
            if (m.Contains("intel") || n.Contains("intel") || n.Contains("rst") || n.Contains("vmd"))
            {
                return OfficialAction.ForUrl(
                    DriverRules.IntelRstDriversUrl,
                    "Intel RST",
                    "Открыть официальный драйвер Intel Rapid Storage Technology");
            }

            if (IsOemLaptopAudioOrSupportComponent(n, oem, isLaptop))
            {
                return OfficialAction.ForUrl(
                    DriverRules.HuaweiPcManagerUrl,
                    "Huawei PC Manager",
                    "Для OEM-ноутбука безопаснее использовать Huawei PC Manager");
            }

            return OfficialAction.ForMessage(
                "OEM/вендор",
                "Для этого контроллера надёжнее использовать поддержку OEM устройства или официальную страницу производителя контроллера.",
                "Показать безопасную рекомендацию без универсального перенаправления");
        }

        if (category == "AudioMain" || category == "AudioExternal")
        {
            if (IsOemLaptopAudioOrSupportComponent(n, oem, isLaptop))
            {
                return OfficialAction.ForUrl(
                    DriverRules.HuaweiPcManagerUrl,
                    "Huawei PC Manager",
                    "Для OEM-зависимого аудио используйте инструмент производителя ноутбука");
            }

            if (m.Contains("realtek") || n.Contains("realtek"))
            {
                return OfficialAction.ForUrl(
                    DriverRules.RealtekDownloadsUrl,
                    "Realtek Downloads",
                    "Открыть официальный центр загрузок Realtek");
            }

            return OfficialAction.ForMessage(
                "OEM/вендор",
                "Для аудио-драйверов используйте OEM-поддержку устройства или официальный сайт производителя аудиочипа.",
                "Показать безопасную рекомендацию по обновлению");
        }

        if (category == "DeviceRecommendation")
        {
            if (oem.Contains("huawei") || oem.Contains("honor"))
            {
                return OfficialAction.ForUrl(DriverRules.HuaweiPcManagerUrl, "Huawei PC Manager", "Открыть официальный инструмент Huawei");
            }
        }

        AppLogger.Info($"Official action fallback used. category={category}, name={name}, manufacturer={manufacturer ?? "-"}.");
        return OfficialAction.ForMessage(
            "OEM/вендор",
            "Для этого устройства точный официальный источник пока не определён. Используйте OEM-поддержку устройства или сайт производителя устройства.",
            "Показать безопасную рекомендацию");
    }

    private static bool IsOemLaptopAudioOrSupportComponent(string name, string oemManufacturer, bool isLaptop)
    {
        if (!isLaptop)
            return false;

        var isHuawei = oemManufacturer.Contains("huawei") || oemManufacturer.Contains("honor");
        if (!isHuawei)
            return false;

        return name.Contains("huawei audio service")
               || name.Contains("hwve")
               || name.Contains("nahimic")
               || name.Contains("elan")
               || name.Contains("smbus");
    }

    private static string? FindNvidiaApp()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA app", "CEF", "NVIDIA App.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NVIDIA Corporation", "NVIDIA app", "CEF", "NVIDIA App.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA app", "NVIDIA App.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NVIDIA Corporation", "NVIDIA app", "NVIDIA App.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA GeForce Experience", "NVIDIA GeForce Experience.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NVIDIA Corporation", "NVIDIA GeForce Experience", "NVIDIA GeForce Experience.exe")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return FindNvidiaFromRegistry();
    }

    private static string? FindNvidiaFromRegistry()
    {
        var roots = new[]
        {
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
        };

        foreach (var root in roots)
        {
            if (root == null)
                continue;

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                try
                {
                    using var subKey = root.OpenSubKey(subKeyName);
                    if (subKey == null)
                        continue;

                    var displayName = subKey.GetValue("DisplayName")?.ToString() ?? "";
                    if (!displayName.Contains("NVIDIA App", StringComparison.OrdinalIgnoreCase) &&
                        !displayName.Contains("GeForce Experience", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var displayIcon = NormalizeExecutablePath(subKey.GetValue("DisplayIcon")?.ToString());
                    if (!string.IsNullOrWhiteSpace(displayIcon) && File.Exists(displayIcon))
                        return displayIcon;

                    var installLocation = subKey.GetValue("InstallLocation")?.ToString();
                    if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
                    {
                        var exe = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly)
                            .FirstOrDefault(f =>
                                Path.GetFileName(f).Contains("NVIDIA App", StringComparison.OrdinalIgnoreCase) ||
                                Path.GetFileName(f).Contains("GeForce Experience", StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrWhiteSpace(exe))
                            return exe;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(
                        "Ошибка при чтении записи реестра NVIDIA/GeForce Experience.",
                        ex);
                }
            }
        }

        return null;
    }

    private static string? NormalizeExecutablePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        var cleaned = rawPath.Trim().Trim('"');
        var commaIndex = cleaned.IndexOf(',');
        if (commaIndex > 0)
            cleaned = cleaned[..commaIndex];

        return cleaned;
    }
}
