using System;
using System.Collections.Generic;
using System.IO;

namespace DriverHealthChecker.App;

internal interface IInstalledOfficialAppCatalog
{
    bool TryResolveInstalledApp(DriverIdentity identity, out OfficialSupportChannel channel);
}

internal sealed class InstalledOfficialAppCatalog : IInstalledOfficialAppCatalog
{
    private static readonly string[] DefaultNvidiaAppCandidates =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA app", "CEF", "NVIDIA App.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NVIDIA Corporation", "NVIDIA app", "CEF", "NVIDIA App.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA app", "NVIDIA App.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NVIDIA Corporation", "NVIDIA app", "NVIDIA App.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVIDIA GeForce Experience", "NVIDIA GeForce Experience.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NVIDIA Corporation", "NVIDIA GeForce Experience", "NVIDIA GeForce Experience.exe")
    };

    private static readonly string[] DefaultIntelAppCandidates =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Intel", "Driver and Support Assistant", "DSAService.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Intel", "Driver and Support Assistant", "DSATray.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Intel", "Driver and Support Assistant", "DSAService.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Intel", "Driver and Support Assistant", "DSATray.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Intel", "Driver and Support Assistant", "IntelDriverSupportAssistant.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Intel", "Driver and Support Assistant", "IntelDriverSupportAssistant.exe")
    };

    private readonly ILocalAppValidator _localAppValidator;
    private readonly IReadOnlyList<string> _nvidiaAppCandidates;
    private readonly IReadOnlyList<string> _intelAppCandidates;

    public InstalledOfficialAppCatalog()
        : this(new LocalAppValidator(), DefaultNvidiaAppCandidates, DefaultIntelAppCandidates)
    {
    }

    internal InstalledOfficialAppCatalog(
        ILocalAppValidator localAppValidator,
        IReadOnlyList<string>? nvidiaAppCandidates = null,
        IReadOnlyList<string>? intelAppCandidates = null)
    {
        _localAppValidator = localAppValidator;
        _nvidiaAppCandidates = nvidiaAppCandidates ?? DefaultNvidiaAppCandidates;
        _intelAppCandidates = intelAppCandidates ?? DefaultIntelAppCandidates;
    }

    public bool TryResolveInstalledApp(DriverIdentity identity, out OfficialSupportChannel channel)
    {
        if (DriverIdentityVendorMatcher.IsNvidia(identity))
        {
            var nvidiaPath = FindInstalledExecutable(_nvidiaAppCandidates);
            if (IsInstalledExecutable(nvidiaPath))
            {
                channel = BuildInstalledAppChannel(
                    "NVIDIA App",
                    nvidiaPath!,
                    "Открыть установленное приложение NVIDIA App или GeForce Experience.");
                return true;
            }
        }

        if (DriverIdentityVendorMatcher.IsIntel(identity))
        {
            var intelPath = FindInstalledExecutable(_intelAppCandidates);
            if (IsInstalledExecutable(intelPath))
            {
                channel = BuildInstalledAppChannel(
                    "Intel Driver & Support Assistant",
                    intelPath!,
                    "Открыть установленное приложение Intel Driver & Support Assistant.");
                return true;
            }
        }

        channel = new OfficialSupportChannel();
        return false;
    }

    private bool IsInstalledExecutable(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && _localAppValidator.Exists(path);
    }

    private string? FindInstalledExecutable(IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (IsInstalledExecutable(candidate))
                return candidate;
        }

        return null;
    }

    private static OfficialSupportChannel BuildInstalledAppChannel(string displayName, string target, string description)
    {
        return new OfficialSupportChannel
        {
            Type = OfficialSupportChannelType.InstalledOfficialApp,
            Target = target,
            IsInstalled = true,
            DisplayName = displayName,
            Description = description
        };
    }
}
