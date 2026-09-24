using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace grzyClothTool.Models.Drawable;
#nullable enable

public class GDrawableDetails : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public enum DetailLevel
    {
        High,
        Med,
        Low
    }

    public enum EmbeddedTextureType
    {
        Specular,
        Normal
    }

    public int TexturesCount = 0;

    public Dictionary<DetailLevel, GDrawableModel?> AllModels { get; set; } = new()
    {
        { DetailLevel.High, null },
        { DetailLevel.Med, null },
        { DetailLevel.Low, null }
    };

    public Dictionary<EmbeddedTextureType, GTextureEmbedded?> EmbeddedTextures { get; set; } = new()
    {
        { EmbeddedTextureType.Specular, null },
        { EmbeddedTextureType.Normal, null }
    };


    private bool _isWarning;
    public bool IsWarning
    {
        get => _isWarning;
        set
        {
            _isWarning = value;
            OnPropertyChanged(nameof(IsWarning));
        }
    }

    private string _tooltip = string.Empty;
    public string Tooltip
    {
        get => _tooltip;
        set
        {
            _tooltip = value;
            OnPropertyChanged(nameof(Tooltip));
        }
    }

    private bool _hasTextureWarnings;
    public bool HasTextureWarnings
    {
        get => _hasTextureWarnings;
        set
        {
            _hasTextureWarnings = value;
            OnPropertyChanged(nameof(HasTextureWarnings));
        }
    }

    private bool _hasEmbeddedTextureWarnings;
    public bool HasEmbeddedTextureWarnings
    {
        get => _hasEmbeddedTextureWarnings;
        set
        {
            _hasEmbeddedTextureWarnings = value;
            OnPropertyChanged(nameof(HasEmbeddedTextureWarnings));
        }
    }

    public void Validate(ObservableCollection<GTexture>? textures = null)
    {
        // reset values
        Tooltip = string.Empty;
        IsWarning = false;
        HasTextureWarnings = false;
        HasEmbeddedTextureWarnings = false;

        foreach (var detailLevel in AllModels.Keys)
        {
            var model = AllModels[detailLevel];
            if (model == null)
            {
                IsWarning = true;
                Tooltip += $"[{GetDetailLevelName(detailLevel)}] Отсутствует модель LOD.\n";
                continue;
            }

            int polygonLimit = detailLevel switch
            {
                DetailLevel.High => SettingsHelper.Instance.PolygonLimitHigh,
                DetailLevel.Med => SettingsHelper.Instance.PolygonLimitMed,
                DetailLevel.Low => SettingsHelper.Instance.PolygonLimitLow,
                _ => throw new InvalidOperationException("Unknown detail level")
            };

            if (model.PolyCount > polygonLimit)
            {
                IsWarning = true;
                Tooltip += $"[{GetDetailLevelName(detailLevel)}] Количество полигонов ({model.PolyCount}) превышает предел ({polygonLimit}).\n";
            }
        }

        foreach (var key in EmbeddedTextures.Keys)
        {
            var txt = EmbeddedTextures[key];
            if (txt == null || txt.TextureData == null)
            {
                IsWarning = true;
                Tooltip += $"Отсутствует текстура: {GetEmbeddedTextureName(key)}.\n";
                continue;
            }
            
            if (txt.Details.IsOptimizeNeeded)
            {
                HasEmbeddedTextureWarnings = true;
            }
        }

        if (TexturesCount == 0)
        {
            IsWarning = true;
            Tooltip += "У элемента одежды нет текстур.\n";
        }
        
        if (textures != null && textures.Count > 0)
        {
            var texturesWithWarnings = textures
                .Where(t => t.TxtDetails != null && t.TxtDetails.IsOptimizeNeeded)
                .ToList();
            
            if (texturesWithWarnings.Count > 0)
            {
                HasTextureWarnings = true;
                IsWarning = true;
            }
        }
        
        var embeddedTexturesWithWarnings = EmbeddedTextures.Values
            .Where(et => et != null && et.TextureData != null && et.Details.IsOptimizeNeeded)
            .Any();
            
        if (embeddedTexturesWithWarnings)
        {
            HasEmbeddedTextureWarnings = true;
        }
        
        if (HasTextureWarnings || HasEmbeddedTextureWarnings)
        {
            Tooltip += "Некоторые текстуры имеют предупреждения. Проверьте сведения о текстурах.\n";
            IsWarning = true;
        }

        // Remove trailing newline character
        Tooltip = Tooltip.TrimEnd('\n');
    }

    private static string GetDetailLevelName(DetailLevel detailLevel) => detailLevel switch
    {
        DetailLevel.High => "высокий",
        DetailLevel.Med => "средний",
        DetailLevel.Low => "низкий",
        _ => detailLevel.ToString()
    };

    private static string GetEmbeddedTextureName(EmbeddedTextureType textureType) => textureType switch
    {
        EmbeddedTextureType.Specular => "отражённая",
        EmbeddedTextureType.Normal => "нормали",
        _ => textureType.ToString()
    };

    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class GDrawableModel
{
    public int PolyCount { get; set; }
}