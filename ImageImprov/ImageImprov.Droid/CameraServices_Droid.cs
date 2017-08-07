using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware;

namespace ImageImprov.Droid {
    [Activity]
    class CameraServices_Droid : Activity { //, TextureView.ISurfaceTextureListener {
        //private TextureView textureView;
        //private SurfaceView surfaceView;
        private CameraPreview surfaceView;
        private Camera camera;
        private RelativeLayout upperOverlay;
        private RelativeLayout lowerOverlay;
        // snap pic
        private bool flashMode = false;
        private int cameraId;
        // flash button
        // other camera button

        private Context context;

        const int TAKE_PICTURE_BUTTON_ID = 2002;
        const int CATEGORY_HEADER_ID = 2001;

        private bool checkCameraHardware(Context context) {
            if (context.PackageManager.HasSystemFeature("android.hardware.camera")) {
                return true;
            } else {
                return false;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState) {
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnCreate made it here!");
            RequestWindowFeature(WindowFeatures.NoTitle);
            // I want the status bars.
            //getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);

            base.OnCreate(savedInstanceState);

            RelativeLayout myLayout = new RelativeLayout(this);
            myLayout.SetBackgroundColor(Android.Graphics.Color.White);

            // category header
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnCreate layout created!");
            Button categoryButton = new Button(this) { Text = GlobalStatusSingleton.uploadingCategories[0].description, };
            RelativeLayout.LayoutParams btnParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.FillParent, WindowManagerLayoutParams.WrapContent);
            btnParams.AddRule(LayoutRules.CenterHorizontal);
            categoryButton.LayoutParameters = btnParams;
            categoryButton.SetBackgroundColor(Android.Graphics.Color.Argb(255, 252, 213, 21));
            categoryButton.Id = CATEGORY_HEADER_ID;
            categoryButton.Click += OnExit;
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnCreate button created!");
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnCreate button added!");
            // end category header

            // surface view
            if (camera == null) {
                camera = getCameraInstance();
            }
            surfaceView = new CameraPreview(this, camera);
            RelativeLayout.LayoutParams viewParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            viewParams.AddRule(LayoutRules.Below, CATEGORY_HEADER_ID);
            viewParams.AddRule(LayoutRules.Above, TAKE_PICTURE_BUTTON_ID);
            surfaceView.LayoutParameters = viewParams;
            //surfaceView.Rotation = 90.0f; // is this always true that it needs to rotate?
            // end surface view
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnCreate we have camera");

            // upper overlay
            upperOverlay = new RelativeLayout(this);
            upperOverlay.SetBackgroundColor(Android.Graphics.Color.White);
            RelativeLayout.LayoutParams uoParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            uoParams.AddRule(LayoutRules.Below, CATEGORY_HEADER_ID);
            upperOverlay.LayoutParameters = uoParams;
            // end upper overlay
            // lower overlay
            lowerOverlay = new RelativeLayout(this);
            lowerOverlay.SetBackgroundColor(Android.Graphics.Color.White);
            RelativeLayout.LayoutParams loParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            loParams.AddRule(LayoutRules.Above, TAKE_PICTURE_BUTTON_ID);  // will be interesting to see if take_picture_button needed to exist first...
            lowerOverlay.LayoutParameters = loParams;
            // end lower overlay

            // take picture button
            ImageButton takePictureButton = new ImageButton(this);
            var btnImg = Android.Support.Graphics.Drawable.VectorDrawableCompat.Create(this.Resources, Resource.Drawable.contests_inactive, null);
            takePictureButton.SetImageDrawable(btnImg);
            takePictureButton.Click += OnSnapPicture;
            takePictureButton.SetBackgroundColor(Android.Graphics.Color.Transparent);
            RelativeLayout.LayoutParams picParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, WindowManagerLayoutParams.WrapContent);
            picParams.AddRule(LayoutRules.CenterHorizontal);
            picParams.AddRule(LayoutRules.AlignParentBottom);
            takePictureButton.LayoutParameters = picParams;
            takePictureButton.Id = TAKE_PICTURE_BUTTON_ID;
            // end take picture button

