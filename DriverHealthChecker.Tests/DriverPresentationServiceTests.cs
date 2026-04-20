using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public class DriverPresentationServiceTests
{
    [Fact]
    public void GetCategoryOrder_ReturnsExpectedOrderForNetwork()
    {
        var service = new DriverPresentationService(new StubProfileDetector(null), new LaptopOemRecommendationResolver());

        Assert.Equal(2, service.GetCategoryOrder(DriverCategory.Network));
    }

    [Fact]
    public void BuildDeviceRecommendationItem_ForDesktop_ReturnsNull()
    {
        var service = new DriverPresentationService(
            new StubProfileDetector(new DeviceProfile("Custom", "Desktop", false)),
            new LaptopOemRecommendationResolver());

        var result = service.BuildDeviceRecommendationItem();

        Assert.Null(result);
    }

    private sealed class StubProfileDetector : IDeviceProfileDetector
    {
        private readonly DeviceProfile? _profile;

        public StubProfileDetector(DeviceProfile? profile)
        {
            _profile = profile;
        }

        public DeviceProfile? TryGetDeviceProfile() => _profile;
    }
}
