/*
using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
*/
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using System.IO;
using System;

namespace ImageImprov.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            Forms.Init();

            LoadApplication(new App());

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
                var filepath = Path.Combine(Environment.GetFolderPath(
                                   Environment.SpecialFolder.MyDocuments), "tmp.png");
                var image = (UIImage)e.Info.ObjectForKey(new NSString("UIImagePickerControllerOriginalImage"));
                InvokeOnMainThread(() => {
                    image.AsPNG().Save(filepath, false);
                    ((ICamera)(((IExposeCamera)(Xamarin.Forms.Application.Current as App).MainPage).getCamera())).ShowImage(filepath);
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

