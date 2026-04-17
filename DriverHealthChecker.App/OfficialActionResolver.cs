using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace DriverHealthChecker.App;

internal interface IOfficialActionResolver
{
    OfficialAction Resolve(string name, string? manufacturer, string category);
}

internal sealed class OfficialActionResolver : IOfficialActionResolver
{
    public OfficialAction Resolve(string name, string? manufacturer, string category)
    {
        var n = name.ToLowerInvariant();
        var m = (manufacturer ?? string.Empty).ToLowerInvariant();

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
            return OfficialAction.ForUrl(
                DriverRules.IntelSupportAssistantUrl,
                "Intel Tool",
                "Открыть Intel Driver & Support Assistant");
        }

        if (category == "Network")
        {
            return OfficialAction.ForSearch(
                name + DriverRules.OfficialDriverSiteSearchSuffix,
                "Найти драйвер",
                "Открыть поиск официального драйвера по модели устройства");
        }

        if (category == "Storage")
        {
            if (m.Contains("intel") || n.Contains("intel"))
            {
                return OfficialAction.ForUrl(
                    DriverRules.IntelSupportAssistantUrl,
                    "Intel Tool",
                    "Открыть Intel Driver & Support Assistant");
            }

            return OfficialAction.ForWindowsUpdate(
                "Windows Update",
                "Открыть Windows Update",
                "Перейти в Windows Update для безопасной проверки системных обновлений");
        }

        if (category == "AudioMain" || category == "AudioExternal")
        {
            if (m.Contains("realtek") || n.Contains("realtek"))
            {
                return OfficialAction.ForSearch(
                    name + DriverRules.OfficialDriverSiteSearchSuffix,
                    "Найти драйвер",
                    "Открыть поиск официального драйвера Realtek");
            }

            return OfficialAction.ForMessage(
                "Как обновить",
                "Для аудио-драйверов в первой версии лучше использовать сайт производителя устройства или производителя ноутбука/материнской платы.",
                "Показать безопасную рекомендацию по обновлению");
        }

        return OfficialAction.ForMessage(
            "Открыть",
            "Для этого устройства точный официальный источник в первой версии ещё не настроен.",
            "Показать информационное сообщение");
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
