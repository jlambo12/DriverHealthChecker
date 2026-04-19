using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace DriverHealthChecker.App;

internal interface INvidiaAppLocator
{
    string? FindInstalledAppPath();
}

internal sealed class NvidiaAppLocator : INvidiaAppLocator
{
    public string? FindInstalledAppPath()
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

        return FindFromRegistry();
    }

    private static string? FindFromRegistry()
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

                    var displayName = subKey.GetValue("DisplayName")?.ToString() ?? string.Empty;
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
                    AppLogger.Error("Ошибка при чтении записи реестра NVIDIA/GeForce Experience.", ex);
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
