using System;
using System.IO;

namespace grzyClothTool.Helpers;

/// <summary>
/// Converts runtime and file-system exceptions into concise, localized user
/// messages. Technical details remain available through ErrorLogHelper.
/// </summary>
public static class ErrorMessageHelper
{
    public static string Friendly(Exception exception)
    {
        if (exception is UnauthorizedAccessException)
        {
            return LocalizationHelper.Translate("Нет доступа к выбранному пути. Проверьте права доступа.");
        }

        if (exception is FileNotFoundException || exception is DirectoryNotFoundException)
        {
            return LocalizationHelper.Translate("Файл или папка не найдены. Проверьте путь.");
        }

        if (exception is IOException)
        {
            return LocalizationHelper.Translate("Не удалось выполнить операцию с файлом. Проверьте, что файл не используется другим приложением.");
        }

        if (exception is ArgumentException || exception is FormatException)
        {
            return LocalizationHelper.Translate("Проверьте введённые данные и повторите действие.");
        }

        if (Contains(exception?.Message, "Семейство изменено") ||
            Contains(exception?.Message, "Collection was modified"))
        {
            return LocalizationHelper.Translate("Данные изменились во время операции. Повторите действие.");
        }

        return LocalizationHelper.Translate("Непредвиденная ошибка. Подробности записаны в журнал.");
    }

    public static string Clean(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return LocalizationHelper.Translate("Непредвиденная ошибка. Подробности записаны в журнал.");
        }

        string result = message;
        result = Replace(result,
            "Семейство изменено, выполнение операции перечисления невозможно.",
            LocalizationHelper.Translate("Данные изменились во время операции. Повторите действие."));
        result = Replace(result,
            "Collection was modified; enumeration operation may not execute.",
            LocalizationHelper.Translate("Данные изменились во время операции. Повторите действие."));
        result = Replace(result,
            "Access to the path is denied.",
            LocalizationHelper.Translate("Нет доступа к выбранному пути. Проверьте права доступа."));
        result = Replace(result,
            "The process cannot access the file because it is being used by another process.",
            LocalizationHelper.Translate("Не удалось выполнить операцию с файлом. Проверьте, что файл не используется другим приложением."));
        return result;
    }

    private static bool Contains(string? value, string fragment)
    {
        return value?.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string Replace(string source, string oldValue, string newValue)
    {
        return source.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);
    }
}
