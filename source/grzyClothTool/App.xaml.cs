using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Properties;
using grzyClothTool.Shared;
using grzyClothTool.Views;
using Material.Icons;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace grzyClothTool;

public partial class App : Application
{
    public static ISplashScreen? splashScreen;
    private ManualResetEvent? _resetSplashCreated;
    private Thread? _splashThread;

    // Kept for the optional Patreon account dialog. Plugin loading and telemetry
    // are intentionally disabled in KazanClothTool.
    public static IPatreonPlugin? patreonAuthPlugin;

    protected override void OnStartup(StartupEventArgs e)
    {
        _resetSplashCreated = new ManualResetEvent(false);

        _splashThread = new Thread(() =>
        {
            try
            {
                ShowSplash();
            }
            finally
            {
                _resetSplashCreated.Set();
            }
        })
        {
            IsBackground = true
        };
        _splashThread.SetApartmentState(ApartmentState.STA);
        _splashThread.Start();

        _resetSplashCreated.WaitOne();
        base.OnStartup(e);

        LocalizationHelper.SetLanguage(PersistentSettingsHelper.Instance.Language, save: false);
        ChangeTheme(PersistentSettingsHelper.Instance.Theme);
    }

    public App()
    {
        MaterialIconDataProvider.Instance = new CustomIconProvider();
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex)
        {
            return;
        }

        WriteErrorLog(ex);
    }

    private bool _fatalDialogVisible;

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLogHelper.LogError("Непредвиденная ошибка интерфейса", e.Exception);
        WriteErrorLog(e.Exception);
        e.Handled = true;

        if (_fatalDialogVisible)
        {
            return;
        }

        _fatalDialogVisible = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                Controls.CustomMessageBox.Show(
                    ErrorMessageHelper.Friendly(e.Exception),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                _fatalDialogVisible = false;
            }
        }), DispatcherPriority.Normal);
    }

    private static void WriteErrorLog(Exception exception)
    {
        try
        {
            string fileName = $"error-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";
            string path = Path.Combine(AppContext.BaseDirectory, fileName);
            File.WriteAllText(path, exception.ToString());
        }
        catch
        {
            // Error reporting must never take down the application.
        }
    }

    private void ShowSplash()
    {
        try
        {
            var animatedSplashScreenWindow = new grzyClothTool.Views.SplashScreen();
            splashScreen = animatedSplashScreenWindow;
            animatedSplashScreenWindow.Show();
            _resetSplashCreated?.Set();
            Dispatcher.Run();
        }
        catch (Exception ex)
        {
            // Splash is optional. A failure must not terminate the process or
            // leave the main window waiting forever for a message queue.
            splashScreen = null;
            WriteErrorLog(ex);
            _resetSplashCreated?.Set();
        }
    }

    public static void ChangeTheme(bool isDarkMode)
    {
        ChangeTheme(isDarkMode ? AppThemes.Dark : AppThemes.Light);
    }

    public static void ChangeLanguage(string languageCode)
    {
        LocalizationHelper.SetLanguage(languageCode);
    }

    public static void ChangeTheme(string themeKey)
    {
        AppThemeOption theme = AppThemes.Get(themeKey);
        string resourcePath = theme.Key switch
        {
            AppThemes.Purity => "Themes/Purity.xaml",
            AppThemes.DesignCode => "Themes/DesignCode.xaml",
            AppThemes.Horizon => "Themes/Horizon.xaml",
            AppThemes.MaterialUI => "Themes/MaterialUI.xaml",
            AppThemes.UntitledUI => "Themes/UntitledUI.xaml",
            AppThemes.AntDesign => "Themes/AntDesign.xaml",
            AppThemes.Carbon => "Themes/Carbon.xaml",
            AppThemes.CarbonGray100 => "Themes/CarbonGray100.xaml",
            AppThemes.Fluent => "Themes/Fluent.xaml",
            AppThemes.Radix => "Themes/Radix.xaml",
            AppThemes.RadixDark => "Themes/RadixDark.xaml",
            AppThemes.IOS => "Themes/IOS.xaml",
            AppThemes.IOSDark => "Themes/IOSDark.xaml",
            AppThemes.PrimeVue => "Themes/PrimeVue.xaml",
            AppThemes.Mantine => "Themes/Mantine.xaml",
            AppThemes.Bulma => "Themes/Bulma.xaml",
            AppThemes.Spectre => "Themes/Spectre.xaml",
            AppThemes.Shadcn => "Themes/Shadcn.xaml",
            AppThemes.Primer => "Themes/Primer.xaml",
            AppThemes.Light => "Themes/Light.xaml",
            AppThemes.Bootstrap => "Themes/Bootstrap.xaml",
            AppThemes.Material => "Themes/Material.xaml",
            AppThemes.Ocean => "Themes/Ocean.xaml",
            AppThemes.Sunset => "Themes/Sunset.xaml",
            AppThemes.Forest => "Themes/Forest.xaml",
            AppThemes.Neon => "Themes/Neon.xaml",
            AppThemes.Aurora => "Themes/Aurora.xaml",
            AppThemes.Ruby => "Themes/Ruby.xaml",
            AppThemes.Cobalt => "Themes/Cobalt.xaml",
            AppThemes.Sand => "Themes/Sand.xaml",
            AppThemes.Rose => "Themes/Rose.xaml",
            AppThemes.Mono => "Themes/Mono.xaml",
            _ => "Themes/Bootstrap.xaml"
        };

        var dictionary = new ResourceDictionary
        {
            Source = new Uri(resourcePath, UriKind.Relative)
        };

        Current.Resources.MergedDictionaries.Clear();
        Current.Resources.MergedDictionaries.Add(dictionary);
        grzyClothTool.MainWindow.Instance?.UpdateAvalonDockTheme(theme.IsDark);
    }
}
