using System;
using System.Collections.Generic;
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
        private readonly IDriverFilteringService _driverFilteringService = new DriverFilteringService();
        private readonly IDriverActionService _driverActionService;
        private readonly IDriverPresentationService _driverPresentationService;
        private bool _isScanning;
        private string _lastSummaryBaseText = "Нажми «Сканировать», чтобы получить список важных драйверов.";

        public MainWindow()
        {
            _driverScanMapper = new DriverScanMapper(_driverClassifier, _officialActionResolver, _driverSelectionService);
            _driverActionService = new DriverActionService(_onlineActionGuard, _onlineTargetValidator, _localAppValidator);
            _driverPresentationService = new DriverPresentationService(_deviceProfileDetector, _laptopOemRecommendationResolver);
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
                    .OrderBy(d => _driverPresentationService.GetCategoryOrder(d.Category))
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

                var deviceRecommendation = _driverPresentationService.BuildDeviceRecommendationItem();
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
            var categoryItems = _driverFilteringService.BuildCategoryItems(
                _currentDrivers,
                _hiddenDrivers,
                ShowHiddenCheckBox.IsChecked == true);

            var previousCategory = CategoryFilterCombo.SelectedItem?.ToString();
            CategoryFilterCombo.ItemsSource = categoryItems;
            CategoryFilterCombo.SelectedItem = previousCategory != null && categoryItems.Contains(previousCategory) ? previousCategory : "Все";
        }

        private void ApplyGridFilters()
        {
            var filterState = new DriverFilterState
            {
                SelectedCategory = CategoryFilterCombo.SelectedItem?.ToString() ?? "Все",
                SelectedStatus = StatusFilterCombo.SelectedItem?.ToString() ?? "Все",
                Search = SearchTextBox.Text?.Trim() ?? string.Empty,
                ShowHidden = ShowHiddenCheckBox.IsChecked == true
            };

            var visibleItems = _driverFilteringService.ApplyFilters(_currentDrivers, _hiddenDrivers, filterState);
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
                AppLogger.Info($"Action requested. device={driver.Name}, category={driver.Category}, kind={driver.OfficialAction.Kind}.");
                var feedback = _driverActionService.Execute(driver);

                if (feedback.Kind == DriverActionFeedbackKind.Warning)
                {
                    MessageBox.Show(
                        feedback.Message,
                        feedback.Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (feedback.Kind == DriverActionFeedbackKind.Info)
                {
                    AppLogger.Info($"Informational fallback shown. device={driver.Name}, message={driver.OfficialAction.Message}.");
                    MessageBox.Show(
                        feedback.Message,
                        feedback.Title,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                if (driver.OfficialAction.Kind == OfficialActionKind.Url)
                    AppLogger.Info($"Official URL opened. device={driver.Name}, target={driver.OfficialAction.Target}.");
                else if (driver.OfficialAction.Kind == OfficialActionKind.LocalApp)
                    AppLogger.Info($"Local app opened. device={driver.Name}, target={driver.OfficialAction.Target}.");
                else if (driver.OfficialAction.Kind == OfficialActionKind.WindowsUpdate)
                    AppLogger.Info($"Windows Update opened for device={driver.Name}.");
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

    }

}
