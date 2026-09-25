using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace grzyClothTool.Helpers;

public sealed class AppThemeOption : INotifyPropertyChanged
{
    public AppThemeOption(
        string key,
        string name,
        string description,
        bool isDark,
        string accentHex,
        string surfaceHex,
        string secondaryHex)
    {
        Key = key;
        Name = name;
        Description = description;
        IsDark = isDark;
        AccentHex = accentHex;
        SurfaceHex = surfaceHex;
        SecondaryHex = secondaryHex;
        LocalizationHelper.LanguageChanged += (_, _) => OnLanguageChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsDark { get; }
    public string AccentHex { get; }
    public string SurfaceHex { get; }
    public string SecondaryHex { get; }

    public string LocalizedName => LocalizationHelper.Translate(Name);
    public string LocalizedDescription => LocalizationHelper.Translate(Description);

    public SolidColorBrush AccentBrush => CreateBrush(AccentHex);
    public SolidColorBrush SurfaceBrush => CreateBrush(SurfaceHex);
    public SolidColorBrush SecondaryBrush => CreateBrush(SecondaryHex);

    public override string ToString() => LocalizedName;

    private void OnLanguageChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedName)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedDescription)));
    }

    private static SolidColorBrush CreateBrush(string value)
    {
        if (ColorConverter.ConvertFromString(value) is Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        return Brushes.Transparent;
    }
}

public static class AppThemes
{
    public const string Purity = "Purity";
    public const string Dark = "Dark";
    public const string Light = "Light";
    public const string Bootstrap = "Bootstrap";
    public const string Material = "Material";
    public const string Ocean = "Ocean";
    public const string Sunset = "Sunset";
    public const string Forest = "Forest";
    public const string Neon = "Neon";
    public const string Aurora = "Aurora";
    public const string Ruby = "Ruby";
    public const string Cobalt = "Cobalt";
    public const string Sand = "Sand";
    public const string Rose = "Rose";
    public const string Mono = "Mono";

    public static IReadOnlyList<AppThemeOption> All { get; } =
    [
        new(Bootstrap, "Bootstrap 5", "Классический Bootstrap: синий #0d6efd, плоские карточки", false, "#0D6EFD", "#F8F9FA", "#6C757D"),
        new(Purity, "Чистая", "Мягкий Purity UI дашборд с фиолетовым акцентом", false, "#6C5CE7", "#F7F8FA", "#14B8A6"),
        new(Dark, "Графит", "Графит и бирюзовый акцент", true, "#22D3EE", "#090C12", "#8B5CF6"),
        new(Light, "Светлая", "Чистый и контрастный интерфейс", false, "#0891B2", "#F5F7FB", "#7C3AED"),
        new(Material, "Material Design", "Лавандовый и бирюзовый", true, "#D0BCFF", "#121016", "#03DAC6"),
        new(Ocean, "Океан", "Глубокий синий и морская бирюза", true, "#38BDF8", "#071522", "#2DD4BF"),
        new(Sunset, "Закат", "Тёплый Sunset-градиент", true, "#FB923C", "#1B1018", "#F472B6"),
        new(Forest, "Лес", "Холодный зелёный и лаймовый акцент", true, "#A3E635", "#0B1711", "#34D399"),
        new(Neon, "Неон", "Электрический magenta и cyan", true, "#E879F9", "#120B1B", "#22D3EE"),
        new(Aurora, "Аврора", "Северное сияние с мятным свечением", true, "#5EEAD4", "#071A1D", "#A78BFA"),
        new(Ruby, "Рубин", "Глубокий красный и розовый акцент", true, "#FB7185", "#1B0B12", "#F472B6"),
        new(Cobalt, "Кобальт", "Синий графит с ярким акцентом", true, "#60A5FA", "#0B1220", "#22D3EE"),
        new(Sand, "Песок", "Тёплая светлая палитра с терракотовым акцентом", false, "#EA580C", "#FFF8ED", "#0F766E"),
        new(Rose, "Роза", "Мягкий розовый интерфейс", false, "#DB2777", "#FFF5F8", "#7C3AED"),
        new(Mono, "Моно", "Минималистичная графитовая палитра", false, "#64748B", "#F3F4F6", "#111827")
    ];

    public static string Normalize(string? key)
    {
        return All.FirstOrDefault(theme => string.Equals(theme.Key, key, StringComparison.OrdinalIgnoreCase))?.Key
            ?? Bootstrap;
    }

    public static AppThemeOption Get(string? key)
    {
        string normalized = Normalize(key);
        return All.First(theme => theme.Key == normalized);
    }
}
