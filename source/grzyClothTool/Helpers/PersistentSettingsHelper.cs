using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace grzyClothTool.Helpers;

/// <summary>
/// Manages application settings that persist across updates.
/// Settings are stored in %LocalAppData%\KazanClothTool\settings.json
/// </summary>
public class PersistentSettingsHelper
{
    private static readonly Lazy<PersistentSettingsHelper> _instance = new(() => new PersistentSettingsHelper());
    public static PersistentSettingsHelper Instance => _instance.Value;

    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private PersistentSettings _settings;

    private PersistentSettingsHelper()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KazanClothTool"
        );
        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                _settings = JsonSerializer.Deserialize<PersistentSettings>(json) ?? new PersistentSettings();
            }
            else
            {
                _settings = new PersistentSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading persistent settings: {ex.Message}");
            _settings = new PersistentSettings();
        }

        ApplyThemeMigrations();
    }

    /// <summary>
    /// v1.3.0 introduced the Purity UI look and v1.4.0 switches the default interface to the
    /// Bootstrap 5 design system. Settings written by older builds stay valid; only the legacy
    /// default themes are migrated, so a theme picked by the user is never overwritten.
    /// </summary>
    private void ApplyThemeMigrations()
    {
        bool changed = false;

        if (!_settings.PurityLookMigrated)
        {
            _settings.PurityLookMigrated = true;
            changed = true;

            if (string.IsNullOrWhiteSpace(_settings.Theme)
                || string.Equals(_settings.Theme, AppThemes.Dark, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Theme = AppThemes.Purity;
            }
        }

        if (!_settings.BootstrapLookMigrated)
        {
            _settings.BootstrapLookMigrated = true;
            changed = true;

            if (string.IsNullOrWhiteSpace(_settings.Theme)
                || string.Equals(_settings.Theme, AppThemes.Dark, StringComparison.OrdinalIgnoreCase)
                || string.Equals(_settings.Theme, AppThemes.Purity, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Theme = AppThemes.Bootstrap;
            }
        }

        if (changed)
        {
            SaveSettings();
        }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);

            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(_settings, options);
            string temporaryPath = _settingsFilePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving persistent settings: {ex.Message}");
        }
    }

    public bool IsFirstRun
    {
        get => _settings.IsFirstRun;
        set
        {
            if (_settings.IsFirstRun != value)
            {
                _settings.IsFirstRun = value;
                SaveSettings();
            }
        }
    }

    public string MainProjectsFolder
    {
        get => _settings.MainProjectsFolder ?? string.Empty;
        set
        {
            if (_settings.MainProjectsFolder != value)
            {
                _settings.MainProjectsFolder = value;
                SaveSettings();
            }
        }
    }

    public string Theme
    {
        get => AppThemes.Normalize(_settings.Theme);
        set
        {
            string normalized = AppThemes.Normalize(value);
            if (!string.Equals(_settings.Theme, normalized, StringComparison.Ordinal))
            {
                _settings.Theme = normalized;
                SaveSettings();
            }
        }
    }

    public string Language
    {
        get => LocalizationHelper.Normalize(_settings.Language);
        set
        {
            string normalized = LocalizationHelper.Normalize(value);
            if (!string.Equals(_settings.Language, normalized, StringComparison.OrdinalIgnoreCase))
            {
                _settings.Language = normalized;
                SaveSettings();
            }
        }
    }

    public string SettingsFilePath => _settingsFilePath;

    public List<RecentProject> RecentlyOpenedProjects
    {
        get => _settings.RecentlyOpenedProjects ?? new List<RecentProject>();
        set
        {
            _settings.RecentlyOpenedProjects = value;
            SaveSettings();
        }
    }


    public void AddRecentProject(string filePath, string projectName, int drawableCount, int addonCount, bool isExternal = false)
    {
        var recentProjects = RecentlyOpenedProjects;
        
        recentProjects.RemoveAll(p => string.Equals(p.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        
        recentProjects.Insert(0, new RecentProject
        {
            FilePath = filePath,
            ProjectName = projectName,
            LastModified = DateTime.Now,
            DrawableCount = drawableCount,
            AddonCount = addonCount,
            IsExternal = isExternal
        });
        
        if (recentProjects.Count > 6)
        {
            recentProjects = [.. recentProjects.Take(6)];
        }
        
        RecentlyOpenedProjects = recentProjects;
    }

    public static bool IsRootDrive(string path)
    {
        try
        {
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string root = Path.GetPathRoot(normalizedPath);

            return string.Equals(normalizedPath, root?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}


public class PersistentSettings
{
    public bool IsFirstRun { get; set; } = true;
    public string Theme { get; set; } = AppThemes.Bootstrap;
    public bool PurityLookMigrated { get; set; }
    public bool BootstrapLookMigrated { get; set; }
    public string MainProjectsFolder { get; set; } = string.Empty;
    public string Language { get; set; } = LocalizationHelper.Russian;
    public List<RecentProject> RecentlyOpenedProjects { get; set; } = [];
}

public class RecentProject : INotifyPropertyChanged
{
    public RecentProject()
    {
        LocalizationHelper.LanguageChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastModifiedText)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath { get; set; }
    public string ProjectName { get; set; }
    public DateTime LastModified { get; set; }
    public string LastModifiedText => LocalizationHelper.Format("изменён {0:dd.MM.yyyy HH:mm}", LastModified);
    public int DrawableCount { get; set; }
    public int AddonCount { get; set; }
    public bool IsExternal { get; set; }
}
