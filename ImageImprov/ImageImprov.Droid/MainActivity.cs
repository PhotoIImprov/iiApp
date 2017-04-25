using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Provider;
using Java.IO;
using Java.Util;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;



namespace ImageImprov.Droid
{
    [Activity(Label = "ImageImprov", 
        Icon = "@drawable/icon", 
        //Theme = "@style/MainTheme", 
        //Theme = "@android:Theme.Holo.Light.NoActionBar",
        MainLauncher = true, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : FormsApplicationActivity
    {
        //< file
        static readonly File file = new File(Android.OS.Environment.GetExternalStoragePublicDirectory(
                                        Android.OS.Environment.DirectoryPictures), "tmp.jpg");
        //> file

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Forms.Init(this, savedInstanceState);
            
            // @todo find a device with no camera to test this with.
            if (Forms.Context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera) == false) {
                GlobalStatusSingleton.hasCamera = false;
            }

            try
            {
                LoadApplication(new App());
            }
            catch (Exception e)
            {
                System.Console.WriteLine("{0} Exception caught.", e);
            }

            if ((GlobalStatusSingleton.UUID == null) 
                || (!Xamarin.Forms.Application.Current.Properties.ContainsKey(App.PROPERTY_UUID))
                || (GlobalStatusSingleton.UUID.Equals(string.Empty))) {
                GlobalStatusSingleton.UUID = UUID.RandomUUID().ToString();
            }

            //< OnCreate
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
    }
}

