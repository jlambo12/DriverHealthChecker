using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DriverHealthChecker.App;

internal sealed class MainWindowViewModel
{
    private readonly IDeviceProfileDetector _deviceProfileDetector;
    private readonly IScanReportWriter _scanReportWriter;
    private readonly IDriverComparisonService _driverComparisonService;
    private readonly IWmiDriverScanner _wmiDriverScanner;
    private readonly IDriverScanMapper _driverScanMapper;
    private readonly IDriverFilteringService _driverFilteringService;
    private readonly IDriverPresentationService _driverPresentationService;

    private Dictionary<string, DriverSnapshot> _previousSnapshot = new();
    private List<DriverItem> _currentDrivers = new();
    private List<DriverItem> _hiddenDrivers = new();
    private string _lastSummaryBaseText = "Нажми «Сканировать», чтобы получить список важных драйверов.";

    public bool IsScanning { get; private set; }

    public MainWindowViewModel(
        IDeviceProfileDetector deviceProfileDetector,
        IScanReportWriter scanReportWriter,
        IDriverComparisonService driverComparisonService,
        IWmiDriverScanner wmiDriverScanner,
        IDriverScanMapper driverScanMapper,
        IDriverFilteringService driverFilteringService,
        IDriverPresentationService driverPresentationService)
    {
        _deviceProfileDetector = deviceProfileDetector;
        _scanReportWriter = scanReportWriter;
        _driverComparisonService = driverComparisonService;
        _wmiDriverScanner = wmiDriverScanner;
        _driverScanMapper = driverScanMapper;
        _driverFilteringService = driverFilteringService;
        _driverPresentationService = driverPresentationService;
    }

    public async Task<OperationResult<ScanUiState>> RunScanAsync(bool isRescan)
    {
        if (IsScanning)
            return OperationResult<ScanUiState>.Failure("Сканирование уже выполняется.");

        IsScanning = true;

        try
        {
            var profile = _deviceProfileDetector.TryGetDeviceProfile();
            var scanResult = await Task.Run(() => ScanImportantDrivers(profile));
            if (!scanResult.IsSuccess)
                return OperationResult<ScanUiState>.Failure(scanResult.ErrorMessage ?? "Не удалось выполнить сканирование драйверов.");

            var currentDrivers = scanResult.Value ?? new List<DriverItem>();
            _driverComparisonService.ApplyComparison(currentDrivers, isRescan, _previousSnapshot);

            _currentDrivers = currentDrivers
                .OrderBy(d => _driverPresentationService.GetCategoryOrder(d.CategoryKind))
                .ThenBy(d => d.Name)
                .ToList();

            var deviceKind = profile == null ? "Unknown" : (profile.IsLaptop ? "Laptop" : "Desktop");
            var reportPath = _scanReportWriter.TryWrite(_currentDrivers, isRescan, deviceKind);
            AppLogger.Info($"Scan completed. drivers={_currentDrivers.Count}, deviceKind={deviceKind}, reportCreated={!string.IsNullOrWhiteSpace(reportPath)}.");

            _lastSummaryBaseText = BuildSummaryBaseText(_currentDrivers);
            _previousSnapshot = _driverComparisonService.BuildSnapshot(_currentDrivers);

            var reportSuffix = string.IsNullOrWhiteSpace(reportPath)
                ? string.Empty
                : $" | Отчёт: {System.IO.Path.GetFileName(reportPath)}";

            return OperationResult<ScanUiState>.Success(new ScanUiState
            {
                SummaryBaseText = _lastSummaryBaseText,
                LastScanText = $"{(isRescan ? "Повторное сканирование" : "Сканирование")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{reportSuffix}"
            });
        }
        catch (Exception ex)
        {
            AppLogger.Error("Ошибка во время выполнения сканирования во ViewModel.", ex);
            return OperationResult<ScanUiState>.Failure(ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    public List<string> BuildCategoryItems(bool showHidden)
    {
        return _driverFilteringService.BuildCategoryItems(_currentDrivers, _hiddenDrivers, showHidden);
    }

    public List<DriverItem> ApplyFilters(DriverFilterState filterState)
    {
        return _driverFilteringService.ApplyFilters(_currentDrivers, _hiddenDrivers, filterState);
    }

    public string GetSummaryTextWithVisibleCount(int visibleCount)
    {
        return $"{_lastSummaryBaseText} | Показано после фильтров: {visibleCount}";
    }

    private OperationResult<List<DriverItem>> ScanImportantDrivers(DeviceProfile? profile)
    {
        var recordsResult = _wmiDriverScanner.ScanSignedDrivers();
        if (!recordsResult.IsSuccess)
            return OperationResult<List<DriverItem>>.Failure(recordsResult.ErrorMessage ?? "Не удалось получить список драйверов.");

        var records = recordsResult.Value ?? new List<ScannedDriverRecord>();
        AppLogger.Info($"WMI records collected: {records.Count}.");

        var buildResult = _driverScanMapper.Build(records, profile);
        var selected = buildResult.SelectedDrivers;

        var deviceRecommendation = _driverPresentationService.BuildDeviceRecommendationItem();
        if (deviceRecommendation != null)
            selected.Add(deviceRecommendation);

        _hiddenDrivers = buildResult.HiddenDrivers;

        return OperationResult<List<DriverItem>>.Success(selected);
    }

    private static string BuildSummaryBaseText(IReadOnlyCollection<DriverItem> drivers)
    {
        var total = drivers.Count;
        var ok = drivers.Count(d => d.StatusKind == DriverHealthStatus.UpToDate);
        var check = drivers.Count(d => d.StatusKind == DriverHealthStatus.NeedsReview);
        var attention = drivers.Count(d => d.StatusKind == DriverHealthStatus.NeedsAttention);
        var updated = drivers.Count(d => d.StatusKind == DriverHealthStatus.RecentlyUpdated);

        return $"Найдено важных драйверов: {total} | Актуален: {ok} | Стоит проверить: {check} | Требует внимания: {attention} | Недавно обновлён: {updated}";
    }
}

internal sealed class ScanUiState
{
    public string SummaryBaseText { get; init; } = string.Empty;
    public string LastScanText { get; init; } = string.Empty;
}
