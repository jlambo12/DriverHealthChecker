using System;
using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal static class DriverIdentityVendorMatcher
{
    private static readonly HashSet<string> NvidiaManufacturers = new(StringComparer.Ordinal)
    {
        "NVIDIA",
        "NVIDIA CORPORATION"
    };

    private static readonly HashSet<string> IntelManufacturers = new(StringComparer.Ordinal)
    {
        "INTEL",
        "INTEL CORPORATION"
    };

    private static readonly HashSet<string> AmdManufacturers = new(StringComparer.Ordinal)
    {
        "AMD",
        "ADVANCED MICRO DEVICES",
        "ADVANCED MICRO DEVICES, INC.",
        "ATI TECHNOLOGIES INC.",
        "ATI TECHNOLOGIES INC"
    };

    private static readonly string[] NvidiaVendorIdPrefixes =
    {
        "PCI\\VEN_10DE",
        "USB\\VID_0955"
    };

    private static readonly string[] IntelVendorIdPrefixes =
    {
        "PCI\\VEN_8086",
        "USB\\VID_8087",
        "USB\\VID_8086"
    };

    private static readonly string[] AmdVendorIdPrefixes =
    {
        "PCI\\VEN_1002",
        "PCI\\VEN_1022"
    };

    public static bool IsNvidia(DriverIdentity identity)
    {
        return MatchesManufacturer(identity, NvidiaManufacturers)
               || MatchesVendorId(identity, NvidiaVendorIdPrefixes);
    }

    public static bool IsIntel(DriverIdentity identity)
    {
        return MatchesManufacturer(identity, IntelManufacturers)
               || MatchesVendorId(identity, IntelVendorIdPrefixes);
    }

    public static bool IsAmd(DriverIdentity identity)
    {
        return MatchesManufacturer(identity, AmdManufacturers)
               || MatchesVendorId(identity, AmdVendorIdPrefixes);
    }

    private static bool MatchesManufacturer(DriverIdentity identity, HashSet<string> supportedManufacturers)
    {
        foreach (var candidate in EnumerateManufacturerCandidates(identity))
        {
            var normalizedCandidate = Normalize(candidate);
            if (!string.IsNullOrWhiteSpace(normalizedCandidate) && supportedManufacturers.Contains(normalizedCandidate))
                return true;
        }

        return false;
    }

    private static bool MatchesVendorId(DriverIdentity identity, IReadOnlyList<string> vendorIdPrefixes)
    {
        foreach (var candidate in EnumerateIdentifierCandidates(identity))
        {
            var normalizedCandidate = Normalize(candidate);
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
                continue;

            foreach (var prefix in vendorIdPrefixes)
            {
                if (normalizedCandidate.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<string?> EnumerateManufacturerCandidates(DriverIdentity identity)
    {
        yield return identity.NormalizedManufacturer;
        yield return identity.Manufacturer;
    }

    private static IEnumerable<string?> EnumerateIdentifierCandidates(DriverIdentity identity)
    {
        yield return identity.PnpDeviceId;

        foreach (var hardwareId in identity.HardwareIds)
            yield return hardwareId;

        foreach (var compatibleId in identity.CompatibleIds)
            yield return compatibleId;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToUpperInvariant();
    }
}
