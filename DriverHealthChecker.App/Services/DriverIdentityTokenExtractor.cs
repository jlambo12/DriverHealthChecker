using System;
using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal interface IDriverIdentityTokenExtractor
{
    bool TryExtract(DriverIdentity identity, out DriverIdentityTokens tokens);
}

internal sealed class DriverIdentityTokenExtractor : IDriverIdentityTokenExtractor
{
    public bool TryExtract(DriverIdentity identity, out DriverIdentityTokens tokens)
    {
        foreach (var identifier in EnumerateIdentityCandidates(identity))
        {
            if (!TryExtractToken(identifier, "VEN_", out var vendorId))
                continue;

            if (!TryExtractToken(identifier, "DEV_", out var deviceId))
                continue;

            tokens = new DriverIdentityTokens
            {
                VendorId = vendorId,
                DeviceId = deviceId
            };
            return true;
        }

        tokens = new DriverIdentityTokens();
        return false;
    }

    private static IEnumerable<string?> EnumerateIdentityCandidates(DriverIdentity identity)
    {
        yield return identity.PnpDeviceId;

        foreach (var hardwareId in identity.HardwareIds)
            yield return hardwareId;

        foreach (var compatibleId in identity.CompatibleIds)
            yield return compatibleId;
    }

    private static bool TryExtractToken(string? identifier, string prefix, out string tokenValue)
    {
        tokenValue = string.Empty;
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        var segments = identifier.Split(['\\', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            if (!segment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (segment.Length != prefix.Length + 4)
                continue;

            var candidate = segment.Substring(prefix.Length, 4).ToUpperInvariant();
            if (!IsHexToken(candidate))
                continue;

            tokenValue = candidate;
            return true;
        }

        return false;
    }

    private static bool IsHexToken(string candidate)
    {
        foreach (var ch in candidate)
        {
            var isDigit = ch >= '0' && ch <= '9';
            var isUpperHex = ch >= 'A' && ch <= 'F';
            if (!isDigit && !isUpperHex)
                return false;
        }

        return candidate.Length == 4;
    }
}
