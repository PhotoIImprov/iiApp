using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Reflection;

namespace ImageImprov {
    public class iiBitmapView : SKCanvasView {
        public const string LOADING_IMG_NAME = "ImageImprov.IconImages.ii_loading.png";
        /// <summary>
        /// @todo You need to actually create this bitmap!!
        /// </summary>
        public static SKBitmap BLANK_BITMAP = null; //loadBitmap("ListViewLearning.Images.ImageImprov_2.jpg");
        //public static SKBitmap BLANK_BITMAP = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, DONT have a static way to access assembly);

        //public static PhotoMetaJSON DEFAULT_PHOTO_META = null; // have nada as the default.
        public static PhotoMetaJSON DEFAULT_PHOTO_META = new PhotoMetaJSON(); // have nada as the default.


        public SKBitmap Bitmap {
            get { return (SKBitmap)GetValue(BitmapProperty); }
            //set { SetValue(BitmapProperty, value); }
            set {
                Device.BeginInvokeOnMainThread(() => {
                    SetValue(BitmapProperty, value);
                    //if (BitmapProperty == null) SetValue(BitmapProperty, BLANK_BITMAP);
                });
            }
        }

        private static void OnBitmapChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as iiBitmapView;
            Device.BeginInvokeOnMainThread(() => myObj.InvalidateSurface());
        }

        public static readonly BindableProperty BitmapProperty
            = BindableProperty.Create("Bitmap", typeof(SKBitmap), typeof(iiBitmapView), BLANK_BITMAP, BindingMode.Default, null, OnBitmapChanged);

        // PhotoMeta Property.
        public PhotoMetaJSON PhotoMeta {
            get { return (PhotoMetaJSON)GetValue(PhotoMetaProperty); }
            set { SetValue(PhotoMetaProperty, value); }
        }

        private static void OnPhotoMetaChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as iiBitmapView;
        }

        public static readonly BindableProperty PhotoMetaProperty
            = BindableProperty.Create("PhotoMeta", typeof(PhotoMetaJSON), typeof(iiBitmapView), DEFAULT_PHOTO_META, BindingMode.Default, null, OnPhotoMetaChanged);

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

        // EnsureSquare Property. Defaults to true.
        public bool EnsureSquare {
            get { return (bool)GetValue(EnsureSquareProperty); }
            set { SetValue(EnsureSquareProperty, value); }
        }

        private static void OnEnsureSquareChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as iiBitmapView;
            Device.BeginInvokeOnMainThread(() => myObj.InvalidateSurface());
        }

        public static readonly BindableProperty EnsureSquareProperty
            = BindableProperty.Create("EnsureSquare", typeof(bool), typeof(iiBitmapView), true, BindingMode.Default, null, OnEnsureSquareChanged);

        public iiBitmapView() {
            if (BLANK_BITMAP == null) {
                BLANK_BITMAP = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, this.GetType().GetTypeInfo().Assembly);
            }
            this.PaintSurface += OnCanvasViewPaintSurface;
            //Bitmap = BLANK_BITMAP;
        }

        public iiBitmapView(SKBitmap bitmap) {
            Bitmap = bitmap;
            this.PaintSurface += OnCanvasViewPaintSurface;
        }

        public virtual void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e) {
            if (Bitmap != null) {
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

        /*
        public static implicit operator iiBitmapView(PropertyInfo v) {
            throw new NotImplementedException();
        }
        */
    }
}