            myLayout.AddView(surfaceView);
            myLayout.AddView(upperOverlay);
            myLayout.AddView(lowerOverlay);
            myLayout.AddView(categoryButton);
            myLayout.AddView(takePictureButton);
            SetContentView(myLayout);
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnCreate done");
        }

        public override void OnWindowFocusChanged(bool hasFocus) {
            base.OnWindowFocusChanged(hasFocus);
            int previewWidth = surfaceView.MeasuredWidth;
            int previewHeight = surfaceView.MeasuredHeight;
            int adjustedHeight = (previewHeight - previewWidth) / 2;

            RelativeLayout.LayoutParams uoParams = (RelativeLayout.LayoutParams)upperOverlay.LayoutParameters;
            uoParams.Height = adjustedHeight;
            upperOverlay.LayoutParameters = uoParams;

            RelativeLayout.LayoutParams loParams = (RelativeLayout.LayoutParams)lowerOverlay.LayoutParameters;
            loParams.Height = adjustedHeight;
            lowerOverlay.LayoutParameters = loParams;
        }

        protected override void OnResume() {
            base.OnResume();
            if (camera == null) {
                camera = getCameraInstance();
            }
        }

        protected override void OnPause() {
            base.OnPause();
            if (camera != null) {
                camera.Release();
                camera = null;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (camera != null) {
                camera.Release();
                camera = null;
            }
        }

        public void OnSnapPicture(object sender, EventArgs e) {
            // image save code goes here.
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnSnapPicture snap!");
            MyPictureData raw = new MyPictureData();
            raw.timeToExit += OnExit;
            camera.TakePicture(null, null, raw);

            //this.Finish(); need to generate an event and fire this there. firing here occurs before takePic and invalidates it.
        }

        public void OnExit(object sender, EventArgs e) {
            // image save code goes here.
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:OnExit");
            this.Finish();
        }


        public class MyPictureData : Java.Lang.Object, Camera.IPictureCallback {
            public EventHandler timeToExit;
            public void OnPictureTaken(byte[] data, Camera camera) {
                System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:MyPictureData:OnPictureTaken here!");
                GlobalStatusSingleton.latestImg = GlobalSingletonHelpers.rotateAndCrop(GlobalSingletonHelpers.SKBitmapFromBytes(data));
                GlobalStatusSingleton.mostRecentImgBytes = data;  // this is the un modified data as jpg.  bytes will give me a byte array (i think)
                //GlobalStatusSingleton.mostRecentImgBytes = GlobalStatusSingleton.latestImg.Bytes;  this is 34mb.! :)
                System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:MyPictureData:OnPictureTaken here!");
                timeToExit(this, null);
            }
        }

        public static Camera getCameraInstance() {
            Camera c = null;
            try {
                c = Camera.Open((int)Android.Hardware.CameraFacing.Back);
                if (c != null) {
                    //toLargestSquare(c);
                    toLargestImage(c);
                }
            } catch (Exception e) {

            }
            return c;
        }

        private static int getBestDimensions(IList<Camera.Size> sizes, ref int w, ref int h) {
            int largestSquare = 0;
            int shortestSideOfLargestCamera = 0;
            foreach (Camera.Size s in sizes) {
                System.Diagnostics.Debug.WriteLine("size: " + s.Width + ", " + s.Height);
                if ((s.Width == s.Height) && (s.Width > largestSquare)) {
                    largestSquare = s.Width;
                }
                int shortestSide = (s.Width > s.Height) ? s.Height : s.Width;
                if (shortestSide > shortestSideOfLargestCamera) {
                    shortestSideOfLargestCamera = shortestSide;
                }
                w = (s.Width > w) ? s.Width : w;
                h = (s.Height > h) ? s.Height : h;
            }
            if (largestSquare > 0) {
                // we have a square alternative.  switch to square.
                System.Diagnostics.Debug.WriteLine("Largest Square:" + largestSquare);
                w = largestSquare;
                h = largestSquare;
            }
            System.Diagnostics.Debug.WriteLine("ShortestSide of Largest Size:" + shortestSideOfLargestCamera);
            return largestSquare;
        }

        private static double calcAspectRatio(int w, int h) {
            return (((double)w) / ((double)h));
        }

        /// <summary>
        /// Given I have a camera, sets the image taken to the largest square image format.
        /// </summary>
        private static void toLargestSquare(Camera inCamera) {
            int outputW = 0, outputH = 0;
            int previewW = 0, previewH = 0;
            try {
                Camera.Parameters cParams = inCamera.GetParameters();
                System.Diagnostics.Debug.WriteLine("Picture size at start:" + cParams.PictureSize.Width + ", " + cParams.PictureSize.Height);
                getBestDimensions(cParams.SupportedPictureSizes, ref outputW, ref outputH);
                getBestDimensions(cParams.SupportedPreviewSizes, ref previewW, ref previewH);
                //if (largestSquare==0) { largestSquare = shortestSideOfLargestCamera; }
                cParams.SetPictureSize(outputW, outputH);
                cParams.SetPreviewSize(previewW, previewH);
                cParams.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
                inCamera.SetParameters(cParams);
                if (calcAspectRatio(outputW, outputH) != calcAspectRatio(previewW, previewH)) {
                    System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:toLargestSquare image and preview sizes have different aspect ratios!!");
                } else {
                    System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:toLargestSquare image and preview size aspect ratios match");
                }
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:toLargestSquare exception: " + e.ToString());
            }
        }

        private static void getLargestDimensions(IList<Camera.Size> sizes, ref int w, ref int h, double aspectRatio = 0.0) {
            foreach (Camera.Size s in sizes) {
                //System.Diagnostics.Debug.WriteLine("size: " + s.Width + ", " + s.Height);
                if (aspectRatio > 0.0) {
                    if (aspectRatio == calcAspectRatio(s.Width, s.Height)) {
                        w = (s.Width > w) ? s.Width : w;
                        h = (s.Height > h) ? s.Height : h;
                    }
                } else {
                    w = (s.Width > w) ? s.Width : w;
                    h = (s.Height > h) ? s.Height : h;
                }
            }
            System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:getLargestDimensions w:" + w + ", h:" + h);
            return;
        }

        /// <summary>
        /// The square comes out distorted, despite having a square input.
        /// Plus, this square is a special case since not every device has a square view.
        /// So sacrifice some image detail for consistency.
        /// </summary>
        /// <param name="inCamera"></param>
        private static void toLargestImage(Camera inCamera) {
            int outputW = 0, outputH = 0;
            int previewW = 0, previewH = 0;
            try {
                Camera.Parameters cParams = inCamera.GetParameters();
                System.Diagnostics.Debug.WriteLine("Picture size at start:" + cParams.PictureSize.Width + ", " + cParams.PictureSize.Height);
                getLargestDimensions(cParams.SupportedPictureSizes, ref outputW, ref outputH);
                double aspectRatio = calcAspectRatio(outputW, outputH);
                getLargestDimensions(cParams.SupportedPreviewSizes, ref previewW, ref previewH, aspectRatio);
                //if (largestSquare==0) { largestSquare = shortestSideOfLargestCamera; }
                cParams.SetPictureSize(outputW, outputH);
                cParams.SetPreviewSize(previewW, previewH);
                inCamera.SetParameters(cParams);
                if (calcAspectRatio(outputW, outputH) != calcAspectRatio(previewW, previewH)) {
                    System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:toLargestImage image and preview sizes have different aspect ratios!!");
                } else {
                    System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:toLargestImage image and preview size aspect ratios match");
                }
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("DHB:CameraServices_Droid:toLargestImage exception: " + e.ToString());
            }
        }
    }
}