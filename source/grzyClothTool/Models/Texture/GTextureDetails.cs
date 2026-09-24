using System;
using grzyClothTool.Helpers;

namespace grzyClothTool.Models.Texture;
#nullable enable

public class GTextureDetails
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MipMapCount { get; set; }
    public string Compression { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public string? Type { get; set; } = string.Empty;

    public bool IsOptimizeNeeded { get; set; }
    public string IsOptimizeNeededTooltip { get; set; } = string.Empty;

    public void Validate()
    {
        IsOptimizeNeeded = false;
        IsOptimizeNeededTooltip = string.Empty;
        
        int resolutionLimit = 2048;
        
        if (Type != null)
        {
            if (Type.Contains("diffuse", StringComparison.OrdinalIgnoreCase))
            {
                resolutionLimit = SettingsHelper.Instance.TextureResolutionLimitDiffuse;
            }
            else if (Type.Contains("normal", StringComparison.OrdinalIgnoreCase))
            {
                resolutionLimit = SettingsHelper.Instance.TextureResolutionLimitNormal;
            }
            else if (Type.Contains("specular", StringComparison.OrdinalIgnoreCase))
            {
                resolutionLimit = SettingsHelper.Instance.TextureResolutionLimitSpecular;
            }
        }
        
        if (Width > resolutionLimit || Height > resolutionLimit)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += $"Разрешение текстуры: {Width}x{Height}. Оно превышает установленный предел ({resolutionLimit}). Оптимизируйте текстуру, чтобы уменьшить размер.\n";
        }

        if (Width <= 0 || Height <= 0)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += "Размер текстуры недействителен.\n";
        }
        else if ((Height & Height - 1) != 0 || (Width & Width - 1) != 0)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += "Ширина или высота текстуры не является степенью двойки.\n";
        }

        int expectedMipMapCount = ImgHelper.GetCorrectMipMapAmount(Width, Height);
        if (MipMapCount > 0 && MipMapCount != expectedMipMapCount)
        {
            IsOptimizeNeeded = true;
            IsOptimizeNeededTooltip += $"Текстура содержит {MipMapCount} mip-уровней, а требуется {expectedMipMapCount}.\n";
        }
    }
}
