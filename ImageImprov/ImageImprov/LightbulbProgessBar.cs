using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace ImageImprov {
    class LightbulbProgessBar : iiBitmapView {
        public double pct = 0.0;

        SKBitmap offBulb;
        SKBitmap onBulb;

        public LightbulbProgessBar() : base() {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            offBulb = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.reward_inactive.png", assembly);
            onBulb = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.reward.png", assembly);
            Bitmap = new SKBitmap(offBulb.Width, offBulb.Height);
        }

        public SKBitmap combineLightbulbs(SKBitmap outputBmp, SKBitmap lowerImage, SKBitmap upperImage, double pctScale) {
            using (var canvas = new SKCanvas(outputBmp)) {
                //canvas.Clear(new SKColor(242,242,242)); // this works.
                canvas.Clear(GlobalSingletonHelpers.SKColorFromXamarinColor(GlobalStatusSingleton.backgroundColor));
                int y = (int)(pctScale * (double)lowerImage.Height);
                SKRect lwrRegion = SKRect.Create(0, 0, lowerImage.Width, y);
                canvas.DrawBitmap(lowerImage, lwrRegion, lwrRegion);
                SKRect uprRegion = SKRect.Create(0, y + 1, upperImage.Width, upperImage.Height - y);
                canvas.DrawBitmap(upperImage, uprRegion, uprRegion);
            }
            return outputBmp;
        }

        public override void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            if (pct > 1.0) pct = 0.0;
            // probably a better way to do this. get to that next.
            double drawPct = 1.0 - pct;
            Bitmap = combineLightbulbs(Bitmap, offBulb, onBulb, drawPct);

            SKSurface vSurface = e.Surface;
            var surfaceWidth = e.Info.Width;
            var surfaceHeight = e.Info.Height;
            SKCanvas vCanvas = vSurface.Canvas;
            //Clear the Canvas
            vCanvas.Clear();

            if (Scaling == true) {
                int drawWidth = surfaceWidth;
                int drawHeight = surfaceHeight;
                int startX = 0;
                int startY = 0;
                if (EnsureSquare == true) {
                    // This maintains  an aspect ratio of 1.
                    if (drawWidth < drawHeight) {
                        drawHeight = drawWidth;
                    } else {
                        drawWidth = drawHeight;
                    }
                    if (drawWidth < surfaceWidth) {
                        startX = (int)((surfaceWidth - drawWidth) / 2);
                    }
                    if (drawHeight < surfaceHeight) {
                        startY = (int)((surfaceHeight - drawHeight) / 2);
                    }
                }
                SKBitmap bitmap = new SKBitmap(new SKImageInfo(drawWidth, drawHeight));
                if ((bitmap != null) && (Bitmap != null)) {
                    Bitmap.Resize(bitmap, SKBitmapResizeMethod.Box);
                    SKRect drawArea = new SKRect(startX, startY, startX + drawWidth, startY + drawHeight);
                    vCanvas.DrawBitmap(bitmap, drawArea);
                }
            } else {
                SKRect drawArea = new SKRect(0, 0, Bitmap.Width, Bitmap.Height);
                vCanvas.DrawBitmap(Bitmap, drawArea);
            }
        }
    }
}
