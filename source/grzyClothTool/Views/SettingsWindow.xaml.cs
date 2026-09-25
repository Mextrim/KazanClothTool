using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : UserControl, INotifyPropertyChanged
    { 
        public static string GTAVPath => CWHelper.GTAVPath;

        public static bool IsDarkMode => AppThemes.Get(PersistentSettingsHelper.Instance.Theme).IsDark;

        public IReadOnlyList<AppThemeOption> AvailableThemes => AppThemes.All;

        public IReadOnlyList<LanguageOption> AvailableLanguages => LocalizationHelper.AvailableLanguages;

        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (value == null || _selectedLanguage == value)
                {
                    return;
                }

                _selectedLanguage = value;
                App.ChangeLanguage(value.Code);
                LocalizationHelper.ApplyTo(this);
                LocalizationHelper.ApplyTo(Window.GetWindow(this));
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }

        private AppThemeOption _selectedTheme;
        public AppThemeOption SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (value == null || _selectedTheme == value)
                {
                    return;
                }

                _selectedTheme = value;
                PersistentSettingsHelper.Instance.Theme = value.Key;
                App.ChangeTheme(value.Key);
                OnPropertyChanged(nameof(SelectedTheme));
                OnPropertyChanged(nameof(IsDarkMode));
            }
        }

        public int[] TextureResolutionOptions { get; } = [128, 256, 512, 1024, 2048, 4096];

        private string _mainProjectsFolder;
        public string MainProjectsFolder
        {
            get
            {
                _mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                return _mainProjectsFolder;
            }
            set
            {
                if (_mainProjectsFolder != value)
                {
                    _mainProjectsFolder = value;
                    PersistentSettingsHelper.Instance.MainProjectsFolder = value;
                    OnPropertyChanged(nameof(MainProjectsFolder));
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SettingsWindow()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);
            Loaded += (_, _) =>
            {
                if (MainWindow.AddonManager != null)
                {
                    MainWindow.AddonManager.PropertyChanged -= OnAddonManagerPropertyChanged;
                    MainWindow.AddonManager.PropertyChanged += OnAddonManagerPropertyChanged;
                }
                LocalizationHelper.ApplyTo(this);
                UpdateBackButtonState();
            };
            Unloaded += (_, _) =>
            {
                if (MainWindow.AddonManager != null)
                {
                    MainWindow.AddonManager.PropertyChanged -= OnAddonManagerPropertyChanged;
                }
            };

            _mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
            _selectedLanguage = LocalizationHelper.GetLanguage(PersistentSettingsHelper.Instance.Language);
            _selectedTheme = AppThemes.Get(PersistentSettingsHelper.Instance.Theme);

            DataContext = this;
            if (MainWindow.AddonManager != null)
            {
                MainWindow.AddonManager.PropertyChanged += OnAddonManagerPropertyChanged;
            }
            UpdateBackButtonState();
        }

        private void OnAddonManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AddonManager.HasProject) or nameof(AddonManager.ProjectName))
            {
                Dispatcher.BeginInvoke(new Action(UpdateBackButtonState));
            }
        }

        private void UpdateBackButtonState()
        {
            bool hasProject = MainWindow.AddonManager?.HasProject == true;
            string key = hasProject ? "Вернуться в редактор" : "Вернуться на главную";
            BackButton.ToolTip = LocalizationHelper.Translate(key);
            System.Windows.Automation.AutomationProperties.SetName(BackButton, BackButton.ToolTip?.ToString() ?? string.Empty);
        }

        private void SettingsScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsScrollViewer.ScrollToVerticalOffset(0);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.AddonManager?.HasProject == true)
            {
                MainWindow.NavigationHelper.Navigate("Project");
            }
            else
            {
                MainWindow.NavigationHelper.Navigate("Home");
            }
        }

        private void OpenIconCatalog_Click(object sender, RoutedEventArgs e)
        {
            IconCatalogHelper.Open();
        }

        private void GTAVPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog selectedGTAPath = new()
            {
                Title = LocalizationHelper.Translate("Выберите папку GTA V"),
                Multiselect = false
            };

            if (selectedGTAPath.ShowDialog() != true)
            {
                return;
            }

            string exeFilePath = Path.Combine(selectedGTAPath.FolderName, "GTA5.exe");
            if (!File.Exists(exeFilePath))
            {
                Controls.CustomMessageBox.Show(
                    "В выбранной папке не найден файл GTA5.exe.",
                    "Некорректная папка GTA V",
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                CWHelper.SetGTAFolder(selectedGTAPath.FolderName);
                SettingsHelper.Preview3DAvailable = CWHelper.IsPreviewAvailable;
                if (SettingsHelper.Preview3DAvailable)
                {
                    MainWindow.Instance?.PreviewHost?.InitializePreviewInBackground();
                }

                OnPropertyChanged(nameof(GTAVPath));
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось настроить путь GTA V", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось настроить путь GTA V: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
        }

        private void MainProjectsFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog selectedFolder = new()
            {
                Title = LocalizationHelper.Translate("Выберите главную папку проектов"),
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(PersistentSettingsHelper.Instance.MainProjectsFolder) &&
                Directory.Exists(PersistentSettingsHelper.Instance.MainProjectsFolder))
            {
                selectedFolder.FolderName = PersistentSettingsHelper.Instance.MainProjectsFolder;
            }

            if (selectedFolder.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (PersistentSettingsHelper.IsRootDrive(selectedFolder.FolderName))
                {
                    Controls.CustomMessageBox.Show(
                        "Нельзя использовать корневой диск (например, C:\\) как папку проектов.\n\nВыберите или создайте вложенную папку.",
                        "Некорректная папка",
                        Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                        Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                    return;
                }

                Directory.CreateDirectory(selectedFolder.FolderName);
                string testFile = Path.Combine(selectedFolder.FolderName, ".kazan-cloth-tool-test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                MainProjectsFolder = selectedFolder.FolderName;
                LogHelper.Log($"Папка проектов обновлена: {selectedFolder.FolderName}", LogType.Info);
            }
            catch (UnauthorizedAccessException)
            {
                Controls.CustomMessageBox.Show(
                    "Нет доступа к папке. Выберите папку, в которую у вас есть права записи.",
                    "Ошибка",
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось настроить папку проектов", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось настроить папку проектов: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
        }

        private void AutoDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || checkBox.IsChecked != true || !SettingsHelper.Instance.AutoDeleteFiles)
            {
                return;
            }

            var result = Controls.CustomMessageBox.Show(
                "Автоудаление изменяет файлы на диске. Удалённые файлы нельзя восстановить.\n\nВключить эту функцию?",
                "Предупреждение",
                Controls.CustomMessageBox.CustomMessageBoxButtons.OKCancel,
                Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);

            if (result != Controls.CustomMessageBox.CustomMessageBoxResult.OK)
            {
                checkBox.IsChecked = false;
                SettingsHelper.Instance.AutoDeleteFiles = false;
            }
        }

        public void PatreonAccount_Click(object sender, RoutedEventArgs e)
        {
            var accountsWindow = new AccountsWindow();
            accountsWindow.ShowDialog();
        }

        private void ThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox { SelectedItem: AppThemeOption theme })
            {
                SelectedTheme = theme;
            }
        }

        public void ThemeModeChange_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton { IsChecked: bool value })
            {
                SelectedTheme = AppThemes.Get(value ? AppThemes.Dark : AppThemes.Light);
            }
        }

    }
}
