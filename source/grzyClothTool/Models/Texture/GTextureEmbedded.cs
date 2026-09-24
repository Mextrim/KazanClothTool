using CodeWalker.GameFiles;
using CodeWalker.Utils;
using grzyClothTool.Helpers;
using ImageMagick;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace grzyClothTool.Models.Texture;

#nullable enable

public class GTextureEmbedded : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string OriginalName;

    public GTextureDetails Details { get; set; } = new GTextureDetails();
    public GTextureDetails? OptimizeDetails { get; set; }

    [JsonIgnore]
    public CodeWalker.GameFiles.Texture? TextureData { get; set; }

    private CodeWalker.GameFiles.Texture? _persistedTextureData;
    private string? _persistedTexturePath;

    /// <summary>
    /// Относительный путь к сохранённой DDS-копии встроенной текстуры.
    /// Сам объект Texture из CodeWalker не сериализуется System.Text.Json.
    /// </summary>
    public string? PersistedTexturePath
    {
        get => _persistedTexturePath;
        set
        {
            if (_persistedTexturePath != value)
            {
                _persistedTexturePath = value;
                OnPropertyChanged();
            }
        }
    }

    private CodeWalker.GameFiles.Texture? _replacementTextureData;
    [JsonIgnore]
    public CodeWalker.GameFiles.Texture? ReplacementTextureData
    {
        get => _replacementTextureData;
        set
        {
            _replacementTextureData = value;
            
            if (value != null)
            {
                IsOptimizedDuringBuild = false;
                OptimizeDetails = null;
            }
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasReplacement));
            OnPropertyChanged(nameof(DisplayTextureData));
            OnPropertyChanged(nameof(IsOptimizedDuringBuild));
        }
    }

    [JsonIgnore]
    public bool HasReplacement => _replacementTextureData != null;

    [JsonIgnore]
    public CodeWalker.GameFiles.Texture? DisplayTextureData => _replacementTextureData ?? _persistedTextureData ?? TextureData;

    private bool _isOptimizedDuringBuild;
    public bool IsOptimizedDuringBuild
    {
        get => _isOptimizedDuringBuild;
        set
        {
            _isOptimizedDuringBuild = value;
            OnPropertyChanged();
        }
    }

    private BitmapSource? _imageThumbnail;
    [JsonIgnore]
    public BitmapSource? ImageThumbnail
    {
        get => _imageThumbnail;
        set
        {
            if (_imageThumbnail != value)
            {
                _imageThumbnail = value;
                OnPropertyChanged(nameof(ImageThumbnail));
            }
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsPreviewDisabled => DisplayTextureData?.Data?.FullData == null || DisplayTextureData.Data.FullData.Length == 0;

    [JsonIgnore]
    public string PreviewDisabledTooltip => IsPreviewDisabled ? "Зашифрованный элемент одежды" : string.Empty;

    // Parameterless constructor for JSON deserialization
    public GTextureEmbedded()
    {
        OriginalName = string.Empty;
        Details = new GTextureDetails();
    }

    public GTextureEmbedded(CodeWalker.GameFiles.Texture? textureData, string type)
    {
        TextureData = textureData;

        if (textureData == null)
        {
            OriginalName = "Отсутствующая текстура";
            Details.Name = "Отсутствующая текстура";
            Details.Type = type;
            Details.Width = 0;
            Details.Height = 0;
            Details.MipMapCount = 0;
            Details.Compression = "N/A";
        }
        else
        {
            OriginalName = textureData.Name;

            Details.Name = textureData.Name;
            Details.Type = type;
            Details.Width = textureData.Width;
            Details.Height = textureData.Height;
            Details.MipMapCount = textureData.Levels;
            Details.Compression = textureData.Format.ToString();
            
            Details.Validate();
            LoadThumbnailAsync();
        }
    }

    public bool TryPersistTexture(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            return false;
        }

        CodeWalker.GameFiles.Texture? texture = DisplayTextureData;
        if (texture?.Data?.FullData == null || texture.Data.FullData.Length == 0)
        {
            DeletePersistedFile(projectRoot);
            return false;
        }

        string? oldPath = PersistedTexturePath;
        try
        {
            byte[] dds = DDSIO.GetDDSFile(texture);
            string safeName = MakeSafeFileName(OriginalName);
            string hash = Convert.ToHexString(SHA256.HashData(dds))[..16];
            string relativePath = Path.Combine("embedded", $"{safeName}_{hash}.dds")
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');
            string root = GetSafeRoot(projectRoot);
            string destination = GetSafePath(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            string temporaryPath = destination + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllBytes(temporaryPath, dds);
                File.Move(temporaryPath, destination, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            PersistedTexturePath = relativePath;
            if (!string.IsNullOrWhiteSpace(oldPath) && !string.Equals(oldPath, relativePath, StringComparison.OrdinalIgnoreCase))
            {
                DeletePersistedPath(root, oldPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Не удалось сохранить встроенную текстуру «{OriginalName}»: {ex.Message}", Views.LogType.Warning);
            return false;
        }
    }

    public bool TryRestorePersistedTexture(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(PersistedTexturePath) || string.IsNullOrWhiteSpace(projectRoot))
        {
            return false;
        }

        try
        {
            string root = GetSafeRoot(projectRoot);
            string path = GetSafePath(root, PersistedTexturePath);
            if (!File.Exists(path))
            {
                return false;
            }

            CodeWalker.GameFiles.Texture? restored = DDSIO.GetTexture(File.ReadAllBytes(path));
            if (restored == null)
            {
                return false;
            }

            _persistedTextureData = restored;
            if (string.IsNullOrWhiteSpace(OriginalName))
            {
                OriginalName = restored.Name ?? string.Empty;
            }

            UpdateDetailsFromTexture(restored);
            OnPropertyChanged(nameof(DisplayTextureData));
            OnPropertyChanged(nameof(IsPreviewDisabled));
            LoadThumbnailAsync();
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Не удалось восстановить встроенную текстуру «{OriginalName}»: {ex.Message}", Views.LogType.Warning);
            return false;
        }
    }

    private void DeletePersistedFile(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(PersistedTexturePath))
        {
            return;
        }

        try
        {
            DeletePersistedPath(GetSafeRoot(projectRoot), PersistedTexturePath);
            PersistedTexturePath = null;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Не удалось удалить сохранённую текстуру: {ex.Message}", Views.LogType.Warning);
        }
    }

    private static void DeletePersistedPath(string root, string relativePath)
    {
        string path = GetSafePath(root, relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GetSafeRoot(string projectRoot)
    {
        return Path.GetFullPath(projectRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    private static string GetSafePath(string root, string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(root, relativePath));
        string normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Путь встроенной текстуры находится за пределами проекта.");
        }

        return path;
    }

    private static string MakeSafeFileName(string? name)
    {
        string value = string.IsNullOrWhiteSpace(name) ? "texture" : name;
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }

    private void UpdateDetailsFromTexture(CodeWalker.GameFiles.Texture texture)
    {
        Details.Name = texture.Name ?? Details.Name;
        Details.Width = texture.Width;
        Details.Height = texture.Height;
        Details.MipMapCount = texture.Levels;
        Details.Compression = texture.Format.ToString();
        Details.Validate();
        OnPropertyChanged(nameof(Details));
    }

    public void SetReplacementTexture(CodeWalker.GameFiles.Texture newTexture)
    {
        _persistedTextureData = null;
        ReplacementTextureData = newTexture;
        
        Details.Name = newTexture.Name;
        Details.Width = newTexture.Width;
        Details.Height = newTexture.Height;
        Details.MipMapCount = newTexture.Levels;
        Details.Compression = newTexture.Format.ToString();
        Details.Validate();
        
        OnPropertyChanged(nameof(Details));
        
        ImageThumbnail = null;
        LoadThumbnailAsync();
    }

    public void RenameTexture(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || DisplayTextureData == null)
            return;

        Details.Name = newName;
        OnPropertyChanged(nameof(Details));
    }

    public async void LoadThumbnailAsync()
    {
        if (ImageThumbnail != null || DisplayTextureData?.Data?.FullData == null)
            return;

        IsLoading = true;

        await Task.Delay(Random.Shared.Next(25, 100));
        await Task.Run(() =>
        {
            try
            {
                if (DisplayTextureData.Data.FullData.Length == 0)
                    return;

                var dds = DDSIO.GetDDSFile(DisplayTextureData);
                using var img = new MagickImage(dds);
                
                img.Resize(90, 90);
                int w = (int)img.Width;
                int h = (int)img.Height;
                byte[] pixels = img.ToByteArray(MagickFormat.Bgra);

                using Bitmap bitmap = new(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, w, h),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var source = BitmapSource.Create(
                        bitmap.Width,
                        bitmap.Height,
                        96, 96,
                        PixelFormats.Bgra32,
                        null,
                        pixels,
                        bitmap.Width * 4
                    );
                    source.Freeze();
                    ImageThumbnail = source;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Embedded texture thumbnail generation failed: {ex.Message}");
                LogHelper.Log($"Не удалось создать миниатюру встроенной текстуры для {Details.Name}");
            }
            finally
            {
                IsLoading = false;
            }
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}