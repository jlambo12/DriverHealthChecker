using System;
using System.Collections.Generic;
using System.IO;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class InstalledOfficialAppCatalogTests
{
    [Fact]
    public void TryResolveInstalledApp_NvidiaInstalled_ReturnsLaunchChannel()
    {
        var executablePath = CreateTemporaryExecutable();
        try
        {
            var catalog = new InstalledOfficialAppCatalog(
                new LocalAppValidator(),
                new List<string> { executablePath },
                Array.Empty<string>());

            var resolved = catalog.TryResolveInstalledApp(
                new DriverIdentity
                {
                    Manufacturer = "NVIDIA",
                    NormalizedManufacturer = "NVIDIA"
                },
                out var channel);

            Assert.True(resolved);
            Assert.Equal(OfficialSupportChannelType.InstalledOfficialApp, channel.Type);
            Assert.True(channel.IsInstalled);
            Assert.Equal(executablePath, channel.Target);
        }
        finally
        {
            DeleteTemporaryExecutable(executablePath);
        }
    }

    [Fact]
    public void TryResolveInstalledApp_IntelInstalled_ReturnsLaunchChannel()
    {
        var executablePath = CreateTemporaryExecutable();
        try
        {
            var catalog = new InstalledOfficialAppCatalog(
                new LocalAppValidator(),
                Array.Empty<string>(),
                new List<string> { executablePath });

            var resolved = catalog.TryResolveInstalledApp(
                new DriverIdentity
                {
                    Manufacturer = "Intel",
                    NormalizedManufacturer = "INTEL"
                },
                out var channel);

            Assert.True(resolved);
            Assert.Equal(OfficialSupportChannelType.InstalledOfficialApp, channel.Type);
            Assert.True(channel.IsInstalled);
            Assert.Equal(executablePath, channel.Target);
            Assert.Equal("Intel Driver & Support Assistant", channel.DisplayName);
        }
        finally
        {
            DeleteTemporaryExecutable(executablePath);
        }
    }

    [Fact]
    public void TryResolveInstalledApp_UnsupportedVendor_ReturnsFalse()
    {
        var catalog = new InstalledOfficialAppCatalog(
            new LocalAppValidator(),
            Array.Empty<string>(),
            Array.Empty<string>());

        var resolved = catalog.TryResolveInstalledApp(
            new DriverIdentity
            {
                Manufacturer = "Contoso",
                NormalizedManufacturer = "CONTOSO"
            },
            out var channel);

        Assert.False(resolved);
        Assert.Equal(OfficialSupportChannelType.ManualExplanation, channel.Type);
        Assert.False(channel.IsInstalled);
    }

    private static string CreateTemporaryExecutable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.exe");
        File.WriteAllText(path, "stub");
        return path;
    }

    private static void DeleteTemporaryExecutable(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
