using System;
using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal static class DriverIdentityVendorMatcher
{
    private const string NvidiaVendor = "NVIDIA";
    private const string IntelVendor = "INTEL";
    private const string AmdVendor = "AMD";

    private static readonly IReadOnlyDictionary<string, string> ManufacturerToVendor = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["NVIDIA"] = NvidiaVendor,
        ["NVIDIA CORPORATION"] = NvidiaVendor,
        ["INTEL"] = IntelVendor,
        ["INTEL CORPORATION"] = IntelVendor,
        ["AMD"] = AmdVendor,
        ["ADVANCED MICRO DEVICES"] = AmdVendor,
        ["ADVANCED MICRO DEVICES, INC."] = AmdVendor,
        ["ATI TECHNOLOGIES INC."] = AmdVendor,
        ["ATI TECHNOLOGIES INC"] = AmdVendor
    };

    private static readonly VendorIdentifierMapping[] VendorIdMappings =
    {
        new("PCI\\VEN_10DE", NvidiaVendor),
        new("USB\\VID_0955", NvidiaVendor),
        new("PCI\\VEN_8086", IntelVendor),
        new("USB\\VID_8087", IntelVendor),
        new("USB\\VID_8086", IntelVendor),
        new("PCI\\VEN_1002", AmdVendor),
        new("PCI\\VEN_1022", AmdVendor)
    };

    public static bool IsNvidia(DriverIdentity identity) => IsVendor(identity, NvidiaVendor);

    public static bool IsIntel(DriverIdentity identity) => IsVendor(identity, IntelVendor);

    public static bool IsAmd(DriverIdentity identity) => IsVendor(identity, AmdVendor);

    internal static bool TryResolveVendor(DriverIdentity identity, out string vendor)
    {
        if (TryResolveVendorFromIdentifier(identity.PnpDeviceId, out vendor))
            return true;

        if (TryResolveVendorFromIdentifiers(identity.HardwareIds, out vendor))
            return true;

        if (TryResolveVendorFromIdentifiers(identity.CompatibleIds, out vendor))
            return true;

        return TryResolveVendorFromManufacturer(identity.NormalizedManufacturer, out vendor);
    }

    private static bool IsVendor(DriverIdentity identity, string vendor)
    {
        return TryResolveVendor(identity, out var resolvedVendor)
               && string.Equals(resolvedVendor, vendor, StringComparison.Ordinal);
    }

    private static bool TryResolveVendorFromIdentifiers(IEnumerable<string> candidates, out string vendor)
    {
        foreach (var candidate in candidates)
        {
            if (TryResolveVendorFromIdentifier(candidate, out vendor))
                return true;
        }

        vendor = string.Empty;
        return false;
    }

    private static bool TryResolveVendorFromIdentifier(string? candidate, out string vendor)
    {
        var normalizedCandidate = Normalize(candidate);
        if (string.IsNullOrWhiteSpace(normalizedCandidate))
        {
            vendor = string.Empty;
            return false;
        }

        foreach (var mapping in VendorIdMappings)
        {
            if (MatchesIdentifier(normalizedCandidate, mapping.IdentifierToken))
            {
                vendor = mapping.Vendor;
                return true;
            }
        }

        vendor = string.Empty;
        return false;
    }

    private static bool TryResolveVendorFromManufacturer(string? normalizedManufacturer, out string vendor)
    {
        var manufacturer = Normalize(normalizedManufacturer);
        if (ManufacturerToVendor.TryGetValue(manufacturer, out vendor!))
            return true;

        vendor = string.Empty;
        return false;
    }

    private static bool MatchesIdentifier(string candidate, string identifierToken)
    {
        return string.Equals(candidate, identifierToken, StringComparison.Ordinal)
               || candidate.StartsWith(identifierToken + "&", StringComparison.Ordinal);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToUpperInvariant();
    }
}

internal readonly record struct VendorIdentifierMapping(string IdentifierToken, string Vendor);
