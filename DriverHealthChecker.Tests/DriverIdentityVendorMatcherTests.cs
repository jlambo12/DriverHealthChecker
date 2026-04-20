using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class DriverIdentityVendorMatcherTests
{
    [Fact]
    public void TryResolveVendor_PnpDeviceIdHasHighestPriority()
    {
        var resolved = DriverIdentityVendorMatcher.TryResolveVendor(
            new DriverIdentity
            {
                PnpDeviceId = @"PCI\VEN_10DE&DEV_2704",
                HardwareIds = { @"PCI\VEN_1002&DEV_164E" },
                CompatibleIds = { @"PCI\VEN_8086&DEV_24FD" },
                NormalizedManufacturer = "INTEL CORPORATION"
            },
            out var vendor);

        Assert.True(resolved);
        Assert.Equal("NVIDIA", vendor);
    }

    [Fact]
    public void TryResolveVendor_HardwareIdsOverrideManufacturerFallback()
    {
        var resolved = DriverIdentityVendorMatcher.TryResolveVendor(
            new DriverIdentity
            {
                HardwareIds = { @"PCI\VEN_1002&DEV_164E" },
                NormalizedManufacturer = "INTEL CORPORATION"
            },
            out var vendor);

        Assert.True(resolved);
        Assert.Equal("AMD", vendor);
    }

    [Fact]
    public void TryResolveVendor_CompatibleIdsUsedBeforeManufacturerFallback()
    {
        var resolved = DriverIdentityVendorMatcher.TryResolveVendor(
            new DriverIdentity
            {
                CompatibleIds = { @"PCI\VEN_8086&DEV_24FD" },
                NormalizedManufacturer = "NVIDIA CORPORATION"
            },
            out var vendor);

        Assert.True(resolved);
        Assert.Equal("INTEL", vendor);
    }

    [Fact]
    public void TryResolveVendor_UsesNormalizedManufacturerOnlyAsFallback()
    {
        var resolved = DriverIdentityVendorMatcher.TryResolveVendor(
            new DriverIdentity
            {
                NormalizedManufacturer = "NVIDIA CORPORATION"
            },
            out var vendor);

        Assert.True(resolved);
        Assert.Equal("NVIDIA", vendor);
    }

    [Fact]
    public void TryResolveVendor_DoesNotGuessByContainsLikeManufacturer()
    {
        var resolved = DriverIdentityVendorMatcher.TryResolveVendor(
            new DriverIdentity
            {
                NormalizedManufacturer = "SUPER NVIDIA COMPATIBLE DEVICES"
            },
            out _);

        Assert.False(resolved);
    }

    [Fact]
    public void TryResolveVendor_DoesNotUseLoosePrefixGuessing()
    {
        var resolved = DriverIdentityVendorMatcher.TryResolveVendor(
            new DriverIdentity
            {
                PnpDeviceId = @"PCI\VEN_10DE1&DEV_2704"
            },
            out _);

        Assert.False(resolved);
    }
}
