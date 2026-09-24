using grzyClothTool.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static grzyClothTool.Controls.CustomMessageBox;
using Timer = System.Timers.Timer;

namespace grzyClothTool.Helpers;

public class SaveFile
{
    public string FileName { get; set; }
    public DateTime SaveDate { get; set; }
}

    public static class SaveHelper
    {
        public const string AutoSaveFileName = "autosave.json";
        public const string AutoSaveExternalFileName = "autosave.external.json";
        public static string GetSaveFileName(bool isExternalProject)
        {
            return isExternalProject ? AutoSaveExternalFileName : AutoSaveFileName;
        }
        
        public static bool ProjectExists(string mainProjectsFolder, string projectName, out bool isExternal)
        {
            isExternal = false;
            
            if (string.IsNullOrEmpty(mainProjectsFolder) || string.IsNullOrEmpty(projectName))
                return false;
                
            var projectPath = Path.Combine(mainProjectsFolder, projectName.Trim());
            
            if (File.Exists(Path.Combine(projectPath, AutoSaveFileName)))
                return true;
                
            if (File.Exists(Path.Combine(projectPath, AutoSaveExternalFileName)))
            {
                isExternal = true;
                return true;
            }
            
            return false;
        }

    public static string SavesPath { get; private set; }
    private static Timer _timer;
    public static event Action SaveCreated;

    public static event Action<double> AutoSaveProgress;
    public static event Action<int> RemainingSecondsChanged;
    private static int _autoSaveInterval = 60000; // 60 seconds
    private static int _elapsedTime = 0;

    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static int _changeVersion;

    private static bool _hasUnsavedChanges;
    private static int _pauseCount;

    public static bool HasUnsavedChanges => Volatile.Read(ref _hasUnsavedChanges);
    public static bool SavingPaused => Volatile.Read(ref _pauseCount) > 0;

    public static void PauseSaving() => Interlocked.Increment(ref _pauseCount);

    public static void ResumeSaving()
    {
        while (true)
        {
            int current = Volatile.Read(ref _pauseCount);
            if (current == 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _pauseCount, current - 1, current) == current)
            {
                return;
            }
        }
    }

    public static JsonSerializerOptions SerializerOptions
    {
        get 
        { 
            return new JsonSerializerOptions { WriteIndented = true };
        }
    }

    static SaveHelper()
    {
        var appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        SavesPath = Path.Combine(appdataPath, "KazanClothTool", "saves");

        try
        {
            Directory.CreateDirectory(SavesPath);
        }
        catch
        {
            SavesPath = Path.Combine(Path.GetTempPath(), "KazanClothTool", "saves");
            Directory.CreateDirectory(SavesPath);
        }
    }

    public static void Init()
    {
        if (_timer != null)
        {
            return;
        }

        _timer = new Timer(1000);
        _timer.Elapsed += OnAutoSaveTick;
        _timer.AutoReset = true;
        _timer.Start();
    }

    public static void Shutdown()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    private static async void OnAutoSaveTick(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (SavingPaused || !HasUnsavedChanges)
        {
            _elapsedTime = 0;
            AutoSaveProgress?.Invoke(0);
            RemainingSecondsChanged?.Invoke(0);
            return;
        }

        _elapsedTime += (int)_timer.Interval;
        double percentage = ((double)_elapsedTime / _autoSaveInterval) * 75.0;
        int remainingSeconds = Math.Max(0, (_autoSaveInterval - _elapsedTime) / 1000);
        
        if (_elapsedTime >= _autoSaveInterval)
        {
            _elapsedTime = 0;
            await SaveAsync();
            RemainingSecondsChanged?.Invoke(0);
            return;
        }
        AutoSaveProgress?.Invoke(percentage);
        RemainingSecondsChanged?.Invoke(remainingSeconds);
    }

    public static async Task SaveAsync()
    {
        var mainWindow = MainWindow.Instance;
        if (!HasUnsavedChanges || SavingPaused || mainWindow == null || MainWindow.AddonManager == null)
        {
            return;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (!HasUnsavedChanges || SavingPaused)
            {
                return;
            }

            var timer = Stopwatch.StartNew();
            LogHelper.Log("Начато сохранение проекта...");

            string projectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
            string projectName = MainWindow.AddonManager.ProjectName;
            string projectFolder = FileHelper.CurrentProjectRoot ??
                GetProjectFolderSafe(projectsFolder, projectName);
            Directory.CreateDirectory(projectFolder);

            SaveSnapshot snapshot = await mainWindow.Dispatcher.InvokeAsync(() =>
            {
                lock (AddonManager.AddonsLock)
                {
                    MainWindow.AddonManager.Groups.Clear();
                    foreach (var group in GroupManager.Instance.Groups)
                    {
                        MainWindow.AddonManager.Groups.Add(group);
                    }

                    PersistEmbeddedTextures(projectFolder);
                    string json = JsonSerializer.Serialize(MainWindow.AddonManager, SerializerOptions);
                    return new SaveSnapshot(
                        json,
                        PersistentSettingsHelper.Instance.MainProjectsFolder,
                        MainWindow.AddonManager.ProjectName,
                        MainWindow.AddonManager.IsExternalProject,
                        Volatile.Read(ref _changeVersion));
                }
            });

            string? temporaryPath = null;
            try
            {
                string saveFileName = GetSaveFileName(snapshot.IsExternalProject);
                string savePath = Path.Combine(projectFolder, saveFileName);
                temporaryPath = savePath + ".tmp-" + Guid.NewGuid().ToString("N");

                await File.WriteAllTextAsync(temporaryPath, snapshot.Json);
                File.Move(temporaryPath, savePath, overwrite: true);
                temporaryPath = null;

                SaveCreated?.Invoke();
                if (Volatile.Read(ref _changeVersion) == snapshot.ChangeVersion)
                {
                    SetUnsavedChanges(false);
                }

                LogHelper.Log($"Проект сохранён за {timer.ElapsedMilliseconds} мс");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Не удалось сохранить проект: {ex.Message}", Views.LogType.Error);
            }
            finally
            {
                if (temporaryPath != null && File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Критическая ошибка сохранения: {ex.Message}", Views.LogType.Error);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private sealed record SaveSnapshot(
        string Json,
        string ProjectsFolder,
        string ProjectName,
        bool IsExternalProject,
        int ChangeVersion);

    private static void PersistEmbeddedTextures(string projectFolder)
    {
        foreach (var addon in MainWindow.AddonManager.Addons)
        {
            foreach (var drawable in addon.Drawables)
            {
                if (drawable.Details?.EmbeddedTextures == null)
                {
                    continue;
                }

                foreach (var embeddedTexture in drawable.Details.EmbeddedTextures.Values)
                {
                    embeddedTexture?.TryPersistTexture(projectFolder);
                }
            }
        }
    }

    private static string GetProjectFolderSafe(string projectsFolder, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectsFolder) || string.IsNullOrWhiteSpace(projectName))
        {
            throw new InvalidOperationException("Папка проектов или название проекта не настроены.");
        }

        string normalizedRoot = Path.GetFullPath(projectsFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string projectFolder = Path.GetFullPath(Path.Combine(projectsFolder, projectName));

        if (!projectFolder.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(projectFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Путь проекта находится за пределами папки проектов.");
        }

        return projectFolder;
    }

    public static void SetUnsavedChanges(bool status)
    {
        if (status)
        {
            Interlocked.Increment(ref _changeVersion);
            Volatile.Write(ref _hasUnsavedChanges, true);
        }
        else
        {
            Volatile.Write(ref _hasUnsavedChanges, false);
        }

        var mainWindow = MainWindow.Instance;
        if (mainWindow == null)
        {
            return;
        }

        mainWindow.Dispatcher.Invoke(() =>
        {
            const string unsavedMarker = " • не сохранено";
            string baseTitle = mainWindow.Title.Replace(unsavedMarker, string.Empty, StringComparison.Ordinal);
            mainWindow.Title = status ? baseTitle + unsavedMarker : baseTitle;
        });
    }

    public static bool CheckUnsavedChangesMessage()
    {
        if (!HasUnsavedChanges || MainWindow.Instance == null)
        {
            return true;
        }

        bool result = false;
        MainWindow.Instance.Dispatcher.Invoke(() =>
        {
            var clickResult = Show(
                "Есть несохранённые изменения. Продолжить без сохранения?",
                "Несохранённые изменения",
                CustomMessageBoxButtons.OKCancel,
                CustomMessageBoxIcon.Warning);

            result = clickResult == CustomMessageBoxResult.OK;
        });

        return result;
    }


    public static async Task LoadSaveFileAsync(string filePath)
    {
        PauseSaving();
        await _semaphore.WaitAsync();
        try
        {
            FileHelper.SetLoadContext(filePath);
            MainWindow.Instance?.PreviewHost?.ClearProjectSelection();

            var json = await File.ReadAllTextAsync(filePath);
            var addonManager = JsonSerializer.Deserialize<AddonManager>(json, SerializerOptions) ?? throw new InvalidOperationException("Не удалось прочитать файл сохранения.");
            string projectRoot = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty;
            FileHelper.SetCurrentProjectRoot(projectRoot);

            var fileName = Path.GetFileName(filePath);
            var isExternalFromFileName = fileName.Equals(AutoSaveExternalFileName, StringComparison.OrdinalIgnoreCase);
            
            var isExternalProject = addonManager.IsExternalProject || isExternalFromFileName;

            foreach (var addon in addonManager.Addons)
            {
                foreach (var drawable in addon.Drawables)
                {
                    if (!string.IsNullOrEmpty(drawable.FilePath) && drawable.FilePath.Contains("reservedDrawable.ydd"))
                    {
                        drawable.IsReserved = true;
                    }

                    drawable.RestorePersistedEmbeddedTextures(projectRoot);
                }
            }

            MainWindow.AddonManager.Addons.Clear();
            foreach (var addon in addonManager.Addons)
            {
                MainWindow.AddonManager.Addons.Add(addon);
            }
            MainWindow.AddonManager.RebuildMoveMenuItems();

            MainWindow.AddonManager.ProjectName = addonManager.ProjectName;
            MainWindow.AddonManager.IsExternalProject = isExternalProject;

            MainWindow.AddonManager.Groups.Clear();
            if (addonManager.Groups != null)
            {
                foreach (var group in addonManager.Groups)
                {
                    MainWindow.AddonManager.Groups.Add(group);
                }
            }

            MainWindow.AddonManager.Tags.Clear();
            if (addonManager.Tags != null)
            {
                foreach (var tag in addonManager.Tags)
                {
                    MainWindow.AddonManager.Tags.Add(tag);
                }
            }

            int drawableCount = addonManager.Addons.Sum(a => a.Drawables.Count);
            int addonCount = addonManager.Addons.Count;

            PersistentSettingsHelper.Instance.AddRecentProject(
                filePath,
                addonManager.ProjectName ?? Path.GetFileNameWithoutExtension(filePath),
                drawableCount,
                addonCount,
                isExternal: isExternalProject
            );

            LogHelper.Log("Сканирование проекта на дубликаты одежды...");
            DuplicateDetector.Clear();
            
            foreach (var addon in MainWindow.AddonManager.Addons)
            {
                foreach (var drawable in addon.Drawables)
                {
                    DuplicateDetector.RegisterDrawable(drawable);
                }
            }
            
            MainWindow.AddonManager.SelectedAddon = MainWindow.AddonManager.Addons.FirstOrDefault();
            MainWindow.AddonManager.IsPreviewEnabled = false;
            SetUnsavedChanges(false);

            LogHelper.Log($"Сканирование завершено. Найдено групп дубликатов одежды: {DuplicateDetector.GetDuplicateGroupCount()}.");
            LogHelper.Log($"Проект загружен: {filePath}");
        }
        finally
        {
            _semaphore.Release();
            ResumeSaving();
        }
    }
}
