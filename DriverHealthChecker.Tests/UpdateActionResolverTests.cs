using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class UpdateActionResolverTests
{
    private readonly DeterministicUpdateActionResolver _resolver = new();

    [Fact]
    public void Resolve_InstalledOfficialApp_ReturnsOpenAppAction()
    {
        var action = _resolver.Resolve(new DriverUpdateContext
        {
            Channel = new OfficialSupportChannel
            {
                Type = OfficialSupportChannelType.InstalledOfficialApp,
                Target = @"C:\Apps\NVIDIA App.exe",
                DisplayName = "NVIDIA App",
                Description = "Официальный путь обновления для поддерживаемых NVIDIA-устройств."
            }
        });

        Assert.Equal(UpdateActionType.OpenApp, action.ActionType);
        Assert.Equal("Открыть NVIDIA App", action.DisplayText);
        Assert.Equal(@"C:\Apps\NVIDIA App.exe", action.Channel.Target);
    }

    [Fact]
    public void Resolve_OfficialAppInstall_ReturnsOpenUrlAction()
    {
        var action = _resolver.Resolve(new DriverUpdateContext
        {
            Channel = new OfficialSupportChannel
            {
                Type = OfficialSupportChannelType.OfficialAppInstall,
                Target = DriverRules.NvidiaAppUrl,
                DisplayName = "NVIDIA App",
                Description = "Официальная страница установки NVIDIA App."
            }
        });

        Assert.Equal(UpdateActionType.OpenUrl, action.ActionType);
        Assert.Equal("Установить NVIDIA App", action.DisplayText);
        Assert.Equal(DriverRules.NvidiaAppUrl, action.Channel.Target);
    }

    [Fact]
    public void Resolve_DirectDriverPageWithoutVerifiedExactTarget_ReturnsExplanation()
    {
        var action = _resolver.Resolve(new DriverUpdateContext
        {
            Channel = new OfficialSupportChannel
            {
                Type = OfficialSupportChannelType.DirectDriverPage,
                Target = "https://www.amd.com/en/support/download/drivers.html",
                DisplayName = "AMD Drivers and Support",
                Description = "Вендор определён, но точный package-specific route ещё не подтверждён."
            },
            HasVerifiedExactTarget = false
        });

        Assert.Equal(UpdateActionType.ShowExplanation, action.ActionType);
        Assert.Contains("verified exact route", action.Explanation);
    }

    [Fact]
    public void Resolve_DirectDriverPageWithVerifiedExactTarget_ReturnsOpenUrlAction()
    {
        var action = _resolver.Resolve(new DriverUpdateContext
        {
            Channel = new OfficialSupportChannel
            {
                Type = OfficialSupportChannelType.DirectDriverPage,
                Target = "https://vendor.example/drivers/device-123",
                DisplayName = "AMD Radeon RX 7600 Driver",
                Description = "Точная официальная страница драйвера."
            },
            HasVerifiedExactTarget = true
        });

        Assert.Equal(UpdateActionType.OpenUrl, action.ActionType);
        Assert.Equal("Открыть AMD Radeon RX 7600 Driver", action.DisplayText);
        Assert.Equal("https://vendor.example/drivers/device-123", action.Channel.Target);
    }

    [Fact]
    public void Resolve_ManualExplanation_RemainsSafeExplanation()
    {
        var action = _resolver.Resolve(new DriverUpdateContext
        {
            Channel = new OfficialSupportChannel
            {
                Type = OfficialSupportChannelType.ManualExplanation,
                DisplayName = "Contoso Device",
                Description = "Точный официальный канал обновления пока не определён. Нужна ручная проверка официального источника."
            }
        });

        Assert.Equal(UpdateActionType.ShowExplanation, action.ActionType);
        Assert.Equal("Показать пояснение", action.DisplayText);
        Assert.Contains("Точный официальный канал обновления пока не определён", action.Explanation);
    }

    [Fact]
    public void Resolve_InstalledOfficialAppWithoutTarget_ReturnsExplanation()
    {
        var action = _resolver.Resolve(new DriverUpdateContext
        {
            Channel = new OfficialSupportChannel
            {
                Type = OfficialSupportChannelType.InstalledOfficialApp,
                DisplayName = "NVIDIA App"
            }
        });

        Assert.Equal(UpdateActionType.ShowExplanation, action.ActionType);
        Assert.Contains("путь запуска не задан", action.Explanation);
    }
}
