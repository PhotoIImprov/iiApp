using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Reflection;

namespace ImageImprov {
    class iiBitmapView : SKCanvasView {
        /// <summary>
        /// @todo You need to actually create this bitmap!!
        /// </summary>
        public static SKBitmap BLANK_BITMAP = null; //loadBitmap("ListViewLearning.Images.ImageImprov_2.jpg");


        public SKBitmap Bitmap {
            get { return (SKBitmap)GetValue(BitmapProperty); }
            set { SetValue(BitmapProperty, value); }
        }

        private static void OnBitmapChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as iiBitmapView;
            Device.BeginInvokeOnMainThread(() => myObj.InvalidateSurface());
        }

        public static readonly BindableProperty BitmapProperty
            = BindableProperty.Create("Bitmap", typeof(SKBitmap), typeof(iiBitmapView), BLANK_BITMAP, BindingMode.Default, null, OnBitmapChanged);

        // Scaling Property.
        public bool Scaling {
            get { return (bool)GetValue(ScalingProperty); }
            set { SetValue(ScalingProperty, value); }
        }

        private static void OnScalingChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as iiBitmapView;
            Device.BeginInvokeOnMainThread(() => myObj.InvalidateSurface());
        }

        public static readonly BindableProperty ScalingProperty
            = BindableProperty.Create("Scaling", typeof(bool), typeof(iiBitmapView), true, BindingMode.Default, null, OnScalingChanged);

        public iiBitmapView() {
            this.PaintSurface += OnCanvasViewPaintSurface;
        }

        public iiBitmapView(SKBitmap bitmap) {
            Bitmap = bitmap;
            this.PaintSurface += OnCanvasViewPaintSurface;
        }

        public void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e) {
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
                // currently, always maintain aspect ratio.
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

        /*
        public static implicit operator iiBitmapView(PropertyInfo v) {
            throw new NotImplementedException();
        }
        */
    }
}

