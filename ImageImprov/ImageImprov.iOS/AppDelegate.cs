/*
using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Auth;
using System.IO;
using System.Net;
using CarouselView.FormsPlugin.iOS;
using SkiaSharp;
using Facebook.CoreKit;

namespace ImageImprov.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        public const string PROPERTY_UUID = "uuid";
        public const string PROPERTY_USERNAME = "username";
        public const string PROPERTY_PWD = "pwd";
        public const string PROPERTY_MAINTAIN_LOGIN = "maintainlogin";
        public const string PROPERTY_ASPECT_OR_FILL_IMGS = "aspectOrFillImgs";
        public const string PROPERTY_MIN_BALLOTS_TO_LOAD = "minBallotsToLoad";
        public const string PROPERTY_IMGS_TAKEN_COUNT = "imgsTakenCount";
        public const string PROPERTY_ACTIVE_BALLOT = "activeBallot";
        public const string PROPERTY_QUEUE_SIZE = "ballotQueueSize";
        public const string PROPERTY_BALLOT_QUEUE = "ballotQueue";
        public const string PROPERTY_LEADERBOARD_CATEGORY_LIST_SIZE = "leaderboardCategoryListSize";
        public const string PROPERTY_LEADERBOARD_CATEGORY_LIST_KEY = "leaderboardCategoryListKey";
        public const string PROPERTY_LEADERBOARD_CATEGORY_LIST_VALUE = "leaderboardCategoryListValue";
        public const string PROPERTY_LEADERBOARD_TIMESTAMP = "leaderboardCategoryLastLoadTimestamp";
        //public const string PROPERTY_REGISTERED = "registered";

        private FileServices fs = new FileServices();
        private AuthServices authSvcs = new AuthServices();
        private Notifications notify = new ImageImprov.iOS.Notifications();
        private FacebookLogin_iOS fbLogin = new FacebookLogin_iOS();

        public static NSData snappedImgData = null;

        UIApplication uiApplication;

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        // @todo Check for camera availability and set in GlobalStatusSingleton
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            this.uiApplication = uiApplication;
            Forms.Init();
            CarouselViewRenderer.Init();
            //var cv = typeof(Xamarin.Forms.CarouselView);
            //var assembly = Assembly.Load(cv.FullName);

            Debug.WriteLine("DHB:AppDelegate:FinishedLaunching pre imgPath.");
            GlobalStatusSingleton.imgPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Debug.WriteLine("DHB:AppDelegate:FinishedLaunching imgPath=" + GlobalStatusSingleton.imgPath);

            LoadApplication(new App());

            if ((!Xamarin.Forms.Application.Current.Properties.ContainsKey(App.PROPERTY_UUID)) 
                || (Xamarin.Forms.Application.Current.Properties[App.PROPERTY_UUID].Equals(""))) {
                NSUuid generator = new NSUuid();
                generator.Init();
                GlobalStatusSingleton.UUID = generator.ToString();
                // this gives the device id.
                //UIKit.UIDevice.CurrentDevice.IdentifierForVendor.AsString();
            }
            Debug.WriteLine("DHB:AppDelegate:FinishedLaunching guid set to:" + GlobalStatusSingleton.UUID);

            //notificationTest();
            notify.RequestAuthorization();

            fbLogin.Init();
            FacebookLogin_iOS.uiApplication = uiApplication;
            CameraServices_iOS myCamera = new CameraServices_iOS();

            ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShouldTakePicture += () => {
                Debug.WriteLine("pre launch");
                uiApplication.KeyWindow.RootViewController.PresentViewController(myCamera, true, null);
                Debug.WriteLine("present called already");
            };

            //< FinishedPickingMedia
            myCamera.FinishedPickingMedia += OnCameraFinished;

            //> FinishedPickingMedia

            //< Canceled
            // i don't have a cancel button...
            //imagePicker.Canceled += (sender, e) => uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            //> Canceled

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public void OnCameraFinished(Object sender, EventArgs e) {
            /*
            var filepath = Path.Combine(Environment.GetFolderPath(
                               Environment.SpecialFolder.MyDocuments), "tmp.png");
                               */

            InvokeOnMainThread(() => {
                Debug.WriteLine("DHB:AppDelegate:FinishedLaunching:FinishedPickingMedia_Anon");
                if (CameraServices_iOS.exitFromPhoto == true) {  // probably better to acheive this by subclassing the event. but i need something quick and dirty.
                    string nextFileName = GlobalStatusSingleton.IMAGE_NAME_PREFIX + GlobalStatusSingleton.imgsTakenTracker + ".jpg";
                    var filepath = Path.Combine(Environment.GetFolderPath(
                                       Environment.SpecialFolder.MyDocuments), nextFileName);

                    SKBitmap bitmap = GlobalSingletonHelpers.SKBitmapFromBytes(GlobalStatusSingleton.mostRecentImgBytes);
                    if (bitmap != null) {
                        int degreesToRotate = CameraServices_iOS.calculateRotationDegrees();
                        Debug.WriteLine("DHB:AppDelegate:FinishedLaunching:FinishedPickingMedia_Anon time to rotate and crop; degrees:" + degreesToRotate);
                        SKBitmap finalBmp = GlobalSingletonHelpers.rotateAndCrop(bitmap, degreesToRotate);
                        //((MainPage)((Xamarin.Forms.Application.Current as App).MainPage)).img.Bitmap = finalBmp;
                        NSData finalBytes = NSData.FromArray(finalBmp.Bytes);
                        AppDelegate.snappedImgData = finalBytes;
                        GlobalStatusSingleton.latestImg = finalBmp;
                    } else {
                        Debug.WriteLine("DHB:AppDelegate:FinishedLaunching:FinishedPickingMedia_Anon bitmap was null");
                    }

                    if (snappedImgData != null) {
                        snappedImgData.Save(filepath, false);
                    }

                    byte[] imgBytes = null;
                    using (var streamReader = new System.IO.StreamReader(filepath)) {
                        using (System.IO.MemoryStream memStream = new System.IO.MemoryStream()) {
                            streamReader.BaseStream.CopyTo(memStream);
                            imgBytes = memStream.ToArray();
                        }
                    }

                ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShowImage(filepath, imgBytes);
                    GlobalStatusSingleton.imgsTakenTracker++;
                }
            });
            uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
        }

        // Called by ThirdParty Authentication login.
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) {
            if (ImageImprov.ThirdPartyAuthenticator.authenticator == null) {
                // This case occurs when using FBook sdk rather than Xamarin.Auth
                // bleh. no easy cast...
                var keys = options.Keys;
                var keys2 = new NSString[keys.Length];
                int i = 0;
                foreach (NSObject k in keys) {
                    keys2[i] = (NSString)k;
                    i++;
                }
                var values = options.Values;

                NSDictionary<NSString, NSObject> copy2 = NSDictionary<NSString, NSObject>.FromObjectsAndKeys(values,keys2);

                ApplicationDelegate.SharedInstance.OpenUrl(app, url, copy2);
            } else {
                // convert NSUrl to Uri
                var uri = new Uri(url.AbsoluteString);
                // Load redirectUrl page from OAuth.

                ImageImprov.ThirdPartyAuthenticator.authenticator.OnPageLoading(uri);
            }
            return true;
        }

        // called by fbook login
        // This is what fbook asks for, but does not exist.
        //public override bool OpenUrl(UIApplication app, NSUrl url, string sourceApplication, NSDictionary options) {
          //  return ApplicationDelegate.SharedInstance.OpenUrl(app, url, sourceApplication, options);
        //}

        protected void notificationTest() {
            notify.RequestAuthorization();
            //bool canNotify = notify.CheckNotificationPriveledges();
            bool canNotify = true;
            if (canNotify) {
                Debug.WriteLine("DHB:AppDelegate:notificationTest adding a notification");
                var content = notify.BuildNotificationContent("iiTest", "this is a testing message");
                notify.ScheduleNotification(content, DateTime.Now, "1337");
                Debug.WriteLine("DHB:AppDelegate:notificationTest scheduled");
                notify.RetrieveNotifications();
                Debug.WriteLine("DHB:AppDelegate:notificationTest done");
            } else {
                Debug.WriteLine("DHB:AppDelegate:notificationTest No notification priviledges.");
            }
        }
    }
}

