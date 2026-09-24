using System;
using System.Collections.Generic;
using CodeWalker.GameFiles;

namespace CodeWalker
{
    public partial class CustomPedsForm
    {
        public event Action BatchExportRequested;
        public Dictionary<string, Drawable> BatchLoadedDrawables = new Dictionary<string, Drawable>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<Drawable, List<TextureDictionary>> BatchLoadedTextureVariants = new Dictionary<Drawable, List<TextureDictionary>>();

        public void RaiseLegacyBatchExportRequested()
        {
            BatchExportRequested?.Invoke();
        }
    }
}
