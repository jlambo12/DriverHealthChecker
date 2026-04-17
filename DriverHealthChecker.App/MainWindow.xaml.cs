using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using Button = System.Windows.Controls.Button;

namespace DriverHealthChecker.App
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, DriverSnapshot> _previousSnapshot = new();
        private List<DriverItem> _currentDrivers = new();
        private List<DriverItem> _hiddenDrivers = new();
        private readonly IDriverStatusEvaluator _statusEvaluator = new DriverStatusEvaluator();
        private readonly IDriverClassifier _driverClassifier = new DriverClassifier();
        private readonly IOfficialActionResolver _officialActionResolver = new OfficialActionResolver();
        private readonly IDeviceProfileDetector _deviceProfileDetector = new DeviceProfileDetector();
        private readonly ILaptopOemRecommendationResolver _laptopOemRecommendationResolver = new LaptopOemRecommendationResolver();
        private readonly IScanReportWriter _scanReportWriter = new ScanReportWriter();
        private readonly IOnlineActionGuard _onlineActionGuard = new OnlineActionGuard(new NetworkStatusProvider());
        private readonly IOnlineTargetValidator _onlineTargetValidator = new OnlineTargetValidator();
        private readonly ILocalAppValidator _localAppValidator = new LocalAppValidator();
        private bool _isScanning;
        private string _lastSummaryBaseText = "Нажми «Сканировать», чтобы получить список важных драйверов.";

        public MainWindow()
        {
            InitializeComponent();
            InitializeFilters();
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            await RunScanAsync(false);
        }

        private async void RescanButton_Click(object sender, RoutedEventArgs e)
        {
            await RunScanAsync(true);
        }

        private async Task RunScanAsync(bool isRescan)
        {
            if (_isScanning)
                return;

            _isScanning = true;
            SetUiBusy(true);

            try
            {
                var currentDrivers = await Task.Run(ScanImportantDrivers);

                ApplyComparison(currentDrivers, isRescan);

                _currentDrivers = currentDrivers
                    .OrderBy(GetCategoryOrder)
                    .ThenBy(d => d.Name)
                    .ToList();

                UpdateFilterItems();
                ApplyGridFilters();

                var profile = _deviceProfileDetector.TryGetDeviceProfile();
                var deviceKind = profile == null ? "Unknown" : (profile.IsLaptop ? "Laptop" : "Desktop");
                var reportPath = _scanReportWriter.TryWrite(_currentDrivers, isRescan, deviceKind);

                UpdateSummary(currentDrivers, isRescan, reportPath);
                SaveSnapshot(currentDrivers);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Ошибка во время выполнения сканирования.", ex);

                MessageBox.Show(
                    $"Ошибка при выполнении сканирования:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetUiBusy(false);
                _isScanning = false;
            }
        }

        private List<DriverItem> ScanImportantDrivers()
        {
            var allDrivers = new List<DriverItem>();
            var hiddenDrivers = new List<DriverItem>();

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

                        if (!_driverClassifier.TryClassify(name, manufacturer, out var category, out var reason))
                        {
                            if (!string.IsNullOrWhiteSpace(reason))
                            {
                                hiddenDrivers.Add(new DriverItem
                                {
                                    Name = CleanDeviceName(name),
                                    Manufacturer = CleanManufacturer(manufacturer),
                                    Version = string.IsNullOrWhiteSpace(version) ? "-" : version,
                                    Date = FormatDate(rawDate),
                                    Category = "HiddenSystem",
                                    CategoryDisplay = GetCategoryDisplay("HiddenSystem"),
                                    Status = "Скрыт",
                                    DetectionReason = reason,
                                    OfficialAction = OfficialAction.ForMessage(
                                        "Почему скрыто",
                                        "Это устройство скрыто из основного списка, чтобы уменьшить шум.",
                                        reason),
                                    ButtonText = "Почему скрыто",
                                    ButtonTooltip = reason
                                });
                            }

                            continue;
                        }

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
                            DetectionReason = reason,
                            ButtonTooltip = $"{action.Tooltip} · Причина: {reason}"
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

            var selected = SelectBestDrivers(allDrivers);
            var deviceRecommendation = BuildDeviceRecommendationItem();
            if (deviceRecommendation != null)
                selected.Add(deviceRecommendation);

            _hiddenDrivers = hiddenDrivers
                .OrderBy(d => d.Name)
                .ToList();

            return selected;
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

                if (driver.Category == "DeviceRecommendation")
                {
                    driver.Status = "Рекомендация";
                    continue;
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

        private void UpdateSummary(List<DriverItem> drivers, bool isRescan, string? reportPath)
        {
            var total = drivers.Count;
            var ok = drivers.Count(d => d.Status == "Актуален");
            var check = drivers.Count(d => d.Status == "Стоит проверить");
            var attention = drivers.Count(d => d.Status == "Требует внимания");
            var updated = drivers.Count(d => d.Status == "Недавно обновлён");

            _lastSummaryBaseText =
                $"Найдено важных драйверов: {total} | Актуален: {ok} | Стоит проверить: {check} | Требует внимания: {attention} | Недавно обновлён: {updated}";

            var reportSuffix = string.IsNullOrWhiteSpace(reportPath)
                ? string.Empty
                : $" | Отчёт: {System.IO.Path.GetFileName(reportPath)}";

            SummaryText.Text = _lastSummaryBaseText;
            LastScanText.Text =
                $"{(isRescan ? "Повторное сканирование" : "Сканирование")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{reportSuffix}";

            UpdateSummaryVisibleHint(GetVisibleItemCount());
        }

        private void InitializeFilters()
        {
            CategoryFilterCombo.ItemsSource = new[] { "Все" };
            CategoryFilterCombo.SelectedIndex = 0;

            StatusFilterCombo.ItemsSource = new[] { "Все", "Актуален", "Стоит проверить", "Требует внимания", "Недавно обновлён", "Скрыт", "Рекомендация" };
            StatusFilterCombo.SelectedIndex = 0;

            SearchTextBox.Text = string.Empty;
        }

        private void UpdateFilterItems()
        {
            var sourceForCategories = _currentDrivers.AsEnumerable();
            if (ShowHiddenCheckBox.IsChecked == true)
                sourceForCategories = sourceForCategories.Concat(_hiddenDrivers);

            var categoryItems = new[] { "Все" }
                .Concat(sourceForCategories.Select(d => d.CategoryDisplay).Distinct().OrderBy(c => c))
                .ToList();

            var previousCategory = CategoryFilterCombo.SelectedItem?.ToString();
            CategoryFilterCombo.ItemsSource = categoryItems;
            CategoryFilterCombo.SelectedItem = categoryItems.Contains(previousCategory) ? previousCategory : "Все";
        }

        private void ApplyGridFilters()
        {
            var selectedCategory = CategoryFilterCombo.SelectedItem?.ToString() ?? "Все";
            var selectedStatus = StatusFilterCombo.SelectedItem?.ToString() ?? "Все";
            var search = SearchTextBox.Text?.Trim() ?? string.Empty;

            var source = _currentDrivers.AsEnumerable();
            if (ShowHiddenCheckBox.IsChecked == true)
                source = source.Concat(_hiddenDrivers);

            var query = source;

            if (!string.Equals(selectedCategory, "Все", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => string.Equals(d.CategoryDisplay, selectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.Equals(selectedStatus, "Все", StringComparison.OrdinalIgnoreCase))
                query = query.Where(d => string.Equals(d.Status, selectedStatus, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d =>
                    d.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    d.Manufacturer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    d.DetectionReason.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var visibleItems = query.ToList();
            DriversGrid.ItemsSource = visibleItems;
            UpdateSummaryVisibleHint(visibleItems.Count);
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyGridFilters();
        }

        private void StatusFilterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyGridFilters();
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyGridFilters();
        }

        private void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            CategoryFilterCombo.SelectedItem = "Все";
            StatusFilterCombo.SelectedItem = "Все";
            SearchTextBox.Text = string.Empty;
            ShowHiddenCheckBox.IsChecked = false;
            UpdateFilterItems();
            ApplyGridFilters();
        }

        private void ShowHiddenCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (ShowHiddenCheckBox.IsChecked == true &&
                !string.Equals(StatusFilterCombo.SelectedItem?.ToString(), "Все", StringComparison.OrdinalIgnoreCase))
            {
                StatusFilterCombo.SelectedItem = "Все";
            }

            UpdateFilterItems();
            ApplyGridFilters();
        }

        private void OpenSource_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not DriverItem driver)
                return;
            if (driver.OfficialAction == null)
                return;

            try
            {
                switch (driver.OfficialAction.Kind)
                {
                    case OfficialActionKind.Url:
                        if (!TryPassOnlineActionGuard())
                            return;
                        if (!_onlineTargetValidator.IsValidUrl(driver.OfficialAction.Target))
                        {
                            MessageBox.Show(
                                "Ссылка на официальный источник некорректна.",
                                "Некорректная ссылка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                        OpenUrl(driver.OfficialAction.Target);
                        break;
                    case OfficialActionKind.LocalApp:
                        if (!_localAppValidator.Exists(driver.OfficialAction.Target))
                        {
                            MessageBox.Show(
                                "Локальное приложение не найдено. Используй официальный сайт производителя.",
                                "Приложение не найдено",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                        OpenLocalApp(driver.OfficialAction.Target);
                        break;
                    case OfficialActionKind.WindowsUpdate:
                        OpenWindowsUpdate();
                        break;
                    case OfficialActionKind.Search:
                        if (!TryPassOnlineActionGuard())
                            return;
                        if (string.IsNullOrWhiteSpace(driver.OfficialAction.Target))
                        {
                            MessageBox.Show(
                                "Строка поиска для этого действия не задана.",
                                "Некорректное действие",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
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

        private bool TryPassOnlineActionGuard()
        {
            if (_onlineActionGuard.CanOpenOnlineAction(out var message))
                return true;

            MessageBox.Show(
                message,
                "Нет сети",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
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

        private static List<DriverItem> SelectBestDrivers(List<DriverItem> drivers)
        {
            var result = new List<DriverItem>();

            result.AddRange(
                drivers.Where(d => d.Category == "GPU")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .Take(3));

            result.AddRange(
                drivers.Where(d => d.Category == "Network")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .OrderByDescending(ParseDateSafe)
                    .Take(5));

            result.AddRange(
                drivers.Where(d => d.Category == "Storage")
                    .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(SelectBestByDateThenVersion)
                    .OrderByDescending(ParseDateSafe)
                    .Take(3));

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
                "DeviceRecommendation" => 6,
                "HiddenSystem" => 98,
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
                "DeviceRecommendation" => "Рекомендация",
                "HiddenSystem" => "Скрытые",
                _ => category
            };
        }

        private DriverItem? BuildDeviceRecommendationItem()
        {
            var profile = _deviceProfileDetector.TryGetDeviceProfile();
            if (profile == null || !profile.IsLaptop)
                return null;

            var action = _laptopOemRecommendationResolver.Resolve(profile.Manufacturer, profile.Model);

            return new DriverItem
            {
                Name = "Оптимизация ноутбука (рекомендация)",
                Manufacturer = string.IsNullOrWhiteSpace(profile.Manufacturer) ? "OEM" : profile.Manufacturer,
                Version = "-",
                Date = "-",
                Category = "DeviceRecommendation",
                CategoryDisplay = GetCategoryDisplay("DeviceRecommendation"),
                Status = "Рекомендация",
                OfficialAction = action,
                DetectionReason = $"Ноутбук: {profile.Manufacturer} {profile.Model}".Trim(),
                ButtonText = action.ButtonText,
                ButtonTooltip = $"{action.Tooltip} · Причина: ноутбук {profile.Manufacturer} {profile.Model}".Trim()
            };
        }

        private void SetUiBusy(bool isBusy)
        {
            ScanButton.IsEnabled = !isBusy;
            RescanButton.IsEnabled = !isBusy;
            CategoryFilterCombo.IsEnabled = !isBusy;
            StatusFilterCombo.IsEnabled = !isBusy;
            SearchTextBox.IsEnabled = !isBusy;
            ShowHiddenCheckBox.IsEnabled = !isBusy;
            ResetFiltersButton.IsEnabled = !isBusy;
            DriversGrid.IsEnabled = !isBusy;
        }

        private void UpdateSummaryVisibleHint(int visibleCount)
        {
            SummaryText.Text = $"{_lastSummaryBaseText} | Показано после фильтров: {visibleCount}";
        }

        private int GetVisibleItemCount()
        {
            return DriversGrid.ItemsSource is IEnumerable<DriverItem> items
                ? items.Count()
                : 0;
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
        public string DetectionReason { get; set; } = "";
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
