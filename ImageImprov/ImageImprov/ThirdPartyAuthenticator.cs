using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Xamarin.Forms;
using Xamarin.Auth;

namespace ImageImprov {
    /// <summary>
    /// This may change to be just Google, FBook, or other.
    /// For now, I'm seeing if I can fire up a generic one and just set params.
    /// </summary>
    public class ThirdPartyAuthenticator { //: ContentView {
        public static OAuth2Authenticator authenticator;

        private string clientId = "";
        private string clientSecret = null;
        private Uri authorizeUrl = null;
        private Uri accessTokenUrl = null;
        private Uri redirectUrl = null;
        private string scope = "";

        private Uri googleAuthorizeUrl = new Uri("https://accounts.google.com/o/oauth2/auth");
        private Uri googleAccessTokenUrl = new Uri("https://www.googleapis.com/oauth2/v4/token");
        string scope_google = "https://www.googleapis.com/auth/games https://www.googleapis.com/auth/plus.login https://www.googleapis.com/auth/plus.me "  // leave the space here!
            + "https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";

        private Uri facebookAuthorizeUrl = new Uri("https://m.facebook.com/dialog/oauth/");
        private Uri facebookAccessTokenUrl = new Uri("https://graph.facebook.com/oauth/access_token");
        private Uri facebookRedirectUrl = new Uri(GlobalStatusSingleton.activeURL);
        //https://developers.facebook.com/docs/facebook-login/permissions/
        private string scope_facebook = "public_profile user_friends email";


        public static string FACEBOOK_APP_ID = "1475439292494090";
        public static string FACEBOOK_SECRET = "98b1d958d200717f6b8f69a170309bf6";
        public static string GOOGLE_ANDROID_ID = "198313221875-k1je9v2ccjvnfk4ae8v8pdmcras8isua.apps.googleusercontent.com";
        public static Uri GOOGLE_ANDROID_REDIRECT = new Uri("com.googleusercontent.apps.198313221875-k1je9v2ccjvnfk4ae8v8pdmcras8isua:/ii_oauth2redirect");
        public static string GOOGLE_IOS_ID = "198313221875-3999a4ev376dplugmig89thjffq34ai8.apps.googleusercontent.com";
        public static Uri GOOGLE_IOS_REDIRECT = new Uri("com.googleusercontent.apps.198313221875-3999a4ev376dplugmig89thjffq34ai8:/ii_oauth2redirect");

        INavigation navigation;

        public ThirdPartyAuthenticator() {
        }

        public void configForGoogle() {
            Device.OnPlatform(Android: () => clientId = GOOGLE_ANDROID_ID,
                iOS: () => clientId = GOOGLE_IOS_ID);

            authorizeUrl = googleAuthorizeUrl;
            accessTokenUrl = googleAccessTokenUrl;
            Device.OnPlatform(Android: () => redirectUrl = GOOGLE_ANDROID_REDIRECT,
                iOS: () => redirectUrl = GOOGLE_IOS_REDIRECT);

            scope = scope_google;
        }

        public void configForFacebook() {
            clientId = FACEBOOK_APP_ID;

            authorizeUrl = facebookAuthorizeUrl;
            accessTokenUrl = facebookAccessTokenUrl;
            redirectUrl = facebookRedirectUrl;
            scope = scope_facebook;
            clientSecret = FACEBOOK_SECRET;
        }

        public void startAuthentication(INavigation navigation) {
            this.navigation = navigation;

            if (authorizeUrl == null) {
                // default to google
                configForGoogle();
            }
            // currently just configured for google.  alter once that works.

            
            if (Device.OS == TargetPlatform.iOS) {
                authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, true);
            } else if (Device.OS == TargetPlatform.Android) {
                authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, false);
            } else {
                return;  // no oauth available.
            }
            
            // hook up listeners for the auth events.
            authenticator.Completed += OnAuthCompleted;
            authenticator.Error += OnAuthError;
            
            Device.OnPlatform(iOS: () =>
                {
                    // init the ui
                    PlatformSpecificCalls.authInit();
                    // and launch...
                    var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();
                    presenter.Login(authenticator);
                },
                Android: () =>
                {
                    navigation.PushModalAsync(new OAuthLoginPage());
                    //PlatformSpecificCalls.authInit();
                }
            );
        }

        public void OnAuthCompleted(object sender, EventArgs args) {
            navigation.PopModalAsync();
            // and now i... return to my regularly scheduled programming
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted Successful login!");
            var token = ((AuthenticatorCompletedEventArgs)args).Account.Properties["access_token"];
            Debug.WriteLine(token.ToString());
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
            Debug.WriteLine("Google login done!");
        }

        public void OnAuthError(object sender, EventArgs args) {
            // do something with the error condition.
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted login fail");
            //AuthenticatorCompletedEventArgs.
        }
    }
}
