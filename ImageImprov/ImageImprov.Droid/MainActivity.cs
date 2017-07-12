using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Hardware;
//using Android.Hardware.Camera;
using Java.IO;
using Java.Util;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using System.Diagnostics;

namespace ImageImprov.Droid {
    [Activity(Label = "ImageImprov", 
        Icon = "@drawable/icon",
        //Theme = "@style/MainTheme", 
        //Theme = "@android:Theme.Holo.Light.NoActionBar",
        //MainLauncher = true, // removed to enable splash screen...
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,  
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : FormsApplicationActivity
    {
        //< file
        // creation here occurs before GlobalStatusSingleton.imgsTakenTracker is guaranteed to have been set.
        static File file;//   = new File(Android.OS.Environment.GetExternalStoragePublicDirectory(
                         //               Android.OS.Environment.DirectoryPictures), "ImageImprov_"+GlobalStatusSingleton.imgsTakenTracker+".jpg");
                         //> file

        static void HandleExceptions(object sender, UnhandledExceptionEventArgs e) {
            System.Console.WriteLine(e.ToString());
        }

        /// <summary>
        /// Saved for later retrieval and use by other activities. First instance was OAuth.
        /// </summary>
        public Bundle bundle;

        FileServices fs = new FileServices();
        //AuthServices authSvcs = new AuthServices();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += HandleExceptions;

            GlobalStatusSingleton.imgPath = 
                Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
            GlobalStatusSingleton.imgsTakenTracker = fs.determineNumImagesTaken() + 1;

            string nextFileName = GlobalStatusSingleton.IMAGE_NAME_PREFIX + GlobalStatusSingleton.imgsTakenTracker + ".jpg";
            file = new File(Android.OS.Environment.GetExternalStoragePublicDirectory(
                Android.OS.Environment.DirectoryPictures),  nextFileName);

            Forms.Init(this, savedInstanceState);
            
            // @todo find a device with no camera to test this with.
            if (Forms.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera) == false) {
                GlobalStatusSingleton.hasCamera = false;
            }

            try {
                LoadApplication(new App());
            } catch (Exception e) {
                var t = Task.Run(async () => { await GlobalSingletonHelpers.SendLogData("User:" + GlobalStatusSingleton.username + " crash:" + e.ToString()); });
                t.Wait();

                System.Console.WriteLine("{0} Exception caught.", e);
            }

            if ((GlobalStatusSingleton.UUID == null) 
                || (!Xamarin.Forms.Application.Current.Properties.ContainsKey(App.PROPERTY_UUID))
                || (GlobalStatusSingleton.UUID.Equals(string.Empty))) {
                GlobalStatusSingleton.UUID = UUID.RandomUUID().ToString();
            }

            //this.bundle = savedInstanceState;
            //< OnCreate
            //cameraSetup();  This doesn't work yet, so don't do it.

            // This is adding functionality to ShouldTakePicture based on the fact we are the droid app.
            // Can I pass through MainPage variable?
            ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShouldTakePicture += () => {
                var intent = new Intent(MediaStore.ActionImageCapture);
                intent.PutExtra("android.intent.extra.quickCapture", true);
                intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(file));
                //StartActivityForResult(intent, 0);
                StartActivityForResult(intent, 1);
                
            };
            //> OnCreate

        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // non-original code added so we have the bytes.
            var bytes = default(byte[]);
            using (var streamReader = new System.IO.StreamReader(file.Path)) {
                using (System.IO.MemoryStream memStream = new System.IO.MemoryStream()) {
                    streamReader.BaseStream.CopyTo(memStream);
                    bytes = memStream.ToArray();
                }
            }
            // end non-original code
            
            //< OnActivityResult
            ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShowImage(file.Path, bytes);

            //> OnActivityResult
        }

        /* the camera2 code
        private void cameraSetup() {
            CameraManager manager = (CameraManager)Forms.Context.GetSystemService(Context.CameraService);
            try {
                String[] cameraIds = manager.GetCameraIdList();
                foreach (String cameraId in cameraIds) {
                    CameraCharacteristics cc = manager.GetCameraCharacteristics(cameraId);
                    var pixels = cc[SENSORInfo]
                }
            } catch (CameraAccessException e) {
                System.Diagnostics.Debug.WriteLine("DHB:Droid:MainActivity:cameraSetup camera exception. " + e.ToString());
            }
        }
        */
        private void cameraSetup() {
            Camera c = null;
            int largestSquare = 0;
            int shortestSideOfLargestCamera = 0;
            try {
                for (int i = 0; i < Camera.NumberOfCameras; i++) {
                    c = Camera.Open(i);
                    Camera.Parameters cParams = c.GetParameters();
                    System.Diagnostics.Debug.WriteLine("Picture size at start:" + cParams.PictureSize.Width + ", " + cParams.PictureSize.Height);
                    IList<Camera.Size> sizes = cParams.SupportedPictureSizes;
                    foreach (Camera.Size s in sizes) {
                        System.Diagnostics.Debug.WriteLine("size: " + s.Width + ", " + s.Height);
                        if ((s.Width == s.Height) && (s.Width > largestSquare)) {
                            largestSquare = s.Width;
                        }
                        int shortestSide = (s.Width > s.Height) ? s.Height : s.Width;
                        if (shortestSide > shortestSideOfLargestCamera) {
                            shortestSideOfLargestCamera = shortestSide;
                        }
                    }
                    System.Diagnostics.Debug.WriteLine("Largest Square:" + largestSquare);
                    System.Diagnostics.Debug.WriteLine("ShortestSide of Largest Size:" + shortestSideOfLargestCamera);
                    //if (largestSquare==0) { largestSquare = shortestSideOfLargestCamera; }
                    cParams.SetPictureSize(largestSquare, largestSquare);
                    c.SetParameters(cParams);
                    cParams = c.GetParameters();
                    System.Diagnostics.Debug.WriteLine("Picture size post:" + cParams.PictureSize.Width + ", " + cParams.PictureSize.Height);
                    c.Release();
                    c = Camera.Open(i);
                    cParams = c.GetParameters();
                    System.Diagnostics.Debug.WriteLine("Picture size post release:" + cParams.PictureSize.Width + ", " + cParams.PictureSize.Height);
                    c.Release();
                }
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("DHB:MainActivity:CameraSetup exception:" +e.ToString());
            }
            System.Diagnostics.Debug.WriteLine("Do I have a camera obj?");
        }
    }
}

