using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using Button = System.Windows.Controls.Button;

namespace DriverHealthChecker.App
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, DriverSnapshot> _previousSnapshot = new();
        private readonly IDriverStatusEvaluator _statusEvaluator = new DriverStatusEvaluator();
        private readonly IOfficialActionResolver _officialActionResolver = new OfficialActionResolver();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            RunScan(false);
        }

        private void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            RunScan(true);
        }

        private void RunScan(bool isRescan)
        {
            var currentDrivers = ScanImportantDrivers();

            ApplyComparison(currentDrivers, isRescan);

            DriversGrid.ItemsSource = currentDrivers
                .OrderBy(GetCategoryOrder)
                .ThenBy(d => d.Name)
                .ToList();

            UpdateSummary(currentDrivers, isRescan);
            SaveSnapshot(currentDrivers);
        }

        private List<DriverItem> ScanImportantDrivers()
        {
            var allDrivers = new List<DriverItem>();

            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver");

                foreach (ManagementObject obj in searcher.Get())
                {
                    try
                    {
                        var name = obj["DeviceName"]?.ToString();
                        var manufacturer = obj["Manufacturer"]?.ToString();
                        var version = obj["DriverVersion"]?.ToString();
                        var rawDate = obj["DriverDate"]?.ToString();

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        if (!TryGetDriverCategory(name, manufacturer, out var category))
                            continue;

                        var action = _officialActionResolver.Resolve(name, manufacturer, category);

                        allDrivers.Add(new DriverItem
                        {
                            Name = CleanDeviceName(name),
                            Manufacturer = CleanManufacturer(manufacturer),
                            Version = string.IsNullOrWhiteSpace(version) ? "-" : version,
                            Date = FormatDate(rawDate),
                            Category = category,
                            CategoryDisplay = GetCategoryDisplay(category),
                            Status = "Стоит проверить",
                            OfficialAction = action,
                            ButtonText = action.ButtonText,
                            ButtonTooltip = action.Tooltip
                        });
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Не удалось обработать запись драйвера Win32_PnPSignedDriver.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Ошибка во время сканирования драйверов.", ex);

                MessageBox.Show(
                    $"Ошибка при сканировании драйверов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return SelectBestDrivers(allDrivers);
        }

        private void ApplyComparison(List<DriverItem> currentDrivers, bool isRescan)
        {
            foreach (var driver in currentDrivers)
            {
                var key = BuildKey(driver);

                if (isRescan && _previousSnapshot.TryGetValue(key, out var previous))
                {
                    var versionChanged = !string.Equals(previous.Version, driver.Version, StringComparison.OrdinalIgnoreCase);
                    var dateChanged = !string.Equals(previous.Date, driver.Date, StringComparison.OrdinalIgnoreCase);

                    if (versionChanged || dateChanged)
                    {
                        driver.Status = "Недавно обновлён";
                        continue;
                    }
                }

                driver.Status = _statusEvaluator.EvaluateStatus(driver.Date);
            }
        }

        private void SaveSnapshot(List<DriverItem> currentDrivers)
        {
            _previousSnapshot = currentDrivers.ToDictionary(
                BuildKey,
                d => new DriverSnapshot
                {
                    Version = d.Version,
                    Date = d.Date
                },
                StringComparer.OrdinalIgnoreCase);
        }

        private void UpdateSummary(List<DriverItem> drivers, bool isRescan)
        {
            var total = drivers.Count;
            var ok = drivers.Count(d => d.Status == "Актуален");
            var check = drivers.Count(d => d.Status == "Стоит проверить");
            var attention = drivers.Count(d => d.Status == "Требует внимания");
            var updated = drivers.Count(d => d.Status == "Недавно обновлён");

            SummaryText.Text =
                $"Найдено важных драйверов: {total} | Актуален: {ok} | Стоит проверить: {check} | Требует внимания: {attention} | Недавно обновлён: {updated}";

            LastScanText.Text =
                $"{(isRescan ? "Повторное сканирование" : "Сканирование")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private void OpenSource_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not DriverItem driver)
                return;

            try
            {
                switch (driver.OfficialAction.Kind)
                {
                    case OfficialActionKind.Url:
                        OpenUrl(driver.OfficialAction.Target);
                        break;
                    case OfficialActionKind.LocalApp:
                        OpenLocalApp(driver.OfficialAction.Target);
                        break;
                    case OfficialActionKind.WindowsUpdate:
                        OpenWindowsUpdate();
                        break;
                    case OfficialActionKind.Search:
                        OpenSearch(driver.OfficialAction.Target);
                        break;
                    default:
                        MessageBox.Show(
                            driver.OfficialAction.Message,
                            "Официальный источник",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    $"Не удалось открыть официальный источник для драйвера: {driver.Name}.",
                    ex);

                MessageBox.Show(
                    $"Не удалось открыть источник:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private static void OpenLocalApp(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private static void OpenWindowsUpdate()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:windowsupdate",
                UseShellExecute = true
            });
        }

        private static void OpenSearch(string query)
        {
            var url = DriverRules.GoogleSearchUrlPrefix + Uri.EscapeDataString(query);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
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

        private bool TryGetDriverCategory(string name, string? manufacturer, out string category)
        {
            category = string.Empty;

            var n = name.ToLowerInvariant();
            var m = (manufacturer ?? string.Empty).ToLowerInvariant();

            if (IsBlacklisted(n))
                return false;

            if (IsGpu(n))
            {
                category = "GPU";
                return true;
            }

            if (IsNetwork(n, m))
            {
                category = "Network";
                return true;
            }

            if (IsStorage(n))
            {
                category = "Storage";
                return true;
            }

            if (IsExternalAudio(n, m))
            {
                category = "AudioExternal";
                return true;
            }

            if (IsMainAudio(n, m))
            {
                category = "AudioMain";
                return true;
            }

            return false;
        }

        private static bool IsBlacklisted(string n)
        {
            if (DriverRules.BlacklistedTerms.Any(n.Contains))
                return true;

            if (n.Contains("nvidia") && n.Contains("audio"))
                return true;

            return false;
        }

        private static bool IsGpu(string n)
        {
            return (n.Contains("nvidia") && !n.Contains("audio")) ||
                   n.Contains("geforce") ||
                   n.Contains("radeon") ||
                   n.Contains("intel(r) uhd") ||
                   n.Contains("intel(r) iris") ||
                   n.Contains("intel arc");
        }

        private static bool IsNetwork(string n, string m)
        {
            return n.Contains("ethernet") ||
                   n.Contains("wi-fi") ||
                   n.Contains("wireless") ||
                   n.Contains("wlan") ||
                   n.Contains("bluetooth") ||
                   n.Contains("killer") ||
                   n.Contains("gigabit") ||
                   n.Contains("gbe family") ||
                   n.Contains("802.11") ||
                   n.Contains("mediatek") ||
                   n.Contains("qualcomm") ||
                   (m.Contains("intel") && (n.Contains("wireless") || n.Contains("bluetooth") || n.Contains("ax") || n.Contains("be200") || n.Contains("be202"))) ||
                   (m.Contains("realtek") && (n.Contains("rtl") || n.Contains("family controller") || n.Contains("gaming")));
        }

        private static bool IsStorage(string n)
        {
            return n.Contains("nvme") ||
                   n.Contains("sata ahci") ||
                   n.Contains("raid") ||
                   n.Contains("rst") ||
                   n.Contains("vmd") ||
                   n.Contains("storage controller");
        }

        private static bool IsMainAudio(string n, string m)
        {
            return (n.Contains("realtek") && n.Contains("audio")) || n.Contains("intel smart sound");
        }

        private static bool IsExternalAudio(string n, string m)
        {
            if (DriverRules.ExternalAudioBrands.Any(n.Contains))
                return true;

            if (n.Contains("usb audio device"))
                return false;

            return (n.Contains("audio") || m.Contains("audio")) &&
                   !n.Contains("realtek") &&
                   !n.Contains("nvidia") &&
                   !n.Contains("virtual") &&
                   !n.Contains("endpoint") &&
                   !n.Contains("controller");
        }

        private static List<DriverItem> SelectBestDrivers(List<DriverItem> drivers)
        {
            var result = new List<DriverItem>();

            result.AddRange(
                drivers.Where(d => d.Category == "GPU")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .Take(2));

            result.AddRange(
                drivers.Where(d => d.Category == "Network")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .OrderByDescending(ParseDateSafe)
                    .Take(3));

            result.AddRange(
                drivers.Where(d => d.Category == "Storage")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .OrderByDescending(ParseDateSafe)
                    .Take(2));

            var mainAudio = drivers.Where(d => d.Category == "AudioMain")
                .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .Select(SelectBestByDateThenVersion)
                .OrderByDescending(ParseDateSafe)
                .FirstOrDefault();

            if (mainAudio != null)
                result.Add(mainAudio);

            result.AddRange(
                drivers.Where(d => d.Category == "AudioExternal")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .OrderByDescending(ParseDateSafe));

            return result
                .GroupBy(BuildKey, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private static DriverItem SelectBestByDateThenVersion(IGrouping<string, DriverItem> group)
        {
            return group.OrderByDescending(ParseDateSafe)
                        .ThenByDescending(d => d.Version)
                        .First();
        }

        private static int GetCategoryOrder(DriverItem driver)
        {
            return driver.Category switch
            {
                "GPU" => 1,
                "Network" => 2,
                "Storage" => 3,
                "AudioMain" => 4,
                "AudioExternal" => 5,
                _ => 99
            };
        }

        private static string GetCategoryDisplay(string category)
        {
            return category switch
            {
                "GPU" => "GPU",
                "Network" => "Сеть",
                "Storage" => "Хранение",
                "AudioMain" => "Аудио",
                "AudioExternal" => "Аудиокарта",
                _ => category
            };
        }

        private static string BuildKey(DriverItem driver)
        {
            return $"{driver.Category}|{driver.Name}";
        }

        private static string CleanDeviceName(string name) => name.Trim();

        private static string CleanManufacturer(string? manufacturer)
        {
            if (string.IsNullOrWhiteSpace(manufacturer))
                return "-";

            return manufacturer.Replace("Corporation", string.Empty)
                               .Replace("(Standard system devices)", string.Empty)
                               .Trim();
        }

        private static string FormatDate(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
                return "-";

            try
            {
                var date = ManagementDateTimeConverter.ToDateTime(rawDate);
                return date.ToString("yyyy-MM-dd");
            }
            catch
            {
                return rawDate;
            }
        }

        private static DateTime ParseDateSafe(DriverItem driver)
        {
            if (DateTime.TryParse(driver.Date, out var parsed))
                return parsed;

            return DateTime.MinValue;
        }
    }

    public class DriverItem
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string CategoryDisplay { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Version { get; set; } = "";
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
        public string ButtonText { get; set; } = "Открыть";
        public string ButtonTooltip { get; set; } = "Открыть действие";
        public OfficialAction OfficialAction { get; set; } = OfficialAction.ForMessage("Открыть", "Источник не задан.", "Открыть действие");
    }

    public class DriverSnapshot
    {
        public string Version { get; set; } = "";
        public string Date { get; set; } = "";
    }

    public enum OfficialActionKind
    {
        None,
        Url,
        LocalApp,
        WindowsUpdate,
        Search
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

        public static OfficialAction ForSearch(string query, string buttonText, string tooltip)
        {
            return new OfficialAction
            {
                Kind = OfficialActionKind.Search,
                Target = query,
                Message = buttonText,
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
}
