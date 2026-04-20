using System;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IDriverClassifier
{
    bool TryClassify(string name, string? manufacturer, out DriverCategory category, out string reason);
}

internal sealed class DriverClassifier : IDriverClassifier
{
    public bool TryClassify(string name, string? manufacturer, out DriverCategory category, out string reason)
    {
        var normalized = Normalize(name, manufacturer);

        category = DriverCategory.Unknown;
        reason = string.Empty;

        if (IsBlacklisted(normalized, out reason))
            return false;

        if (TryClassifyGpu(normalized, out reason))
            return SetResult(DriverCategory.Gpu, reason, out category, out reason);

        if (TryClassifyNetwork(normalized, out reason))
            return SetResult(DriverCategory.Network, reason, out category, out reason);

        if (TryClassifyStorage(normalized, out reason))
            return SetResult(DriverCategory.Storage, reason, out category, out reason);

        if (TryClassifyMainAudio(normalized, out reason))
            return SetResult(DriverCategory.AudioMain, reason, out category, out reason);

        if (TryClassifyExternalAudio(normalized, out reason))
            return SetResult(DriverCategory.AudioExternal, reason, out category, out reason);

        return false;
    }

    private static NormalizedDriverInfo Normalize(string name, string? manufacturer)
    {
        return new NormalizedDriverInfo(
            name.ToLowerInvariant(),
            (manufacturer ?? string.Empty).ToLowerInvariant());
    }

    private static bool SetResult(DriverCategory resolvedCategory, string resolvedReason, out DriverCategory category, out string reason)
    {
        category = resolvedCategory;
        reason = resolvedReason;
        return true;
    }

    private static bool IsBlacklisted(NormalizedDriverInfo info, out string reason)
    {
        var driverName = info.Name;

        foreach (var group in DriverRules.BlacklistGroups)
        {
            var matched = group.Terms.FirstOrDefault(term => driverName.Contains(term));
            if (string.IsNullOrWhiteSpace(matched))
            {
                continue;
            }

            // "Audio CoProcessor Device" should stay visible as an external audio candidate.
            if (matched == "processor" && driverName.Contains("audio"))
            {
                continue;
            }

            reason = $"Скрыто: {group.GroupName} ('{matched}')";
            return true;
        }

        if (driverName.Contains("nvidia") && driverName.Contains("audio"))
        {
            reason = "Скрыто: виртуальное/служебное NVIDIA Audio устройство";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryClassifyGpu(NormalizedDriverInfo info, out string reason)
    {
        foreach (var gpuRule in DriverRules.GpuKeywordRules)
        {
            if (!info.Name.Contains(gpuRule.Term))
                continue;

            // Avoid classifying NVIDIA audio components as GPU.
            if (gpuRule.Term == "nvidia" && info.Name.Contains("audio"))
                continue;

            reason = gpuRule.Reason;
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryClassifyNetwork(NormalizedDriverInfo info, out string reason)
    {
        if (TryMatchByTerms(info.Name, DriverRules.NetworkTerms, "Сеть", out reason))
            return true;

        if (info.Manufacturer.Contains("intel") && ContainsAny(info.Name, "wireless", "bluetooth", "ax", "be200", "be202"))
        {
            reason = "Сеть: Intel wireless/bluetooth эвристика";
            return true;
        }

        if (info.Manufacturer.Contains("realtek") && ContainsAny(info.Name, "rtl", "family controller", "gaming"))
        {
            reason = "Сеть: Realtek family/RTL эвристика";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryClassifyStorage(NormalizedDriverInfo info, out string reason)
    {
        return TryMatchByTerms(info.Name, DriverRules.StorageTerms, "Хранение", out reason);
    }

    private static bool TryClassifyMainAudio(NormalizedDriverInfo info, out string reason)
    {
        if (TryMatchByTerms(info.Name, DriverRules.MainAudioTerms, "Аудио", out reason))
            return true;

        if ((info.Name.Contains("realtek") && info.Name.Contains("audio")) ||
            (info.Manufacturer.Contains("realtek") && info.Name.Contains("audio")))
        {
            reason = "Аудио: эвристика Realtek";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryClassifyExternalAudio(NormalizedDriverInfo info, out string reason)
    {
        var brand = DriverRules.ExternalAudioBrands.FirstOrDefault(info.Name.Contains);
        if (!string.IsNullOrWhiteSpace(brand))
        {
            reason = $"Внешнее аудио: бренд '{brand}'";
            return true;
        }

        if (info.Name.Contains("usb audio device"))
        {
            reason = string.Empty;
            return false;
        }

        if (ContainsAny(info.Name, "audio") || info.Manufacturer.Contains("audio"))
        {
            var shouldSkip = ContainsAny(info.Name, "realtek", "nvidia", "virtual", "endpoint", "controller");
            if (!shouldSkip)
            {
                reason = "Внешнее аудио: обобщённая аудио-эвристика";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryMatchByTerms(string source, string[] terms, string areaName, out string reason)
    {
        var matchedTerm = terms.FirstOrDefault(source.Contains);
        if (!string.IsNullOrWhiteSpace(matchedTerm))
        {
            reason = $"{areaName}: ключевое слово '{matchedTerm}'";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool ContainsAny(string source, params string[] terms)
    {
        return terms.Any(source.Contains);
    }

    private readonly record struct NormalizedDriverInfo(string Name, string Manufacturer);
}
