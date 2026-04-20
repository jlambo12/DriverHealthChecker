using System;
using System.Linq;

namespace DriverHealthChecker.App;

internal interface IDriverClassifier
{
    bool TryClassify(string name, string? manufacturer, out string category, out string reason);
}

internal sealed class DriverClassifier : IDriverClassifier
{
    public bool TryClassify(string name, string? manufacturer, out string category, out string reason)
    {
        category = string.Empty;
        reason = string.Empty;

        var n = name.ToLowerInvariant();
        var m = (manufacturer ?? string.Empty).ToLowerInvariant();

        if (IsBlacklisted(n, out reason))
            return false;

        if (IsGpu(n, out reason))
        {
            category = "GPU";
            return true;
        }

        if (IsNetwork(n, m, out reason))
        {
            category = "Network";
            return true;
        }

        if (IsStorage(n, out reason))
        {
            category = "Storage";
            return true;
        }

        if (IsMainAudio(n, m, out reason))
        {
            category = "AudioMain";
            return true;
        }

        if (IsExternalAudio(n, m, out reason))
        {
            category = "AudioExternal";
            return true;
        }

        return false;
    }

    private static bool IsBlacklisted(string n, out string reason)
    {
        foreach (var group in DriverRules.BlacklistGroups)
        {
            var matched = group.Terms.FirstOrDefault(n.Contains);
            if (string.IsNullOrWhiteSpace(matched))
            {
                continue;
            }

            // "Audio CoProcessor Device" should stay visible as an external audio candidate.
            if (matched == "processor" && n.Contains("audio"))
            {
                continue;
            }

            reason = $"Скрыто: {group.GroupName} ('{matched}')";
            return true;
        }

        if (n.Contains("nvidia") && n.Contains("audio"))
        {
            reason = "Скрыто: виртуальное/служебное NVIDIA Audio устройство";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsGpu(string n, out string reason)
    {
        foreach (var gpuRule in DriverRules.GpuKeywordRules)
        {
            if (!n.Contains(gpuRule.Term))
            {
                continue;
            }

            // Avoid classifying NVIDIA audio components as GPU.
            if (gpuRule.Term == "nvidia" && n.Contains("audio"))
            {
                continue;
            }

            reason = gpuRule.Reason;
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsNetwork(string n, string m, out string reason)
    {
        var matchedTerm = DriverRules.NetworkTerms.FirstOrDefault(n.Contains);
        if (!string.IsNullOrWhiteSpace(matchedTerm))
        {
            reason = $"Сеть: ключевое слово '{matchedTerm}'";
            return true;
        }

        if (m.Contains("intel") && (n.Contains("wireless") || n.Contains("bluetooth") || n.Contains("ax") || n.Contains("be200") || n.Contains("be202")))
        {
            reason = "Сеть: Intel wireless/bluetooth эвристика";
            return true;
        }

        if (m.Contains("realtek") && (n.Contains("rtl") || n.Contains("family controller") || n.Contains("gaming")))
        {
            reason = "Сеть: Realtek family/RTL эвристика";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsStorage(string n, out string reason)
    {
        var matchedTerm = DriverRules.StorageTerms.FirstOrDefault(n.Contains);
        if (!string.IsNullOrWhiteSpace(matchedTerm))
        {
            reason = $"Хранение: ключевое слово '{matchedTerm}'";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsMainAudio(string n, string m, out string reason)
    {
        var matchedTerm = DriverRules.MainAudioTerms.FirstOrDefault(n.Contains);
        if (!string.IsNullOrWhiteSpace(matchedTerm))
        {
            reason = $"Аудио: ключевое слово '{matchedTerm}'";
            return true;
        }

        if ((n.Contains("realtek") && n.Contains("audio")) ||
            (m.Contains("realtek") && n.Contains("audio")))
        {
            reason = "Аудио: эвристика Realtek";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool IsExternalAudio(string n, string m, out string reason)
    {
        var brand = DriverRules.ExternalAudioBrands.FirstOrDefault(n.Contains);
        if (!string.IsNullOrWhiteSpace(brand))
        {
            reason = $"Внешнее аудио: бренд '{brand}'";
            return true;
        }

        if (n.Contains("usb audio device"))
        {
            reason = string.Empty;
            return false;
        }

        if ((n.Contains("audio") || m.Contains("audio")) &&
            !n.Contains("realtek") &&
            !n.Contains("nvidia") &&
            !n.Contains("virtual") &&
            !n.Contains("endpoint") &&
            !n.Contains("controller"))
        {
            reason = "Внешнее аудио: обобщённая аудио-эвристика";
            return true;
        }

        reason = string.Empty;
        return false;
    }
}
