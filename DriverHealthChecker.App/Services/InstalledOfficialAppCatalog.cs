using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

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

    private static readonly RegistryValueProbe[] DefaultNvidiaRegistryProbes = CreateAppPathRegistryProbes(
        "NVIDIA App.exe",
        "NVIDIA GeForce Experience.exe");

    private static readonly RegistryValueProbe[] DefaultIntelRegistryProbes = CreateAppPathRegistryProbes(
        "DSATray.exe",
        "DSAService.exe",
        "IntelDriverSupportAssistant.exe");

    private readonly ILocalAppValidator _localAppValidator;
    private readonly IReadOnlyList<string> _nvidiaAppCandidates;
    private readonly IReadOnlyList<string> _intelAppCandidates;
    private readonly IReadOnlyList<RegistryValueProbe> _nvidiaRegistryProbes;
    private readonly IReadOnlyList<RegistryValueProbe> _intelRegistryProbes;

    public InstalledOfficialAppCatalog()
        : this(
            new LocalAppValidator(),
            DefaultNvidiaAppCandidates,
            DefaultIntelAppCandidates,
            DefaultNvidiaRegistryProbes,
            DefaultIntelRegistryProbes)
    {
    }

    internal InstalledOfficialAppCatalog(
        ILocalAppValidator localAppValidator,
        IReadOnlyList<string>? nvidiaAppCandidates = null,
        IReadOnlyList<string>? intelAppCandidates = null,
        IReadOnlyList<RegistryValueProbe>? nvidiaRegistryProbes = null,
        IReadOnlyList<RegistryValueProbe>? intelRegistryProbes = null)
    {
        _localAppValidator = localAppValidator;
        _nvidiaAppCandidates = nvidiaAppCandidates ?? DefaultNvidiaAppCandidates;
        _intelAppCandidates = intelAppCandidates ?? DefaultIntelAppCandidates;
        _nvidiaRegistryProbes = nvidiaRegistryProbes ?? DefaultNvidiaRegistryProbes;
        _intelRegistryProbes = intelRegistryProbes ?? DefaultIntelRegistryProbes;
    }

    public bool TryResolveInstalledApp(DriverIdentity identity, out OfficialSupportChannel channel)
    {
        if (DriverIdentityVendorMatcher.IsNvidia(identity))
        {
            var nvidiaPath = FindInstalledExecutable(_nvidiaAppCandidates, _nvidiaRegistryProbes);
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
            var intelPath = FindInstalledExecutable(_intelAppCandidates, _intelRegistryProbes);
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

    private string? FindInstalledExecutable(IReadOnlyList<string> pathCandidates, IReadOnlyList<RegistryValueProbe> registryProbes)
    {
        var pathCandidate = FindInstalledExecutableFromPaths(pathCandidates);
        if (IsInstalledExecutable(pathCandidate))
            return pathCandidate;

        return FindInstalledExecutableFromRegistry(registryProbes);
    }

    private string? FindInstalledExecutableFromPaths(IReadOnlyList<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (IsInstalledExecutable(candidate))
                return candidate;
        }

        return null;
    }

    private string? FindInstalledExecutableFromRegistry(IReadOnlyList<RegistryValueProbe> probes)
    {
        foreach (var probe in probes)
        {
            var path = TryReadRegistryValue(probe);
            if (IsInstalledExecutable(path))
                return path;
        }

        return null;
    }

    private static string? TryReadRegistryValue(RegistryValueProbe probe)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(probe.Hive, probe.View);
            using var subKey = baseKey.OpenSubKey(probe.KeyPath);
            if (subKey == null)
                return null;

            return NormalizeExecutablePath(subKey.GetValue(probe.ValueName)?.ToString());
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Ошибка при чтении реестра для {probe.KeyPath}.", ex);
            return null;
        }
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

    private static RegistryValueProbe[] CreateAppPathRegistryProbes(params string[] executableNames)
    {
        var probes = new List<RegistryValueProbe>();
        var views = new[] { RegistryView.Registry64, RegistryView.Registry32 };
        var hives = new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser };

        foreach (var executableName in executableNames)
        {
            foreach (var hive in hives)
            {
                foreach (var view in views)
                {
                    probes.Add(new RegistryValueProbe(
                        hive,
                        view,
                        $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{executableName}",
                        string.Empty));
                }
            }
        }

        return probes.ToArray();
    }

    private static string? NormalizeExecutablePath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        var cleaned = rawPath.Trim().Trim('"');
        var commaIndex = cleaned.IndexOf(',');
        if (commaIndex > 0)
            cleaned = cleaned[..commaIndex];

        return cleaned;
    }
}

internal readonly record struct RegistryValueProbe(RegistryHive Hive, RegistryView View, string KeyPath, string ValueName);
