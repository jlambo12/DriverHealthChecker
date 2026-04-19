using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverActionServiceTests
{
    [Fact]
    public void Execute_UrlWithoutNetwork_ReturnsWarning()
    {
        var service = new DriverActionService(
            new StubOnlineActionGuard(canOpen: false),
            new OnlineTargetValidator(),
            new LocalAppValidator());

        var feedback = service.Execute(new DriverItem
        {
            OfficialAction = OfficialAction.ForUrl("https://example.com", "Open", "tip")
        });

        Assert.Equal(DriverActionFeedbackKind.Warning, feedback.Kind);
        Assert.Equal("Нет сети", feedback.Title);
    }

    [Fact]
    public void Execute_InvalidUrl_ReturnsWarning()
    {
        var service = new DriverActionService(
            new StubOnlineActionGuard(canOpen: true),
            new OnlineTargetValidator(),
            new LocalAppValidator());

        var feedback = service.Execute(new DriverItem
        {
            OfficialAction = OfficialAction.ForUrl("ht!tp://bad", "Open", "tip")
        });

        Assert.Equal(DriverActionFeedbackKind.Warning, feedback.Kind);
        Assert.Equal("Некорректная ссылка", feedback.Title);
    }

    [Fact]
    public void Execute_MessageAction_ReturnsInfoFeedback()
    {
        var service = new DriverActionService(
            new StubOnlineActionGuard(canOpen: true),
            new OnlineTargetValidator(),
            new LocalAppValidator());

        var feedback = service.Execute(new DriverItem
        {
            OfficialAction = OfficialAction.ForMessage("Info", "safe fallback", "tip")
        });

        Assert.Equal(DriverActionFeedbackKind.Info, feedback.Kind);
        Assert.Equal("safe fallback", feedback.Message);
    }

    private sealed class StubOnlineActionGuard : IOnlineActionGuard
    {
        private readonly bool _canOpen;

        public StubOnlineActionGuard(bool canOpen)
        {
            _canOpen = canOpen;
        }

        public bool CanOpenOnlineAction(out string message)
        {
            message = _canOpen ? string.Empty : "Нет подключения к сети. Проверь интернет и попробуй снова.";
            return _canOpen;
        }
    }
}
