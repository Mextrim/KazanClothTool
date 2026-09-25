using grzyClothTool.Helpers;
using grzyClothTool.Models;
using grzyClothTool.Views;
using Material.Icons;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using AvalonDock.Themes;
using static grzyClothTool.Enums;

namespace grzyClothTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string AppVersion => LocalizationHelper.Translate("Версия") + ": " + UpdateHelper.GetCurrentVersion();
        public bool HasProject => _addonManager?.HasProject == true;
        private static MainWindow _instance;
        public static MainWindow Instance => _instance;
        private static NavigationHelper _navigationHelper;
        public static NavigationHelper NavigationHelper => _navigationHelper;

        private static AddonManager _addonManager;
        public static AddonManager AddonManager => _addonManager;
        private bool _topBarActionsBusy;

        private readonly static Dictionary<string, string> TempFoldersNames = new()
        {
            { "import", "KazanClothTool_import" },
            { "export", "KazanClothTool_export" },
            { "dragdrop", "KazanClothTool_dragdrop" }
        };

        public MainWindow()
        {
            InitializeComponent();
            FitWindowToWorkArea();
            LocalizationHelper.SetLanguage(PersistentSettingsHelper.Instance.Language, save: false);
            LocalizationHelper.LanguageChanged += (_, _) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged(nameof(AppVersion));
                    LocalizationHelper.ApplyTo(this);
                }));
            SettingsHelper.Preview3DAvailable = false;
            this.Visibility = Visibility.Hidden;

            _instance = this;
            _addonManager = new AddonManager();
            _addonManager.PropertyChanged += OnAddonManagerPropertyChanged;
            SetTopBarActionsEnabled(true);
            SaveHelper.AutoSaveProgress += OnAutoSaveProgress;
            SaveHelper.RemainingSecondsChanged += OnRemainingSecondsChanged;

            _navigationHelper = new NavigationHelper();
            _navigationHelper.RegisterPage("Home", () => new Home());
            _navigationHelper.RegisterPage("Project", () => new ProjectWindow());
            _navigationHelper.RegisterPage("Settings", () => new SettingsWindow());

            DataContext = _navigationHelper;
            _navigationHelper.Navigate("Home");
            LocalizationHelper.ApplyTo(_navigationHelper.CurrentPage);

            TempFoldersCleanup();

            FileHelper.GenerateReservedAssets();
            LogHelper.Init();
            LogHelper.LogMessageCreated += LogHelper_LogMessageCreated;
            ProgressHelper.ProgressStatusChanged += ProgressHelper_ProgressStatusChanged;

            SaveHelper.Init();

            CWHelper.DockedPreviewHost = PreviewHost;

            PreviewAnchorable.Closing += PreviewAnchorable_Closing;
            PreviewAnchorable.IsVisibleChanged += PreviewAnchorable_IsVisibleChanged;

            Dispatcher.BeginInvoke((Action)(async () =>
            {
                App.splashScreen.AddMessage(LocalizationHelper.Translate("Подготовка редактора одежды..."));

                while (App.splashScreen.MessageQueueCount > 0)
                {
                    await Task.Delay(2000);
                }

                await App.splashScreen.LoadComplete();

                try
                {
                    await Task.Run(CWHelper.Init);
                    SettingsHelper.Preview3DAvailable = CWHelper.IsPreviewAvailable;
                }
                catch (Exception ex)
                {
                    ErrorLogHelper.LogError("Ошибка инициализации 3D-просмотра", ex);
                }
            }));

            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
            this.StateChanged += MainWindow_StateChanged;
        }

        private void FitWindowToWorkArea()
        {
            Rect workArea = SystemParameters.WorkArea;
            double availableWidth = Math.Max(800, workArea.Width - 32);
            double availableHeight = Math.Max(560, workArea.Height - 32);

            Width = Math.Min(1440, availableWidth);
            Height = Math.Min(900, availableHeight);
            MinWidth = Math.Min(1000, availableWidth);
            MinHeight = Math.Min(640, availableHeight);
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Событие приходит от внешней панели и может проходить через кнопки.
            // Перетаскивание окна разрешаем только за пустую область заголовка.
            if (e.Handled ||
                e.ButtonState != MouseButtonState.Pressed ||
                IsInteractiveElement(e.OriginalSource))
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            if (WindowState == WindowState.Normal)
            {
                DragMove();
            }
        }

        private static bool IsInteractiveElement(object? originalSource)
        {
            if (originalSource is not DependencyObject current)
            {
                return false;
            }

            while (current is not null)
            {
                if (current is ButtonBase)
                {
                    return true;
                }

                current = current switch
                {
                    Visual visual => VisualTreeHelper.GetParent(visual),
                    ContentElement contentElement => ContentOperations.GetParent(contentElement),
                    _ => LogicalTreeHelper.GetParent(current)
                };
            }

            return false;
        }

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (MaximizeRestoreIcon == null)
            {
                return;
            }

            bool isMaximized = WindowState == WindowState.Maximized;
            MaximizeRestoreIcon.Kind = isMaximized
                ? MaterialIconKind.WindowRestore
                : MaterialIconKind.WindowMaximize;

            WindowFrame.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(12);

            string actionKey = isMaximized ? "Свернуть в окно" : "Развернуть";
            string actionText = LocalizationHelper.Translate(actionKey);
            MaximizeWindowButton.ToolTip = actionText;
            System.Windows.Automation.AutomationProperties.SetName(MaximizeWindowButton, actionText);
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                await SaveAsync();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LocalizationHelper.ApplyTo(this);
            PreviewAnchorable.Hide();
            
            App.ChangeTheme(PersistentSettingsHelper.Instance.Theme);

            CheckFirstRun();
        }

        private void OnAutoSaveProgress(double percentage)
        {
            Dispatcher.Invoke(() =>
            {
                if (percentage > 0 && SaveHelper.HasUnsavedChanges)
                {
                    AutoSaveIndicator.Visibility = Visibility.Visible;
                    AutoSaveIndicator.UpdateProgress(percentage);
                }
                else
                {
                    AutoSaveIndicator.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void OnRemainingSecondsChanged(int seconds)
        {
            Dispatcher.Invoke(() =>
            {
                AutoSaveIndicator.RemainingSeconds = seconds;
            });
        }

        private void CheckFirstRun()
        {
            if (PersistentSettingsHelper.Instance.IsFirstRun)
            {
                this.Hide();
                
                var setupWindow = new FirstRunSetupWindow
                {
                    Owner = null
                };

                LocalizationHelper.ApplyTo(setupWindow);
                bool? result = setupWindow.ShowDialog();
                
                this.Show();
                Activate();

                if (result == true && setupWindow.SetupCompleted &&
                    !string.IsNullOrEmpty(PersistentSettingsHelper.Instance.MainProjectsFolder))
                {
                    LogHelper.Log($"Папка проектов настроена: {PersistentSettingsHelper.Instance.MainProjectsFolder}", LogType.Info);
                }
            }
        }
        
        private void PreviewAnchorable_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            PreviewAnchorable.Hide();
            AddonManager.IsPreviewEnabled = false;
        }

        private void PreviewAnchorable_IsVisibleChanged(object sender, EventArgs e)
        {
            if (PreviewAnchorable.IsVisible)
            {
                PreviewHost?.InitializePreview();
            }
        }

        private void ProgressHelper_ProgressStatusChanged(object sender, ProgressMessageEventArgs e)
        {
            var visibility = e.Status switch
            {
                ProgressStatus.Start => Visibility.Visible,
                ProgressStatus.Stop => Visibility.Hidden,
                _ => Visibility.Collapsed
            };

            this.Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = visibility;
            });
        }

        private void LogHelper_LogMessageCreated(object sender, LogMessageEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                logBar.Text = e.Message;
                if (Enum.TryParse(e.TypeIcon, out MaterialIconKind iconKind))
                {
                    logBarIcon.Kind = iconKind;
                }
                logBarIcon.Visibility = Visibility.Visible;
            });
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = e.Uri.AbsoluteUri;
            p.Start();
        }

        //this is needed so window can be clicked anywhere to unfocus textbox
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this);
        }

        private void NavigateHome_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Home");
        }

        private void Navigation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string pageKey })
            {
                NavigateToPage(pageKey);
            }
        }

        private void NavigateToPage(string pageKey)
        {
            try
            {
                if (_navigationHelper == null || string.IsNullOrWhiteSpace(pageKey))
                {
                    return;
                }

                _navigationHelper.Navigate(pageKey);
                LocalizationHelper.ApplyTo(_navigationHelper.CurrentPage);
                Activate();
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось открыть раздел", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось открыть раздел «{0}»: {1}", pageKey, ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка навигации"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
        }

        private void SetTopBarActionsEnabled(bool enabled)
        {
            _topBarActionsBusy = !enabled;
            bool hasProject = HasProject;

            if (OpenProjectButton != null)
            {
                OpenProjectButton.IsEnabled = enabled;
            }

            if (ImportProjectButton != null)
            {
                ImportProjectButton.IsEnabled = enabled;
            }

            if (SaveProjectButton != null)
            {
                SaveProjectButton.IsEnabled = enabled && hasProject;
            }

            if (ExportProjectButton != null)
            {
                ExportProjectButton.IsEnabled = enabled && hasProject;
            }
        }

        private void OnAddonManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(AddonManager.HasProject) or nameof(AddonManager.ProjectName))
            {
                Dispatcher.BeginInvoke(new Action(() => SetTopBarActionsEnabled(!_topBarActionsBusy)));
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            SetTopBarActionsEnabled(false);
            try
            {
                await SaveAsync();
            }
            finally
            {
                SetTopBarActionsEnabled(true);
            }
        }

        private async Task SaveAsync()
        {
            if (!SaveHelper.HasUnsavedChanges)
            {
                LogHelper.Log("Нет несохранённых изменений", LogType.Info);
                return;
            }

            try
            {
                await SaveHelper.SaveAsync();
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось сохранить проект", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось сохранить проект: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка сохранения"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
        }

        public void HomeScreen_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                return;
            }

            // Reset selection before clearing collections so WPF can safely
            // detach property and selection callbacks from the old project.
            AddonManager.SelectedAddon = null;
            AddonManager.MoveMenuItems.Clear();
            AddonManager.Addons.Clear();
            AddonManager.ProjectName = string.Empty;
            AddonManager.IsExternalProject = false;
            AddonManager.Groups.Clear();
            AddonManager.Tags.Clear();
            AddonManager.IsPreviewEnabled = false;

            DuplicateDetector.Clear();

            SaveHelper.SetUnsavedChanges(false);

            _navigationHelper.Navigate("Home");

            LogHelper.Log(LocalizationHelper.Translate("Cleared data, moved to home screen"), LogType.Info);
        }

        public async void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            SetTopBarActionsEnabled(false);
            try
            {
                if (await OpenAddonAsync(true))
                {
                    NavigateToPage("Project");
                }
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось открыть набор одежды", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось открыть набор одежды: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка открытия"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                SetTopBarActionsEnabled(true);
            }
        }

        public async void AddAddon_Click(object sender, RoutedEventArgs e)
        {
            await AddAddonAsync(true);
            if (MainWindow.AddonManager.Addons.Count > 0)
            {
                _navigationHelper.Navigate("Project");
            }
        }

        public async Task<bool> OpenAddonAsync(bool shouldSetProjectName = false)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                return false;
            }

            OpenFileDialog metaFiles = new()
            {
                Title = LocalizationHelper.Translate("Выберите файлы .meta одежды"),
                Multiselect = true,
                Filter = LocalizationHelper.Translate("Файлы .meta одежды (*.meta)|*.meta")
            };

            if (metaFiles.ShowDialog() != true)
            {
                return false;
            }

            var validMetaFiles = new List<string>();
            var extractedProjectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalDrawableCount = 0;
            string suggestedProjectName = string.Empty;

            foreach (var dir in metaFiles.FileNames)
            {
                using (var reader = new StreamReader(dir))
                {
                    string firstLine = await reader.ReadLineAsync();
                    string secondLine = await reader.ReadLineAsync();

                    if ((firstLine == null || !firstLine.Contains("ShopPedApparel")) &&
                        (secondLine == null || !secondLine.Contains("ShopPedApparel")))
                    {
                        LogHelper.Log($"Файл пропущен: {dir}, вероятно, это не .meta одежды", LogType.Warning);
                        continue;
                    }
                }

                validMetaFiles.Add(dir);

                var addonName = Path.GetFileNameWithoutExtension(dir);
                string extractedName;
                
                if (addonName.StartsWith("mp_m_freemode_01_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_m_freemode_01_".Length..];
                }
                else if (addonName.StartsWith("mp_f_freemode_01_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_f_freemode_01_".Length..];
                }
                else if (addonName.StartsWith("mp_m_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_m_".Length..];
                }
                else if (addonName.StartsWith("mp_f_", StringComparison.OrdinalIgnoreCase))
                {
                    extractedName = addonName["mp_f_".Length..];
                }
                else
                {
                    extractedName = addonName;
                }
                
                extractedProjectNames.Add(extractedName);

                var dirPath = Path.GetDirectoryName(dir);
                var addonFileName = Path.GetFileNameWithoutExtension(dir);
                string genderPart = addonFileName.Contains("mp_m_freemode_01") ? "mp_m_freemode_01" : "mp_f_freemode_01";
                string addonNameWithoutGender = addonFileName.Replace(genderPart, "").TrimStart('_');
                
                var yddFiles = await Task.Run(() =>
                {
                    string pattern = $@"^{genderPart}(_p)?.*?{System.Text.RegularExpressions.Regex.Escape(addonNameWithoutGender)}\^";
                    var compiledPattern = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
                    
                    return Directory.GetFiles(dirPath, "*.ydd", SearchOption.AllDirectories)
                        .Where(f => compiledPattern.IsMatch(Path.GetFileName(f)))
                        .Count();
                });
                
                totalDrawableCount += yddFiles;
            }

            if (validMetaFiles.Count == 0)
            {
                Controls.CustomMessageBox.Show("Не выбрано подходящих .meta файлов одежды.", "Ошибка", Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
                return false;
            }

            if (extractedProjectNames.Count == 1)
            {
                suggestedProjectName = extractedProjectNames.First();
            }
            else if (extractedProjectNames.Count > 1)
            {
                var names = extractedProjectNames.ToList();
                var commonPrefix = FindCommonPrefix(names);
                
                if (!string.IsNullOrEmpty(commonPrefix) && commonPrefix.Length >= 3)
                {
                    suggestedProjectName = commonPrefix.TrimEnd('_');
                }
                else
                {
                    suggestedProjectName = names.First();
                }
            }

            if (totalDrawableCount == 0)
            {
                Controls.CustomMessageBox.Show(
                    "Для выбранных .meta файлов не найдены элементы одежды (.ydd).\n\n" +
                    "Проверьте, что файлы .ydd находятся в той же папке или во вложенных папках рядом с .meta.",
                    "Одежда не найдена",
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, 
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return false;
            }

            var dialog = ProjectSetupDialog.ShowForOpenAddon(this, suggestedProjectName, totalDrawableCount, validMetaFiles.Count);
            if (!dialog.Confirmed)
            {
                return false;
            }

            ProgressHelper.Start("Загрузка одежды...");
            bool success = false;
            try
            {
                PreviewHost?.ClearProjectSelection();
                 AddonManager.SelectedAddon = null;
                AddonManager.Addons = [];
                AddonManager.MoveMenuItems.Clear();
                AddonManager.SelectedAddon = null;
                AddonManager.IsExternalProject = !dialog.IsSelfContained;
                DuplicateDetector.Clear();

                int loadedAddonCount = 0;
                foreach (var metaFile in validMetaFiles)
                {
                    if (await AddonManager.LoadAddon(metaFile, shouldSetProjectName))
                    {
                        loadedAddonCount++;
                    }
                }

                if (loadedAddonCount == 0)
                {
                    Controls.CustomMessageBox.Show("Не удалось загрузить ни один набор одежды.", "Ошибка", Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
                    return false;
                }

                AddonManager.ProjectName = dialog.ProjectName;
                AddonManager.SelectedAddon = AddonManager.Addons.FirstOrDefault();
                string projectRoot = FileHelper.GetProjectRoot(PersistentSettingsHelper.Instance.MainProjectsFolder, dialog.ProjectName);
                FileHelper.SetCurrentProjectRoot(projectRoot);

                SaveHelper.SetUnsavedChanges(true);
                await SaveHelper.SaveAsync();
                PersistentSettingsHelper.Instance.AddRecentProject(
                    Path.Combine(projectRoot, SaveHelper.GetSaveFileName(dialog.IsSelfContained)),
                    dialog.ProjectName,
                    AddonManager.Addons.Sum(addon => addon.Drawables.Count),
                    AddonManager.Addons.Count,
                    dialog.IsSelfContained == false);

                success = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Ошибка загрузки набора одежды", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось загрузить набор одежды: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
                return false;
            }
            finally
            {
                ProgressHelper.Stop(success ? "Одежда загружена" : "Загрузка не завершена", success);
            }
        }

        public async Task AddAddonAsync(bool shouldSetProjectName = false)
        {
            OpenFileDialog metaFiles = new()
            {
                Title = LocalizationHelper.Translate("Выберите .meta файлы для добавления"),
                Multiselect = true,
                Filter = LocalizationHelper.Translate("Meta files (*.meta)|*.meta")
            };

            if (metaFiles.ShowDialog() != true)
            {
                return;
            }

            var validFiles = new List<string>();
            foreach (string file in metaFiles.FileNames)
            {
                using var reader = new StreamReader(file);
                string firstLine = await reader.ReadLineAsync() ?? string.Empty;
                string secondLine = await reader.ReadLineAsync() ?? string.Empty;
                if (firstLine.Contains("ShopPedApparel", StringComparison.OrdinalIgnoreCase) ||
                    secondLine.Contains("ShopPedApparel", StringComparison.OrdinalIgnoreCase))
                {
                    validFiles.Add(file);
                }
                else
                {
                    LogHelper.Log($"Файл пропущен: {Path.GetFileName(file)} не является .meta одежды", LogType.Warning);
                }
            }

            if (validFiles.Count == 0)
            {
                Controls.CustomMessageBox.Show("Не выбрано подходящих .meta файлов.", "Ошибка", Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            ProgressHelper.Start("Добавление одежды...");
            bool success = false;
            try
            {
                int loaded = 0;
                foreach (string file in validFiles)
                {
                    if (await AddonManager.LoadAddon(file, shouldSetProjectName))
                    {
                        loaded++;
                    }
                }

                success = loaded > 0;
                if (success)
                {
                    AddonManager.SelectedAddon ??= AddonManager.Addons.FirstOrDefault();
                    SaveHelper.SetUnsavedChanges(true);
                }
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось добавить одежду", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось добавить одежду: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                ProgressHelper.Stop(success ? "Одежда добавлена" : "Не удалось добавить одежду", success);
            }
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            SetTopBarActionsEnabled(false);
            try
            {
                if (await ImportProjectAsync(true))
                {
                    NavigateToPage("Project");
                }
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось импортировать проект", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось импортировать проект: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка импорта"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                SetTopBarActionsEnabled(true);
            }
        }

        public async Task<bool> ImportProjectAsync(bool shouldSetProjectName = false)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = LocalizationHelper.Translate("Импорт проекта KazanClothTool"),
                Filter = LocalizationHelper.Translate("Проект KazanClothTool (*.kctproject;*.gctproject)|*.kctproject;*.gctproject")
            };

            if (openFileDialog.ShowDialog() != true || !SaveHelper.CheckUnsavedChangesMessage())
            {
                return false;
            }

            string projectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
            if (string.IsNullOrWhiteSpace(projectsFolder))
            {
                Controls.CustomMessageBox.Show("Сначала укажите папку проектов в настройках.", "Настройка", Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return false;
            }

            string selectedPath = openFileDialog.FileName;
            string projectName = Path.GetFileNameWithoutExtension(selectedPath);
            if (projectName is "." or ".." || projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            string projectRoot;
            try
            {
                projectRoot = FileHelper.GetProjectRoot(projectsFolder, projectName);
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось определить путь импорта", ex);
                Controls.CustomMessageBox.Show(
                    ErrorMessageHelper.Friendly(ex),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
                return false;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), TempFoldersNames["import"]);
            string buildPath = Path.Combine(tempPath, projectName + "_" + Guid.NewGuid().ToString("N"));
            string zipPath = buildPath + ".zip";
            Directory.CreateDirectory(tempPath);

            ProgressHelper.Start("Импорт проекта...");
            bool success = false;
            try
            {
                await ObfuscationHelper.XORFile(selectedPath, zipPath);
                await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, buildPath));

                string[] metaFiles = Directory.GetFiles(buildPath, "*.meta", SearchOption.AllDirectories)
                    .Where(file => Path.GetFileName(file).Contains("mp_m_freemode", StringComparison.OrdinalIgnoreCase) ||
                                   Path.GetFileName(file).Contains("mp_f_freemode", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (metaFiles.Length == 0)
                {
                    throw new InvalidDataException("В архиве проекта не найдены .meta файлы одежды.");
                }

                PreviewHost?.ClearProjectSelection();
                 AddonManager.SelectedAddon = null;
                AddonManager.Addons = [];
                AddonManager.MoveMenuItems.Clear();
                AddonManager.SelectedAddon = null;
                AddonManager.Groups.Clear();
                AddonManager.Tags.Clear();
                AddonManager.ProjectName = projectName;
                AddonManager.IsExternalProject = false;
                DuplicateDetector.Clear();
                FileHelper.SetCurrentProjectRoot(projectRoot);

                int loaded = 0;
                foreach (string metaFile in metaFiles)
                {
                    if (await AddonManager.LoadAddon(metaFile))
                    {
                        loaded++;
                    }
                }

                if (loaded == 0)
                {
                    throw new InvalidDataException("Не удалось загрузить одежду из архива.");
                }

                AddonManager.SelectedAddon = AddonManager.Addons.FirstOrDefault();
                SaveHelper.SetUnsavedChanges(true);
                await SaveHelper.SaveAsync();
                PersistentSettingsHelper.Instance.AddRecentProject(
                    Path.Combine(projectRoot, SaveHelper.AutoSaveFileName),
                    projectName,
                    AddonManager.Addons.Sum(addon => addon.Drawables.Count),
                    AddonManager.Addons.Count);

                success = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Ошибка импорта", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось импортировать проект: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
                return false;
            }
            finally
            {
                ProgressHelper.Stop(success ? "Проект импортирован" : "Импорт не завершён", success);
                try
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    if (Directory.Exists(buildPath)) Directory.Delete(buildPath, true);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Не удалось удалить временные файлы импорта: {ex.Message}", LogType.Warning);
                }
            }
        }

        private async void ExportProject_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AddonManager.ProjectName))
            {
                Controls.CustomMessageBox.Show("Откройте или создайте проект перед экспортом.", "Нет проекта", Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly, Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            SetTopBarActionsEnabled(false);
            try
            {
                string savedProjectName = AddonManager.ProjectName;
            var saveFileDialog = new SaveFileDialog
            {
                Title = LocalizationHelper.Translate("Экспорт проекта KazanClothTool"),
                Filter = LocalizationHelper.Translate("Проект KazanClothTool (*.kctproject)|*.kctproject"),
                FileName = $"{savedProjectName}.kctproject",
                AddExtension = true
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            string tempPath = Path.Combine(Path.GetTempPath(), TempFoldersNames["export"]);
            string operationId = Guid.NewGuid().ToString("N");
            string projectName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
            string buildPath = Path.Combine(tempPath, projectName + "_" + operationId);
            string zipPath = buildPath + ".zip";
            string temporaryOutput = saveFileDialog.FileName + ".tmp-" + operationId;
            bool success = false;

            ProgressHelper.Start("Экспорт проекта...");
            try
            {
                await SaveHelper.SaveAsync();
                var builder = new BuildResourceHelper(projectName, buildPath, new Progress<int>(), BuildResourceType.FiveM, false);
                await builder.BuildFiveMResource();
                await Task.Run(() => ZipFile.CreateFromDirectory(buildPath, zipPath, CompressionLevel.Fastest, false));
                await ObfuscationHelper.XORFile(zipPath, temporaryOutput);
                File.Move(temporaryOutput, saveFileDialog.FileName, overwrite: true);
                success = true;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Ошибка экспорта", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось экспортировать проект: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                ProgressHelper.Stop(success ? "Проект экспортирован" : "Экспорт не завершён", success);
                try
                {
                    if (File.Exists(temporaryOutput)) File.Delete(temporaryOutput);
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    if (Directory.Exists(buildPath)) Directory.Delete(buildPath, true);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Не удалось удалить временные файлы экспорта: {ex.Message}", LogType.Warning);
                }
            }
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Ошибка экспорта", ex);
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось экспортировать проект: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка экспорта"),
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                SetTopBarActionsEnabled(true);
            }
        }

        // if main window is closed, close CW window too
        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (_addonManager != null)
            {
                _addonManager.PropertyChanged -= OnAddonManagerPropertyChanged;
            }
            PreviewHost?.ClosePreview();
            SaveHelper.Shutdown();
            LogHelper.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                e.Cancel = true;
            }
        }

        private void StatusBarItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
            {
                return;
            }

            LogHelper.OpenLogWindow();
        }

        private void LogsOpen_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.OpenLogWindow();
        }

        private void DuplicateInspector_Click(object sender, RoutedEventArgs e)
        {
            var inspector = new DuplicateInspectorWindow();
            inspector.ShowDialog();
        }

        private static void TempFoldersCleanup()
        {
            foreach (var tempName in TempFoldersNames.Values)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), tempName);
                try
                {
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogHelper.LogError($"Не удалось очистить временную папку {tempPath}", ex);
                }
            }
        }

        public void UpdateAvalonDockTheme(bool isDarkMode)
        {
            if (DockManager != null)
            {
                DockManager.Theme = isDarkMode ? new Vs2013DarkTheme() : new Vs2013LightTheme();
            }
        }

        private static string FindCommonPrefix(List<string> strings)
        {
            if (strings == null || strings.Count == 0)
                return string.Empty;

            if (strings.Count == 1)
                return strings[0];

            var prefix = strings[0];
            
            for (int i = 1; i < strings.Count; i++)
            {
                while (!strings[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    prefix = prefix[..^1];
                    if (string.IsNullOrEmpty(prefix))
                        return string.Empty;
                }
            }

            return prefix;
        }
    }
}
