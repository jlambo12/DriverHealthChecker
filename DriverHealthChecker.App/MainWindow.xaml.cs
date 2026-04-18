using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly IDriverClassifier _driverClassifier = new DriverClassifier();
        private readonly IOfficialActionResolver _officialActionResolver = new OfficialActionResolver();
        private readonly IDeviceProfileDetector _deviceProfileDetector = new DeviceProfileDetector();
        private readonly ILaptopOemRecommendationResolver _laptopOemRecommendationResolver = new LaptopOemRecommendationResolver();
        private readonly IScanReportWriter _scanReportWriter = new ScanReportWriter();
        private readonly IOnlineActionGuard _onlineActionGuard = new OnlineActionGuard(new NetworkStatusProvider());
        private readonly IOnlineTargetValidator _onlineTargetValidator = new OnlineTargetValidator();
        private readonly ILocalAppValidator _localAppValidator = new LocalAppValidator();
        private readonly IDriverSelectionService _driverSelectionService = new DriverSelectionService(new DriverVersionComparer());
        private readonly IDriverComparisonService _driverComparisonService = new DriverComparisonService(new DriverStatusEvaluator());
        private readonly IWmiDriverScanner _wmiDriverScanner = new WmiDriverScanner();
        private readonly IDriverScanMapper _driverScanMapper;
        private bool _isScanning;
        private string _lastSummaryBaseText = "Нажми «Сканировать», чтобы получить список важных драйверов.";

        public MainWindow()
        {
            _driverScanMapper = new DriverScanMapper(_driverClassifier, _officialActionResolver, _driverSelectionService);
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
            AppLogger.Info($"Scan started. isRescan={isRescan}.");

            try
            {
                var profile = _deviceProfileDetector.TryGetDeviceProfile();
                var currentDrivers = await Task.Run(() => ScanImportantDrivers(profile));

                _driverComparisonService.ApplyComparison(currentDrivers, isRescan, _previousSnapshot);

                _currentDrivers = currentDrivers
                    .OrderBy(GetCategoryOrder)
                    .ThenBy(d => d.Name)
                    .ToList();

                UpdateFilterItems();
                ApplyGridFilters();

                var deviceKind = profile == null ? "Unknown" : (profile.IsLaptop ? "Laptop" : "Desktop");
                var reportPath = _scanReportWriter.TryWrite(_currentDrivers, isRescan, deviceKind);
                AppLogger.Info($"Scan completed. drivers={_currentDrivers.Count}, deviceKind={deviceKind}, reportCreated={!string.IsNullOrWhiteSpace(reportPath)}.");

                UpdateSummary(currentDrivers, isRescan, reportPath);
                _previousSnapshot = _driverComparisonService.BuildSnapshot(currentDrivers);
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

        private List<DriverItem> ScanImportantDrivers(DeviceProfile? profile)
        {
            try
            {
                var records = _wmiDriverScanner.ScanSignedDrivers();
                AppLogger.Info($"WMI records collected: {records.Count}.");

                var buildResult = _driverScanMapper.Build(records, profile);
                var selected = buildResult.SelectedDrivers;

                var deviceRecommendation = BuildDeviceRecommendationItem();
                if (deviceRecommendation != null)
                    selected.Add(deviceRecommendation);

                _hiddenDrivers = buildResult.HiddenDrivers;
                return selected;
            }
            catch (Exception ex)
            {
                AppLogger.Error("Ошибка во время сканирования драйверов.", ex);

                MessageBox.Show(
                    $"Ошибка при сканировании драйверов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return new List<DriverItem>();
            }
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
            CategoryFilterCombo.SelectedItem = previousCategory != null && categoryItems.Contains(previousCategory) ? previousCategory : "Все";
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
            AppLogger.Info($"Filters applied. category={selectedCategory}, status={selectedStatus}, searchLength={search.Length}, showHidden={ShowHiddenCheckBox.IsChecked == true}, visible={visibleItems.Count}.");
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
                AppLogger.Info($"Action requested. device={driver.Name}, category={driver.Category}, kind={driver.OfficialAction.Kind}.");
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
                        AppLogger.Info($"Official URL opened. device={driver.Name}, target={driver.OfficialAction.Target}.");
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
                        AppLogger.Info($"Local app opened. device={driver.Name}, target={driver.OfficialAction.Target}.");
                        break;
                    case OfficialActionKind.WindowsUpdate:
                        OpenWindowsUpdate();
                        AppLogger.Info($"Windows Update opened for device={driver.Name}.");
                        break;
                    default:
                        AppLogger.Info($"Informational fallback shown. device={driver.Name}, message={driver.OfficialAction.Message}.");
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

    }

}
