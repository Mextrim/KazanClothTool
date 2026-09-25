using System.Text.RegularExpressions;

namespace grzyClothTool.Helpers;

public static class ProjectNameValidator
{
    private static readonly Regex ValidNamePattern = new("^[a-z0-9_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? Validate(string? projectName)
    {
        string name = projectName?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return "Название проекта не может быть пустым";
        }

        if (name.Length < 3)
        {
            return "Название проекта должно содержать не менее 3 символов";
        }

        if (name.Length > 50)
        {
            return "Название проекта не может быть длиннее 50 символов";
        }

        if (!ValidNamePattern.IsMatch(name))
        {
            return "Название проекта может содержать только строчные буквы, цифры и подчёркивания";
        }

        return null;
    }

    public static bool IsValid(string? projectName) => Validate(projectName) == null;
}
