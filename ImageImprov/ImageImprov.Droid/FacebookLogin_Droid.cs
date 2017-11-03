using Android.OS;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Facebook;
using Xamarin.Facebook.Login;
using Xamarin.Facebook.Login.Widget;
using Android.Content.PM;
using Java.Security;
using Android.App;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using System.Diagnostics;
using Java.Lang;
using System;
using System.Threading.Tasks;
using Android.Content;

[assembly: Dependency(typeof(ImageImprov.Droid.FacebookLogin_Droid))]
namespace ImageImprov.Droid {
    
    public class FacebookLogin_Droid : ImageImprov.I_ii_FacebookLogin, IFacebookCallback {
        //0pS9olwwj0AJ1yqChAck3t/NZf0=
        private ICallbackManager mCallbackManager;
        // There's something about dependency services that creates multiple instances of this object.
        // Make the callback a static so I'm guaranteed to have it.
        private static LoginPage loginCallback;

        public void Init() {
            mCallbackManager = CallbackManagerFactory.Create();
            LoginManager.Instance.RegisterCallback(mCallbackManager, this);
            //printKey();
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
            LoginManager.Instance.LogInWithReadPermissions((Activity)Forms.Context, new List<string> { "public_profile", "user_friends", "email" });
        }

        public bool loginCheck() {
            bool loggedIn = false;
            if ((AccessToken.CurrentAccessToken != null) && (Profile.CurrentProfile != null)) {
                System.Diagnostics.Debug.WriteLine("DHB: LoggedIn!");
                loggedIn = true;
            }
            return loggedIn;
        }

        public void logout() {
            LoginManager.Instance.LogOut();
        }
        public void OnCancel() {
            //throw new NotImplementedException();
        }

        public void OnError(FacebookException p0) {
            //throw new NotImplementedException();
        }

        public void OnSuccess(Java.Lang.Object result) {
            // The user approved. Moving on!
            Xamarin.Facebook.Login.LoginResult loginResult = result as Xamarin.Facebook.Login.LoginResult;
            System.Diagnostics.Debug.WriteLine("UserId: "+loginResult.AccessToken.UserId);
        }

        /* How does this integrate??? */
        public async void OnActivityResult(int requestCode, Result resultCode, Intent data) {
            //base.OnActivityResult(requestCode, resultCode, data);
            mCallbackManager.OnActivityResult(requestCode, (int)resultCode, data);
            if (loginCheck()) {
                loginCallback.Content = loginCallback.createPreConnectAutoLoginLayout();
                // grab token and go!
                GlobalStatusSingleton.maintainLogin = true;
                string result = await ThirdPartyAuthenticator.requestTokenAsync(ThirdPartyAuthenticator.METHOD_FACEBOOK, AccessToken.CurrentAccessToken.Token);
                if (result.Equals("Success")) {
                    GlobalStatusSingleton.facebookRefreshToken = AccessToken.CurrentAccessToken.Token;
                    // request token takes care of housekeeping issues, like setting the email address. goto login success steps.
                    loginCallback.LoginSuccess();
                }

            }
        }
        
        public async Task<string> relogin() {
            string result = await ThirdPartyAuthenticator.requestTokenAsync(ThirdPartyAuthenticator.METHOD_FACEBOOK, GlobalStatusSingleton.facebookRefreshToken);
            return result;
        }

        public IntPtr Handle { get;}
        
        public void Dispose() {
            //throw new NotImplementedException();
        }
    }
}

