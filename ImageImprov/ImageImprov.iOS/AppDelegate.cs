﻿/*
using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
*/
using System;
using System.Collections.Generic;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using System.IO;

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

            LoadApplication(new App());

            if (!Xamarin.Forms.Application.Current.Properties.ContainsKey(App.PROPERTY_UUID)) {
                NSUuid generator = new NSUuid();
                generator.Init();
                GlobalStatusSingleton.UUID = generator.ToString();
                // this gives the device id.
                //UIKit.UIDevice.CurrentDevice.IdentifierForVendor.AsString();
            }

            //< imagePicker
            var imagePicker = new UIImagePickerController { SourceType = UIImagePickerControllerSourceType.Camera };
            //> imagePicker

            //< PresentViewController
            ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShouldTakePicture += () =>
                uiApplication.KeyWindow.RootViewController.PresentViewController(
                imagePicker, true, null);
            //> PresentViewController

            //< FinishedPickingMedia
            imagePicker.FinishedPickingMedia += (sender, e) => {
                /*
                var filepath = Path.Combine(Environment.GetFolderPath(
                                   Environment.SpecialFolder.MyDocuments), "tmp.png");
                                   */
                var filepath = Path.Combine(Environment.GetFolderPath(
                                   Environment.SpecialFolder.MyDocuments), "ImageImprov_" + GlobalStatusSingleton.imgsTakenTracker + ".jpg");

                var image = (UIImage)e.Info.ObjectForKey(new NSString("UIImagePickerControllerOriginalImage"));
                InvokeOnMainThread(() => {
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
                });
                uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            };
            //> FinishedPickingMedia

            //< Canceled
            imagePicker.Canceled += (sender, e) => uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            //> Canceled

            return base.FinishedLaunching(uiApplication, launchOptions);
        }

    }
}

