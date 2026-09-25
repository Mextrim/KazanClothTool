using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using ImageMagick;
using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Text.Json.Serialization;

namespace grzyClothTool.Models.Texture;

#nullable enable

public class GTexture : INotifyPropertyChanged
{
    private readonly static SemaphoreSlim _semaphore = new(3);

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; set; }
    public string FilePath { get; set; }
    public string Extension { get; set; }

    [JsonIgnore]
    public string FullFilePath => FileHelper.ResolveFilePath(FilePath);

    private string _displayName = string.Empty;
    public string DisplayName
    {
        get { return _displayName; }
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    private int _number;
    public int Number
    {
        get => _number;
        set
        {
            _number = value;
            OnPropertyChanged(nameof(Number));
            UpdateDisplayName();
        }
    }

    private int _txtNumber;
    public int TxtNumber
    {
        get => _txtNumber;
        set
        {
            _txtNumber = value;
            
            OnPropertyChanged();
            OnPropertyChanged("BuildName");
            OnPropertyChanged("TxtLetter");
            UpdateDisplayName();
        }
    }

    public char TxtLetter
    {
        get => (char)('a' + TxtNumber);
    }

    public int TypeNumeric { get; set; }
    public string TypeName => EnumHelper.GetName(TypeNumeric, IsProp);

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

    public GTextureDetails? TxtDetails { get; set; }

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
    public bool IsProp { get; set; }
    public bool HasSkin { get; set; }

    private bool _isOptimizedDuringBuild;
    public bool IsOptimizedDuringBuild
    {
        get => _isOptimizedDuringBuild;
        set
        {
            if (_isOptimizedDuringBuild != value)
            {
                _isOptimizedDuringBuild = value;
                OnPropertyChanged(nameof(IsOptimizedDuringBuild));
            }
        }
    }
    private GTextureDetails? _optimizeDetails;
    public GTextureDetails? OptimizeDetails
    {
        get => _optimizeDetails;
        set
        {
            if (_optimizeDetails != value)
            {
                _optimizeDetails = value;
                OnPropertyChanged(nameof(OptimizeDetails));
            }
        }
    }

    private bool _isPreviewDisabled;
    public bool IsPreviewDisabled
    {
        get => _isPreviewDisabled;
        set
        {
            if (_isPreviewDisabled == value)
            {
                return;
            }

            _isPreviewDisabled = value;
            OnPropertyChanged(nameof(IsPreviewDisabled));
        }
    }

    public GTexture(Guid id, string filePath, int typeNumeric, int number, int txtNumber, bool hasSkin, bool isProp)
    {
        IsLoading = true;

        Id = id;
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }

        FilePath = filePath;
        Extension = Path.GetExtension(filePath).ToLowerInvariant();
        Number = number;
        TxtNumber = txtNumber;
        TypeNumeric = typeNumeric;
        IsProp = isProp;
        HasSkin = hasSkin;
        DisplayName = GetBuildName();

        if (filePath != null)
        {
            try
            {
                var fullPath = FileHelper.ResolveFilePath(filePath);
                Task<GTextureDetails?> _textureDetailsTask = LoadTextureDetailsWithConcurrencyControl(fullPath).ContinueWith(t =>
                {
                    void ApplyResult()
                    {
                        if (t.IsFaulted)
                        {
                            LogHelper.Log($"Не удалось загрузить сведения о текстуре «{DisplayName}»: {t.Exception?.InnerException?.Message ?? t.Exception?.Message}", Views.LogType.Warning);
                            IsPreviewDisabled = true;
                            IsLoading = false;
                            return;
                        }

                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            if (t.Result == null)
                            {
                                IsPreviewDisabled = true;
                            }
                            else
                            {
                                TxtDetails = t.Result;
                                OnPropertyChanged(nameof(TxtDetails));
                                TxtDetails.Validate();
                            }
                        }

                        IsLoading = false;
                    }

                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                    {
                        ApplyResult();
                    }
                    else if (dispatcher.CheckAccess())
                    {
                        ApplyResult();
                    }
                    else
                    {
                        dispatcher.BeginInvoke(new Action(ApplyResult));
                    }

                    return t.Status == TaskStatus.RanToCompletion ? t.Result : null;
                });
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Не удалось загрузить текстуру «{DisplayName}»: {ex.Message}", Views.LogType.Warning);
                IsLoading = false;
                IsPreviewDisabled = true;
            }
        }
    }

    public async void LoadThumbnailAsync()
    {
        if (ImageThumbnail != null)
            return;

        string fullPath;
        try
        {
            fullPath = FullFilePath;
            if (FilePath == null || !File.Exists(fullPath))
                return;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Не удалось найти текстуру «{DisplayName}»: {ex.Message}", Views.LogType.Warning);
            return;
        }

        await Task.Delay(Random.Shared.Next(25, 100));
        await Task.Run(() =>
        {
            try
            {
                using MagickImage img = ImgHelper.GetImage(fullPath);
                if (img == null)
                    return;

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

                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                {
                    return;
                }

                void SetThumbnail()
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
                }

                if (dispatcher.CheckAccess())
                {
                    SetThumbnail();
                }
                else
                {
                    dispatcher.Invoke(SetThumbnail);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image thumbnail generation failed: {ex.Message}");
                LogHelper.Log($"Не удалось создать миниатюру текстуры «{DisplayName}»");
            }
        });
    }



    public string GetBuildName()
    {
        string name = $"{TypeName}_diff_{Number:D3}_{TxtLetter}";
        return IsProp ? name : $"{name}_{(HasSkin ? "whi" : "uni")}";
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public async Task LoadDetails()
    {
        string fullPath = FullFilePath;
        if (File.Exists(fullPath))
        {
            try
            {
                var result = await LoadTextureDetailsWithConcurrencyControl(fullPath);
                if (result != null)
                {
                    TxtDetails = result;
                    OnPropertyChanged(nameof(TxtDetails));
                    result.Validate();
                    IsPreviewDisabled = false;
                }
                else
                {
                    IsPreviewDisabled = true;
                }
            }
            catch (Exception ex)
            {
                IsPreviewDisabled = true;
                LogHelper.Log($"Не удалось загрузить текстуру {DisplayName}: {ex.Message}", Views.LogType.Warning);
            }
        }
        else
        {
            IsPreviewDisabled = true;
        }

        IsLoading = false;
    }

    private void UpdateDisplayName()
    {
        DisplayName = GetBuildName();
    }

    private static async Task<GTextureDetails?> LoadTextureDetailsWithConcurrencyControl(string path)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await GetTextureDetailsAsync(path);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    private static async Task<GTextureDetails?> GetTextureDetailsAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".ytd")
        {
            var ytdFile = new YtdFile();
            await ytdFile.LoadAsync(bytes);

            if (ytdFile.TextureDict.Textures.Count == 0)
            {
                return null;
            }

            var txt = ytdFile.TextureDict.Textures[0];

            return new GTextureDetails
            {
                MipMapCount = txt.Levels,
                Compression = txt.Format.ToString(),
                Width = txt.Width,
                Height = txt.Height,
                Name = txt.Name,
                Type = "diffuse"
            };
        }
        else if (extension is ".jpg" or ".jpeg" or ".png" or ".dds")
        {
            using var img = new MagickImage(bytes);

            return new GTextureDetails
            {
                Width = (int)img.Width,
                Height = (int)img.Height,
                MipMapCount = ImgHelper.GetCorrectMipMapAmount((int)img.Width, (int)img.Height),
                Compression = "UNKNOWN",
                Name = img.FileName,
                Type = "diffuse"
            };
        }

        return null;
    }
}

