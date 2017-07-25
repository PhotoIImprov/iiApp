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

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Auth;
using System.IO;
using System.Net;

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
            Forms.Init();

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

            //< imagePicker
            var imagePicker = new UIImagePickerController { SourceType = UIImagePickerControllerSourceType.Camera };
            //> imagePicker

            //< PresentViewController
            ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShouldTakePicture += () =>
                uiApplication.KeyWindow.RootViewController.PresentViewController(imagePicker, true, null);
            //> PresentViewController

            //< FinishedPickingMedia
            imagePicker.FinishedPickingMedia += (sender, e) => {
                /*
                var filepath = Path.Combine(Environment.GetFolderPath(
                                   Environment.SpecialFolder.MyDocuments), "tmp.png");
                                   */

                var image = (UIImage)e.Info.ObjectForKey(new NSString("UIImagePickerControllerOriginalImage"));
                InvokeOnMainThread(() => {
                    string nextFileName = GlobalStatusSingleton.IMAGE_NAME_PREFIX + GlobalStatusSingleton.imgsTakenTracker + ".jpg";
                    var filepath = Path.Combine(Environment.GetFolderPath(
                                       Environment.SpecialFolder.MyDocuments), nextFileName);

                    //image.AsPNG().Save(filepath, false);
                    image.AsJPEG().Save(filepath, false);

                    byte[] imgBytes = null;
                    using (var streamReader = new System.IO.StreamReader(filepath)) {
                        using (System.IO.MemoryStream memStream = new System.IO.MemoryStream()) {
                            streamReader.BaseStream.CopyTo(memStream);
                            imgBytes = memStream.ToArray();
                        }
                    }

                    ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShowImage(filepath, imgBytes);
                    GlobalStatusSingleton.imgsTakenTracker++;
                });
                uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            };
            //> FinishedPickingMedia

            //< Canceled
            imagePicker.Canceled += (sender, e) => uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            //> Canceled

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) {
            // convert NSUrl to Uri
            var uri = new Uri(url.AbsoluteString);
            // Load redirectUrl page from OAuth.

            ImageImprov.ThirdPartyAuthenticator.authenticator.OnPageLoading(uri);
            return true;
        }

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

