using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using grzyClothTool.Models.Drawable;
using static grzyClothTool.Enums;

namespace grzyClothTool.Helpers;

/// <summary>
/// Builds paths below a build root while rejecting absolute/traversal group
/// names. Group names are user-editable and must never be able to redirect a
/// build outside the selected output directory.
/// </summary>
public static class SimplePathBuilder
{
    private static readonly char[] InvalidNameChars =
        Path.GetInvalidFileNameChars()
            .Concat(new[] { '<', '>', ':', '"', '|', '?', '*' })
            .Distinct()
            .ToArray();

    public static string BuildPath(GDrawable drawable, string buildPath, BuildResourceType? resourceType = null)
    {
        ArgumentNullException.ThrowIfNull(drawable);
        if (string.IsNullOrWhiteSpace(buildPath))
            throw new ArgumentException("Build path is required.", nameof(buildPath));

        string root = Path.GetFullPath(buildPath);
        var pathParts = new List<string> { root, "stream" };

        var genderFolder = drawable.Sex == SexType.male ? "[male]" : "[female]";
        pathParts.Add(genderFolder);

        if (resourceType == BuildResourceType.FiveM && !string.IsNullOrWhiteSpace(drawable.Group))
        {
            pathParts.AddRange(SplitAndValidateGroup(drawable.Group));
        }

        pathParts.Add(ValidateSegment(drawable.TypeName, nameof(drawable.TypeName)));

        string result = Path.GetFullPath(Path.Combine(pathParts.ToArray()));
        string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        string normalizedResult = result.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!normalizedResult.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The generated build path is outside the selected output directory.");

        return result;
    }

    public static bool IsValidGroupPath(string? group)
    {
        return TryNormalizeGroupPath(group, out _);
    }

    public static bool TryNormalizeGroupPath(string? group, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(group))
            return false;

        string value = group.Trim();
        if (Path.IsPathRooted(value) || value.StartsWith("\\\\", StringComparison.Ordinal))
            return false;

        string[] rawSegments = value.Split(
            ['/', '\\'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rawSegments.Length == 0)
            return false;

        var segments = new List<string>(rawSegments.Length);
        foreach (string segment in rawSegments)
        {
            if (segment is "." or "..")
                return false;
            segments.Add(ValidateSegment(segment, nameof(group)));
        }

        normalized = string.Join(Path.DirectorySeparatorChar, segments);
        return true;
    }

    private static IEnumerable<string> SplitAndValidateGroup(string group)
    {
        if (!TryNormalizeGroupPath(group, out string normalized))
            throw new InvalidOperationException($"Недопустимое имя группы: {group}");

        return normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string ValidateSegment(string? value, string parameterName)
    {
        string segment = value?.Trim() ?? string.Empty;
        if (segment.Length == 0 || segment is "." or "..")
            throw new InvalidOperationException($"Недопустимый сегмент пути: {value}");

        if (segment.IndexOfAny(InvalidNameChars) >= 0 || segment.Any(char.IsControl))
            throw new InvalidOperationException($"Недопустимый сегмент пути: {value}");

        return segment;
    }
}
