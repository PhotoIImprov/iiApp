using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

using Foundation;
using UIKit;
using Xamarin.Forms;
using Facebook;
using Facebook.LoginKit;
using Facebook.CoreKit;

[assembly: Dependency(typeof(ImageImprov.iOS.FacebookLogin_iOS))]
namespace ImageImprov.iOS {
    class FacebookLogin_iOS : UIViewController, ImageImprov.I_ii_FacebookLogin { //, IFacebookCallback {
        //private ICallbackManager mCallbackManager;
        // There's something about dependency services that creates multiple instances of this object.
        // Make the callback a static so I'm guaranteed to have it.
        private static LoginPage loginCallback;
        string[] readPermissions = new string[] { "email", "public_profile", "user_friends" };
        private static LoginManager manager;
        //UIViewController viewCtrl;

        public static UIApplication uiApplication;

        public void Init() {
            //mCallbackManager = CallbackManagerFactory.Create();
            //LoginManager.Instance.RegisterCallback(mCallbackManager, this);
            //printKey();
            manager = new LoginManager();
            manager.Init();
        }

        public void SetLoginCallback(LoginPage callback) {
            loginCallback = callback;
        }

        /*
         private void printKey() {
            PackageInfo info = activity.PackageManager.GetPackageInfo("com.imageimprov", PackageInfoFlags.Signatures);
            foreach (Android.Content.PM.Signature signature in info.Signatures) {
                MessageDigest md = MessageDigest.GetInstance("SHA");
                md.Update(signature.ToByteArray());
                string keyhash = System.Convert.ToBase64String(md.Digest());
                System.Diagnostics.Debug.WriteLine("KeyHash:" + keyhash);
            }
        }
        */

        public void startFacebookLogin() {
            //LoginManager.Instance.LogInWithReadPermissions(activity, new List<string> { "public_profile", "user_friends", "email" });
            //LoginManager.Instance.LogInWithReadPermissions((Activity)Forms.Context, new List<string> { "public_profile", "user_friends", "email" });
            if (loginCheck()) {
                // logged in already
            } else {
                uiApplication.KeyWindow.RootViewController.PresentViewController(this, true, null);
                
                manager.LogInWithReadPermissions(readPermissions, this, (result, error) =>
                {
                    if (error != null) {
                        new UIAlertView("Error", error.Description, null, "Ok", null).Show();
                        return;
                    }

                    if (result.IsCancelled) {
                        //new UIAlertView("Cancelled", error.Description, null, "Ok", null).Show();
                        var alert = UIAlertController.Create("Cancelled", "Login with Facebook cancelled", UIAlertControllerStyle.Alert);
                        alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, null));
                        PresentViewController(alert, animated: true, completionHandler: null);
                        //uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
                        return;
                    }

                    //new UIAlertView("Success", error.Description, null, "Ok", null).Show();
                    OnLoginSuccess();
                    var successAlert = UIAlertController.Create("Success", "Login with Facebook complete", UIAlertControllerStyle.Alert);
                    successAlert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Cancel, null));
                    PresentViewController(successAlert, animated: true, completionHandler: null);
                    uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
                }); 
                
                Debug.WriteLine("DHB:FacebookLogin_iOS: ");
                //return ApplicationDelegate.SharedInstance.FinishedLaunching(app)
                //uiApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            }
        }

        public async void OnLoginSuccess() {
            if (loginCheck()) {
                loginCallback.Content = loginCallback.createPreConnectAutoLoginLayout();
                // grab token and go!
                GlobalStatusSingleton.maintainLogin = true;
                string result = await ThirdPartyAuthenticator.requestTokenAsync(ThirdPartyAuthenticator.METHOD_FACEBOOK, AccessToken.CurrentAccessToken.TokenString);
                if (result.Equals("Success")) {
                    GlobalStatusSingleton.facebookRefreshToken = AccessToken.CurrentAccessToken.TokenString;
                    // request token takes care of housekeeping issues, like setting the email address. goto login success steps.
                    loginCallback.LoginSuccess();
                }
            }
        }

        public async Task<string> relogin() {
            string result = await ThirdPartyAuthenticator.requestTokenAsync(ThirdPartyAuthenticator.METHOD_FACEBOOK, GlobalStatusSingleton.facebookRefreshToken);
            return result;
        }

        public bool loginCheck() {
            bool loggedIn = false;
            // profile is not passed in by default on iOS.
            //if ((AccessToken.CurrentAccessToken != null) && (Profile.CurrentProfile != null)) {
            if (AccessToken.CurrentAccessToken != null) { 
                System.Diagnostics.Debug.WriteLine("DHB: LoggedIn!");
                loggedIn = true;
            }
            return loggedIn;
        }

        public void logout() {
            manager.LogOut();
        }

    }
}