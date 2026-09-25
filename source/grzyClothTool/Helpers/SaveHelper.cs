using grzyClothTool.Models;
using grzyClothTool.Models.Drawable;
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
                
            string projectPath;
            try
            {
                projectPath = GetProjectFolderSafe(mainProjectsFolder, projectName);
            }
            catch
            {
                return false;
            }
            
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
    private static Timer? _timer;
    public static event Action? SaveCreated;

    public static event Action<double>? AutoSaveProgress;
    public static event Action<int>? RemainingSecondsChanged;
    private static int _autoSaveInterval = 60000; // 60 seconds
    private static int _elapsedTime = 0;

    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static int _changeVersion;
    private static int _shutdownRequested;
    private static int _autoSaveTickRunning;

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
        Interlocked.Exchange(ref _shutdownRequested, 0);
        if (_timer != null)
        {
            return;
        }

        _timer = new Timer(1000)
        {
            AutoReset = true
        };
        _timer.Elapsed += OnAutoSaveTick;
        _timer.Start();
    }

    public static void Shutdown()
    {
        Interlocked.Exchange(ref _shutdownRequested, 1);

        Timer? timer = Interlocked.Exchange(ref _timer, null);
        timer?.Stop();
        timer?.Dispose();
        _elapsedTime = 0;
    }

    private static void OnAutoSaveTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _ = HandleAutoSaveTickAsync();
    }

    private static async Task HandleAutoSaveTickAsync()
    {
        if (Interlocked.Exchange(ref _autoSaveTickRunning, 1) != 0)
        {
            return;
        }

        try
        {
            Timer? timer = _timer;
            if (timer == null || Volatile.Read(ref _shutdownRequested) != 0)
            {
                return;
            }

            if (SavingPaused || !HasUnsavedChanges)
            {
                _elapsedTime = 0;
                AutoSaveProgress?.Invoke(0);
                RemainingSecondsChanged?.Invoke(0);
                return;
            }

            _elapsedTime += (int)timer.Interval;
            double percentage = ((double)_elapsedTime / _autoSaveInterval) * 75.0;
            int remainingSeconds = Math.Max(0, (_autoSaveInterval - _elapsedTime) / 1000);

            if (_elapsedTime >= _autoSaveInterval)
            {
                _elapsedTime = 0;
                await SaveAsync();
                if (Volatile.Read(ref _shutdownRequested) == 0)
                {
                    RemainingSecondsChanged?.Invoke(0);
                }
                return;
            }

            AutoSaveProgress?.Invoke(percentage);
            RemainingSecondsChanged?.Invoke(remainingSeconds);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Ошибка автосохранения: {ex.Message}", Views.LogType.Error);
        }
        finally
        {
            Interlocked.Exchange(ref _autoSaveTickRunning, 0);
        }
    }

    /// <summary>
    /// Saves the current project. Explicit user/project-operation saves use
    /// <paramref name="force"/> so progress scopes cannot silently turn Ctrl+S
    /// or a required save into a no-op.
    /// </summary>
    public static async Task<bool> SaveAsync(bool force = false)
    {
        var mainWindow = MainWindow.Instance;
        AddonManager? manager = MainWindow.AddonManager;
        if (!HasUnsavedChanges || mainWindow == null || manager == null ||
            Volatile.Read(ref _shutdownRequested) != 0 ||
            (!force && SavingPaused))
        {
            return false;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (!HasUnsavedChanges || Volatile.Read(ref _shutdownRequested) != 0 || (!force && SavingPaused))
            {
                return false;
            }

            var timer = Stopwatch.StartNew();
            LogHelper.Log("Начало сохранения проекта...");

            string projectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
            string projectName = manager.ProjectName;
            string projectFolder = FileHelper.CurrentProjectRoot ??
                GetProjectFolderSafe(projectsFolder, projectName);
            Directory.CreateDirectory(projectFolder);

            SaveSnapshot snapshot = await mainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (!ReferenceEquals(MainWindow.AddonManager, manager))
                {
                    throw new InvalidOperationException("Проект изменился во время сохранения. Повторите сохранение.");
                }

                lock (AddonManager.AddonsLock)
                {
                    // GroupManager.Instance.Groups is a view of this same
                    // collection. Clearing it before enumerating the view used
                    // to erase every group from the save. Serialize the current
                    // collection directly instead.
                    PersistEmbeddedTextures(manager, projectFolder);
                    string json = JsonSerializer.Serialize(manager, SerializerOptions);
                    return new SaveSnapshot(
                        json,
                        PersistentSettingsHelper.Instance.MainProjectsFolder,
                        manager.ProjectName,
                        manager.IsExternalProject,
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
                if (!ReferenceEquals(MainWindow.AddonManager, manager))
                {
                    throw new InvalidOperationException("Проект изменился во время сохранения. Повторите сохранение.");
                }
                File.Move(temporaryPath, savePath, overwrite: true);
                temporaryPath = null;

                SaveCreated?.Invoke();
                if (ReferenceEquals(MainWindow.AddonManager, manager) &&
                    Volatile.Read(ref _changeVersion) == snapshot.ChangeVersion)
                {
                    SetUnsavedChanges(false);
                }

                LogHelper.Log($"Проект сохранён за {timer.ElapsedMilliseconds} мс");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Не удалось сохранить проект: {ex.Message}", Views.LogType.Error);
                return false;
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
            return false;
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

    private static void PersistEmbeddedTextures(AddonManager manager, string projectFolder)
    {
        foreach (var addon in manager.Addons)
        {
            foreach (var drawable in addon.Drawables)
            {
                if (drawable.Details?.EmbeddedTextures == null)
                {
                    continue;
                }

                foreach (var embeddedTexture in drawable.Details.EmbeddedTextures.Values)
                {
                    if (embeddedTexture != null && !embeddedTexture.TryPersistTexture(projectFolder))
                    {
                        throw new IOException($"Не удалось сохранить встроенную текстуру «{embeddedTexture.OriginalName}».");
                    }
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

        if (Path.IsPathRooted(projectName) || projectName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("Путь проекта содержит недопустимые сегменты.");
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
        if (mainWindow == null || mainWindow.Dispatcher.HasShutdownStarted || mainWindow.Dispatcher.HasShutdownFinished)
        {
            return;
        }

        void UpdateTitle()
        {
            const string unsavedMarker = " • не сохранено";
            string baseTitle = mainWindow.Title.Replace(unsavedMarker, string.Empty, StringComparison.Ordinal);
            mainWindow.Title = status ? baseTitle + unsavedMarker : baseTitle;
        }

        if (mainWindow.Dispatcher.CheckAccess())
        {
            UpdateTitle();
        }
        else
        {
            mainWindow.Dispatcher.Invoke(UpdateTitle);
        }
    }

    public static bool CheckUnsavedChangesMessage()
    {
        if (!HasUnsavedChanges || MainWindow.Instance == null ||
            MainWindow.Instance.Dispatcher.HasShutdownStarted || MainWindow.Instance.Dispatcher.HasShutdownFinished)
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
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь файла проекта не указан.", nameof(filePath));
        if (!MainWindow.TryBeginProjectOperation())
            throw new InvalidOperationException("Другая операция с проектом уже выполняется.");

        PauseSaving();
        await _semaphore.WaitAsync();
        string? previousRoot = FileHelper.CurrentProjectRoot;
        MainWindow? mainWindow = MainWindow.Instance;
        AddonManager? previousManager = mainWindow == null ? null : MainWindow.AddonManager;
        bool committed = false;

        try
        {
            string fullPath = Path.GetFullPath(filePath);
            FileHelper.SetLoadContext(fullPath);
            MainWindow.Instance?.PreviewHost?.ClearProjectSelection();

            string json = await File.ReadAllTextAsync(fullPath);
            AddonManager loadedManager = JsonSerializer.Deserialize<AddonManager>(json, SerializerOptions)
                ?? throw new InvalidOperationException("Не удалось прочитать файл сохранения.");

            loadedManager.Addons ??= [];
            loadedManager.Groups ??= [];
            loadedManager.Tags ??= [];

            string projectRoot = Path.GetDirectoryName(fullPath)
                ?? throw new InvalidOperationException("Не удалось определить папку проекта.");
            FileHelper.SetCurrentProjectRoot(projectRoot);

            string fileName = Path.GetFileName(fullPath);
            bool isExternalFromFileName = fileName.Equals(AutoSaveExternalFileName, StringComparison.OrdinalIgnoreCase);
            loadedManager.IsExternalProject = loadedManager.IsExternalProject || isExternalFromFileName;

            foreach (var addon in loadedManager.Addons)
            {
                if (addon == null)
                    continue;

                addon.Drawables ??= [];
                addon.SelectedDrawables ??= [];
                foreach (var drawable in addon.Drawables)
                {
                    if (drawable == null)
                        continue;

                    drawable.Textures ??= [];
                    drawable.Tags ??= [];
                    drawable.SelectedFlags ??= [];
                    drawable.Details ??= new GDrawableDetails();
                    if (!string.IsNullOrEmpty(drawable.FilePath) && drawable.FilePath.Contains("reservedDrawable.ydd", StringComparison.OrdinalIgnoreCase))
                    {
                        drawable.IsReserved = true;
                    }

                    drawable.RestorePersistedEmbeddedTextures(projectRoot);
                }

                addon.SelectedDrawable ??= addon.Drawables.FirstOrDefault();
                addon.SelectedTexture ??= addon.SelectedDrawable?.Textures?.FirstOrDefault();
            }

            loadedManager.ProjectName = string.IsNullOrWhiteSpace(loadedManager.ProjectName)
                ? Path.GetFileName(projectRoot)
                : loadedManager.ProjectName;
            loadedManager.SelectedAddon = loadedManager.Addons.FirstOrDefault();
            loadedManager.IsPreviewEnabled = false;

            LogHelper.Log("Сканирование проекта на дубликаты одежды...");
            DuplicateDetector.Clear();
            foreach (var addon in loadedManager.Addons)
            {
                foreach (var drawable in addon.Drawables)
                {
                    if (drawable != null)
                    {
                        DuplicateDetector.RegisterDrawable(drawable);
                    }
                }
            }

            int drawableCount = loadedManager.Addons.Sum(a => a.Drawables.Count);
            int addonCount = loadedManager.Addons.Count;
            PersistentSettingsHelper.Instance.AddRecentProject(
                fullPath,
                loadedManager.ProjectName,
                drawableCount,
                addonCount,
                isExternal: loadedManager.IsExternalProject);

            FileHelper.ClearLoadContext();
            if (MainWindow.Instance == null || previousManager == null)
                throw new InvalidOperationException("Главное окно недоступно.");

            MainWindow.Instance.CommitProjectLoad(previousManager, loadedManager, projectRoot);
            committed = true;
            SetUnsavedChanges(false);

            LogHelper.Log($"Сканирование завершено. Найдено групп дубликатов одежды: {DuplicateDetector.GetDuplicateGroupCount()}.");
            LogHelper.Log($"Проект загружен: {fullPath}");
        }
        catch
        {
            if (!committed)
            {
                FileHelper.SetCurrentProjectRoot(previousRoot);
                DuplicateDetector.Clear();
                if (previousManager != null)
                {
                    foreach (var addon in previousManager.Addons)
                    {
                        foreach (var drawable in addon.Drawables)
                        {
                            if (drawable != null)
                            {
                                DuplicateDetector.RegisterDrawable(drawable);
                            }
                        }
                    }
                }
            }
            throw;
        }
        finally
        {
            _semaphore.Release();
            ResumeSaving();
            MainWindow.EndProjectOperation();
        }
    }
}
