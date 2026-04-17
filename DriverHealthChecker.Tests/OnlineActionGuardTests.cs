using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class OnlineActionGuardTests
{
    [Fact]
    public void CanOpenOnlineAction_WhenNetworkAvailable_ReturnsTrue()
    {
        var guard = new OnlineActionGuard(new FakeNetworkStatusProvider(isAvailable: true));

        var result = guard.CanOpenOnlineAction(out var message);

        Assert.True(result);
        Assert.True(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void CanOpenOnlineAction_WhenNetworkUnavailable_ReturnsFalseWithMessage()
    {
        var guard = new OnlineActionGuard(new FakeNetworkStatusProvider(isAvailable: false));

        var result = guard.CanOpenOnlineAction(out var message);

        Assert.False(result);
        Assert.Contains("Нет доступа к сети", message);
    }

    private sealed class FakeNetworkStatusProvider : INetworkStatusProvider
    {
        private readonly bool _isAvailable;

        public FakeNetworkStatusProvider(bool isAvailable)
        {
            _isAvailable = isAvailable;
        }

        public bool IsNetworkAvailable() => _isAvailable;
    }
}
