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
        private readonly IOnlineActionGuard _onlineActionGuard = new OnlineActionGuard(new NetworkStatusProvider());
        private readonly IOnlineTargetValidator _onlineTargetValidator = new OnlineTargetValidator();
        private readonly ILocalAppValidator _localAppValidator = new LocalAppValidator();
        private readonly IDriverActionService _driverActionService;
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            _driverActionService = new DriverActionService(_onlineActionGuard, _onlineTargetValidator, _localAppValidator);

            var deviceProfileDetector = new DeviceProfileDetector();
            var scanReportWriter = new ScanReportWriter();
            var driverComparisonService = new DriverComparisonService(new DriverStatusEvaluator());
            var wmiDriverScanner = new WmiDriverScanner();
            var driverSelectionService = new DriverSelectionService(new DriverVersionComparer());
            var driverVerifierRegistry = new DriverVerifierRegistry();
            var driverScanMapper = new DriverScanMapper(new DriverClassifier(), new OfficialActionResolver(), driverSelectionService, driverVerifierRegistry);
            var driverFilteringService = new DriverFilteringService();
            var driverPresentationService = new DriverPresentationService(deviceProfileDetector, new LaptopOemRecommendationResolver());

            _viewModel = new MainWindowViewModel(
                deviceProfileDetector,
                scanReportWriter,
                driverComparisonService,
                wmiDriverScanner,
                driverScanMapper,
                driverFilteringService,
                driverPresentationService);
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
            if (_viewModel.IsScanning)
                return;

            SetUiBusy(true);
            AppLogger.Info($"Scan started. isRescan={isRescan}.");

            try
            {
                var result = await _viewModel.RunScanAsync(isRescan);
                if (!result.IsSuccess)
                    throw new InvalidOperationException(result.ErrorMessage ?? "Не удалось выполнить сканирование драйверов.");

                var scanData = result.Value;
                if (scanData == null)
                    throw new InvalidOperationException("Сканирование завершилось без данных.");

                UpdateFilterItems();
                ApplyGridFilters();

                SummaryText.Text = scanData.SummaryBaseText;
                LastScanText.Text = scanData.LastScanText;
                UpdateSummaryVisibleHint(GetVisibleItemCount());
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
            }
        }

        private void InitializeFilters()
        {
            CategoryFilterCombo.ItemsSource = new[] { "Все" };
            CategoryFilterCombo.SelectedIndex = 0;

            StatusFilterCombo.ItemsSource = new[]
            {
                "Все",
                DriverTextMapper.ToStatusDisplay(DriverHealthStatus.UpToDate),
                DriverTextMapper.ToStatusDisplay(DriverHealthStatus.NeedsReview),
                DriverTextMapper.ToStatusDisplay(DriverHealthStatus.NeedsAttention),
                DriverTextMapper.ToStatusDisplay(DriverHealthStatus.RecentlyUpdated),
                DriverTextMapper.ToStatusDisplay(DriverHealthStatus.Hidden),
                DriverTextMapper.ToStatusDisplay(DriverHealthStatus.Recommendation)
            };
            StatusFilterCombo.SelectedIndex = 0;

            SearchTextBox.Text = string.Empty;
        }

        private void UpdateFilterItems()
        {
            var categoryItems = _viewModel.BuildCategoryItems(ShowHiddenCheckBox.IsChecked == true);

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

            var visibleItems = _viewModel.ApplyFilters(filterState);
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
            SummaryText.Text = _viewModel.GetSummaryTextWithVisibleCount(visibleCount);
        }

        private int GetVisibleItemCount()
        {
            return DriversGrid.ItemsSource is IEnumerable<DriverItem> items
                ? items.Count()
                : 0;
        }
    }
}
