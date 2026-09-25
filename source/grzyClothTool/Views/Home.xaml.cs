using grzyClothTool.Constants;
using grzyClothTool.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views;

public partial class Home : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string _latestVersion;
    public string LatestVersion
    {
        get => _latestVersion;
        private set => SetProperty(ref _latestVersion, value);
    }

    private ObservableCollection<RecentProject> _recentlyOpened = [];
    public ObservableCollection<RecentProject> RecentlyOpened
    {
        get => _recentlyOpened;
        private set
        {
            _recentlyOpened = value ?? [];
            OnPropertyChanged(nameof(ShowNoRecentProjects));
        }
    }

    public bool ShowNoRecentProjects => RecentlyOpened.Count == 0;

    private readonly List<string> _quickTips =
    [
        "Выберите одежду слева и сразу увидите все доступные свойства и текстуры.",
        "3D-режим обновляет модель при выборе другого элемента одежды.",
        "Shift+Delete удаляет выбранную одежду без дополнительного диалога.",
        "Ctrl+Delete заменяет одежду на зарезервированный слот, сохраняя нумерацию.",
        "Экспортируйте текстуры в PNG или DDS из контекстного меню списка.",
        "Проекты автоматически сохраняются каждую минуту после изменения."
    ];

    public string QuickTip => LocalizationHelper.Translate(_quickTips[(Environment.TickCount & int.MaxValue) % _quickTips.Count]);

    public Home()
    {
        InitializeComponent();
        LocalizationHelper.ApplyTo(this);
        DataContext = this;

        LatestVersion = UpdateHelper.GetCurrentVersion();
        LoadRecentProjects();
        Loaded += Home_Loaded;
        Loaded += (_, _) => LocalizationHelper.ApplyTo(this);
        LocalizationHelper.LanguageChanged += (_, _) => OnPropertyChanged(nameof(QuickTip));
    }

    private void Home_Loaded(object sender, RoutedEventArgs e)
    {
        OnPropertyChanged(nameof(QuickTip));
        LoadRecentProjects();
    }

    private void LoadRecentProjects()
    {
        var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
        var validProjects = recentProjects
            .Where(project => !string.IsNullOrWhiteSpace(project.FilePath) && File.Exists(project.FilePath))
            .OrderByDescending(project => project.LastModified)
            .ToList();

        if (validProjects.Count != recentProjects.Count)
        {
            PersistentSettingsHelper.Instance.RecentlyOpenedProjects = validProjects;
        }

        RecentlyOpened = new ObservableCollection<RecentProject>(validProjects);
        OnPropertyChanged(nameof(RecentlyOpened));
    }

    private async void CreateNew_Click(object sender, RoutedEventArgs e)
    {
        if (!SaveHelper.CheckUnsavedChangesMessage())
        {
            return;
        }

        try
        {
            var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
            if (string.IsNullOrWhiteSpace(mainProjectsFolder))
            {
                Show("Сначала укажите папку проектов в настройках.", "Нужна настройка", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                return;
            }

            Directory.CreateDirectory(mainProjectsFolder);

            var dialog = ProjectSetupDialog.ShowForNewProject(Window.GetWindow(this));
            if (!dialog.Confirmed)
            {
                return;
            }

            var projectName = dialog.ProjectName.Trim();
            if (projectName is "." or ".." ||
                Path.IsPathRooted(projectName) ||
                projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Show("Название проекта содержит недопустимые символы.", "Неверное название", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                return;
            }

            string normalizedRoot = Path.GetFullPath(mainProjectsFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string projectFolder = Path.GetFullPath(Path.Combine(mainProjectsFolder, projectName));
            if (!projectFolder.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                Show("Путь проекта находится за пределами папки проектов.", "Неверное название", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                return;
            }
            if (Directory.Exists(projectFolder) && Directory.EnumerateFileSystemEntries(projectFolder).Any())
            {
                Show("Проект с таким названием уже существует. Выберите другое название.", "Проект уже существует", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                return;
            }

            Directory.CreateDirectory(projectFolder);
            FileHelper.SetCurrentProjectRoot(projectFolder);
            var isExternal = !dialog.IsSelfContained;
            if (!isExternal)
            {
                Directory.CreateDirectory(Path.Combine(projectFolder, GlobalConstants.ASSETS_FOLDER_NAME));
            }

            // Clear dependent UI state before disposing the selected addon.
            // WPF selection/property callbacks may enumerate the old collection.
            MainWindow.AddonManager.SelectedAddon = null;
            MainWindow.AddonManager.MoveMenuItems.Clear();
            MainWindow.AddonManager.Addons.Clear();
            MainWindow.AddonManager.IsPreviewEnabled = false;
            MainWindow.AddonManager.Groups.Clear();
            MainWindow.AddonManager.Tags.Clear();
            DuplicateDetector.Clear();
            MainWindow.AddonManager.ProjectName = projectName;
            MainWindow.AddonManager.IsExternalProject = isExternal;
            MainWindow.AddonManager.CreateAddon();

            var saveFileName = SaveHelper.GetSaveFileName(isExternal);
            PersistentSettingsHelper.Instance.AddRecentProject(
                Path.Combine(projectFolder, saveFileName),
                projectName,
                drawableCount: 0,
                addonCount: 1,
                isExternal: isExternal);

            SaveHelper.SetUnsavedChanges(true);
            if (!await SaveHelper.SaveAsync(force: true))
            {
                throw new IOException("Не удалось сохранить новый проект.");
            }
            LoadRecentProjects();

            LogHelper.Log($"Создан новый проект: {projectName}");
            MainWindow.NavigationHelper.Navigate("Project");
        }
        catch (Exception ex)
        {
            ErrorLogHelper.LogError("Не удалось создать проект", ex);
            string userMessage = ErrorMessageHelper.Friendly(ex);
            Show(
                LocalizationHelper.Format("Не удалось создать проект: {0}", userMessage),
                LocalizationHelper.Translate("Ошибка"),
                CustomMessageBoxButtons.OKOnly,
                CustomMessageBoxIcon.Error);
        }
    }

    private async void OpenAddon_Click(object sender, RoutedEventArgs e)
    {
        if (await MainWindow.Instance.OpenAddonAsync(true))
        {
            MainWindow.NavigationHelper.Navigate("Project");
        }
    }

    private async void ImportProject_Click(object sender, RoutedEventArgs e)
    {
        if (await MainWindow.Instance.ImportProjectAsync(true))
        {
            MainWindow.NavigationHelper.Navigate("Project");
        }
    }

    private async void OpenSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = LocalizationHelper.Translate("Открыть проект KazanClothTool"),
                Filter = LocalizationHelper.Translate("Проект KazanClothTool (*.json)|*.json|Все файлы (*.*)|*.*"),
                Multiselect = false
            };

            if (dialog.ShowDialog() != true || !SaveHelper.CheckUnsavedChangesMessage())
            {
                return;
            }

            await SaveHelper.LoadSaveFileAsync(dialog.FileName);
            SaveHelper.SetUnsavedChanges(false);
            LoadRecentProjects();
            MainWindow.NavigationHelper.Navigate("Project");
        }
        catch (Exception ex)
        {
            ErrorLogHelper.LogError("Не удалось открыть проект", ex);
            Show(
                LocalizationHelper.Format("Не удалось открыть проект: {0}", ErrorMessageHelper.Friendly(ex)),
                LocalizationHelper.Translate("Ошибка"),
                CustomMessageBoxButtons.OKOnly,
                CustomMessageBoxIcon.Error);
        }
    }

    private async void RecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string filePath })
        {
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                RemoveRecentProject(filePath);
                Show("Файл проекта больше не существует.", "Проект не найден", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Warning);
                return;
            }

            if (!SaveHelper.CheckUnsavedChangesMessage())
            {
                return;
            }

            await SaveHelper.LoadSaveFileAsync(filePath);
            SaveHelper.SetUnsavedChanges(false);
            LoadRecentProjects();
            MainWindow.NavigationHelper.Navigate("Project");
        }
        catch (Exception ex)
        {
            ErrorLogHelper.LogError("Не удалось открыть проект", ex);
            Show(
                LocalizationHelper.Format("Не удалось открыть проект: {0}", ErrorMessageHelper.Friendly(ex)),
                LocalizationHelper.Translate("Ошибка"),
                CustomMessageBoxButtons.OKOnly,
                CustomMessageBoxIcon.Error);
        }
    }

    private void RemoveRecentProject_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button { Tag: string filePath })
        {
            RemoveRecentProject(filePath);
        }
    }

    private void RemoveRecentProject(string filePath)
    {
        var projects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
        projects.RemoveAll(project => string.Equals(project.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        PersistentSettingsHelper.Instance.RecentlyOpenedProjects = projects;
        LoadRecentProjects();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.MainWindow?.Close();
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
