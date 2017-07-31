using System;
using System.Reflection;
using SkiaSharp;

namespace ImageImprov {
    /// <summary>
    /// Backing that holds ImageImprov meta data, as well as the image itself in the SKBitmap.
    /// Setting it up this way also enables caching as we keep the uuid of the image.
    /// This is still a placeholder and not really used yet.
    /// </summary>
    class iiBitmap : SKBitmap {
        long imageId { get; set; }
        SKBitmap SKBitmap { get; set; }

        public static implicit operator iiBitmap(PropertyInfo v) {
            throw new NotImplementedException();
        }
    };
}
