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
    public const string DesignCode = "DesignCode";
    public const string Horizon = "Horizon";
    public const string MaterialUI = "MaterialUI";
    public const string UntitledUI = "UntitledUI";
    public const string AntDesign = "AntDesign";
    public const string Carbon = "Carbon";
    public const string Fluent = "Fluent";
    public const string Radix = "Radix";
    public const string RadixDark = "RadixDark";
    public const string IOS = "IOS";
    public const string IOSDark = "IOSDark";
    public const string PrimeVue = "PrimeVue";
    public const string Mantine = "Mantine";
    public const string Bulma = "Bulma";
    public const string Spectre = "Spectre";
    public const string Shadcn = "Shadcn";
    public const string Primer = "Primer";
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
        new(Horizon, "Horizon UI", "Мягкий Soft UI дашборд: индиго #4318ff и голубые акценты", false, "#4318FF", "#F4F7FE", "#36BFFA"),
        new(MaterialUI, "Material UI", "Официальный MUI: синий #0072e5, фиолетовый и Material 3 формы", false, "#0072E5", "#F3F6F9", "#A259FF"),
        new(UntitledUI, "Untitled UI", "Untitled UI: фиолетовый #6941c6 и глубокий текст", false, "#6941C6", "#FFFFFF", "#0075FF"),
        new(AntDesign, "Ant Design", "Ant Design: синий #1677ff и геометрия 4/8/12", false, "#1677FF", "#F5F5F5", "#722ED1"),
        new(Carbon, "IBM Carbon", "IBM Carbon: синий #0f62fe и строгая сетка", false, "#0F62FE", "#F4F4F4", "#4589FF"),
        new(Fluent, "Fluent 2", "Microsoft Fluent 2: тёмный #1f1f1f и #479ef5", true, "#479EF5", "#1F1F1F", "#62ABF5"),
        new(Radix, "Radix Themes", "Radix Themes v3: индиго #3e63dd на светлой шкале", false, "#3E63DD", "#F9F9FB", "#12A594"),
        new(RadixDark, "Radix Themes Dark", "Radix Themes v3: тёмный режим #1c1e22 с индиго", true, "#3E63DD", "#1C1E22", "#2CC8B7"),
        new(IOS, "iOS 16", "Системные цвета iOS: синий #007aff и фон #f2f2f7", false, "#007AFF", "#F2F2F7", "#AF52DE"),
        new(IOSDark, "iOS 16 Dark", "Тёмный iOS: чёрный фон и #0a84ff", true, "#0A84FF", "#000000", "#BF5AF2"),
        new(PrimeVue, "PrimeVue Aura", "PrimeVue Aura: изумрудный #10b981", false, "#10B981", "#F8F9FA", "#6366F1"),
        new(Mantine, "Mantine", "Mantine: синий #228be6 и фиолетовый", false, "#228BE6", "#F8F9FA", "#7950F2"),
        new(Bulma, "Bulma", "Bulma: бирюзовый #00d1b2", false, "#00D1B2", "#F5F5F5", "#485FC7"),
        new(Spectre, "Spectre.css", "Spectre.css: минимализм и фиолетовый #5755d9", false, "#5755D9", "#F8F8F8", "#7B6CF6"),
        new(Shadcn, "shadcn/ui", "shadcn/ui: цинк и чёрные кнопки", false, "#18181B", "#FAFAFA", "#3B82F6"),
        new(Primer, "GitHub Primer", "GitHub Primer: синий #0969da и холст #f6f8fa", false, "#0969DA", "#F6F8FA", "#8250DF"),
        new(DesignCode, "DesignCode", "Тёмный DesignCode UI: индиго-поверхности и ледяной акцент", true, "#2F6BFF", "#050715", "#9ED0EE"),
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
