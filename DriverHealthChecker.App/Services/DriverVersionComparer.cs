using System;
using System.Collections.Generic;

namespace DriverHealthChecker.App;

internal interface IDriverVersionComparer
{
    int Compare(string? left, string? right);
}

internal sealed class DriverVersionComparer : IDriverVersionComparer
{
    public int Compare(string? left, string? right)
    {
        var leftSegments = ParseSegments(left);
        var rightSegments = ParseSegments(right);
        var max = Math.Max(leftSegments.Count, rightSegments.Count);

        for (var i = 0; i < max; i++)
        {
            var l = i < leftSegments.Count ? leftSegments[i] : 0;
            var r = i < rightSegments.Count ? rightSegments[i] : 0;
            if (l != r)
                return l.CompareTo(r);
        }

        return 0;
    }

    private static List<int> ParseSegments(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) || version == "-")
            return [];

        var result = new List<int>();
        var parts = version.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (int.TryParse(part, out var value) && value >= 0)
                result.Add(value);
            else
                result.Add(0);
        }

        return result;
    }
}
