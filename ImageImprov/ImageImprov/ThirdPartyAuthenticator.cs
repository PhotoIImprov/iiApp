using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;  // for debug assertions.

using Xamarin.Forms;
using Xamarin.Auth;
using Newtonsoft.Json;

namespace ImageImprov {
    /// <summary>
    /// This may change to be just Google, FBook, or other.
    /// For now, I'm seeing if I can fire up a generic one and just set params.
    /// </summary>
    public class ThirdPartyAuthenticator { //: ContentView {
        private const string OAUTH_API_CALL = "auth";

        private LoginPage parent = null;

        public static OAuth2Authenticator authenticator;

        private string clientId = "";
        private string clientSecret = "";
        private Uri authorizeUrl = null;
        private Uri accessTokenUrl = null;
        private Uri redirectUrl = null;
        private string scope = "";
        private string method = "";

        private const string METHOD_FACEBOOK = "Facebook";
        private const string METHOD_GOOGLE = "Google";


        //private string accessToken = "";
        // would i be better off making the class a singleton?
        //public static string refreshToken = "";
        public static Account authAccount;
        public static OAUTHDataJSON oauthData = new OAUTHDataJSON();

        private Uri googleAuthorizeUrl = new Uri("https://accounts.google.com/o/oauth2/auth");
        private Uri googleAccessTokenUrl = new Uri("https://www.googleapis.com/oauth2/v4/token");
        string scope_google = "https://www.googleapis.com/auth/games https://www.googleapis.com/auth/plus.login https://www.googleapis.com/auth/plus.me "  // leave the space here!
            + "https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email";

        //private Uri facebookAuthorizeUrl = new Uri("https://www.facebook.com/connect/login_success.html");
        private Uri facebookAuthorizeUrl = new Uri("https://m.facebook.com/dialog/oauth/");
        private Uri facebookAccessTokenUrl = new Uri("https://graph.facebook.com/oauth/access_token");
        //private Uri facebookRedirectUrl = new Uri(GlobalStatusSingleton.activeURL);
        private Uri facebookRedirectUrl = new Uri("https://www.imageimprov.com/static/ii_logo.png");
        //https://developers.facebook.com/docs/facebook-login/permissions/
        private string scope_facebook = "public_profile,user_friends,email";
        //private string scope_facebook = "email";


        public static string FACEBOOK_APP_ID = "1475439292494090";
        public static string FACEBOOK_SECRET = "98b1d958d200717f6b8f69a170309bf6";
        public static string GOOGLE_ANDROID_ID = "198313221875-k1je9v2ccjvnfk4ae8v8pdmcras8isua.apps.googleusercontent.com";
        public static Uri GOOGLE_ANDROID_REDIRECT = new Uri("com.googleusercontent.apps.198313221875-k1je9v2ccjvnfk4ae8v8pdmcras8isua:/ii_oauth2redirect");
        public static string GOOGLE_IOS_ID = "198313221875-3999a4ev376dplugmig89thjffq34ai8.apps.googleusercontent.com";
        public static Uri GOOGLE_IOS_REDIRECT = new Uri("com.googleusercontent.apps.198313221875-3999a4ev376dplugmig89thjffq34ai8:/ii_oauth2redirect");

        INavigation navigation;

        public ThirdPartyAuthenticator(LoginPage parent) {
            this.parent = parent;
        }

        /// <summary>
        /// Sets config off Method string.
        /// </summary>
        /// <param name="inMethod"></param>
        /// <returns>1 on success. -1 on method not found. </returns>
        public int configForMethod(string inMethod) {
            int result = 1;
            if (inMethod.Equals(METHOD_GOOGLE)) {
                configForGoogle();
            } else if (inMethod.Equals(METHOD_FACEBOOK)) {
                configForFacebook();
            } else {
                result = -1;
            }
            return result;
        }

        public void configForGoogle() {
            Device.OnPlatform(Android: () => clientId = GOOGLE_ANDROID_ID,
                iOS: () => clientId = GOOGLE_IOS_ID);

            authorizeUrl = googleAuthorizeUrl;
            accessTokenUrl = googleAccessTokenUrl;
            Device.OnPlatform(Android: () => redirectUrl = GOOGLE_ANDROID_REDIRECT,
                iOS: () => redirectUrl = GOOGLE_IOS_REDIRECT);

            scope = scope_google;
            method = METHOD_GOOGLE;

            // trick the linker into having this...
            Xamarin.Auth.Account x = new Xamarin.Auth.Account();
        }

        public void configForFacebook() {
            clientId = FACEBOOK_APP_ID;

            authorizeUrl = facebookAuthorizeUrl;
            accessTokenUrl = facebookAccessTokenUrl;
            redirectUrl = facebookRedirectUrl;
            scope = scope_facebook;
            clientSecret = FACEBOOK_SECRET;
            method = METHOD_FACEBOOK;
        }

        /// <summary>
        /// Authentication in this instance refers to the third party authenticator, not our service.
        /// </summary>
        /// <param name="navigation"></param>
        public void startAuthentication(INavigation navigation) {
            Debug.WriteLine("DHB:ThirdPartyAuthentiator:startAuthentication start");
            this.navigation = navigation;

            if (authorizeUrl == null) {
                // default to facebook as google doesn't work on all platforms.
                configForFacebook();
            }
            
            /*
            if (Device.OS == TargetPlatform.iOS) {
                // clientId; clientSecret; permissionsRequest; authorizeUrl; redirectUrl; accessTokenUrl; username; isNativeUI
                authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, false);
            } else if (Device.OS == TargetPlatform.Android) {
                authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, false);
            } else {
                return;  // no oauth available.
            }
            */
            if (method == METHOD_FACEBOOK) {
                authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, false);
                //authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, true);
            } else if (method == METHOD_GOOGLE) {
                authenticator = new OAuth2Authenticator(clientId, clientSecret, scope, authorizeUrl, redirectUrl, accessTokenUrl, null, true);
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
            Debug.WriteLine("DHB:ThirdPartyAuthentiator:startAuthentication end");
        }

        /// <summary>
        /// Gets a new access_token from the service with the refresh_token.
        /// I assume expiration has been checked for BEFORE entering this function!
        /// </summary>
        public async void refreshAuthentication(INavigation navigation) {
            Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication");
            if (authorizeUrl == null) {
                if ((oauthData.method == null) || (oauthData.method.Equals(""))) {
                    parent.handleLoginFail("Invalid state. Please relogin.");
                    return;
                } else {
                    if (oauthData.method.Equals(METHOD_FACEBOOK)) {
                        configForFacebook();
                    } else if (oauthData.method.Equals(METHOD_GOOGLE)) {
                        configForGoogle();
                    }
                }
            }
            bool loginSuccess = false;

            if (method.Equals(METHOD_FACEBOOK)) {
                // facebook uses long expiry access_tokens
                // Expiration was already checked for.
                // access_token is correct. move on to retrieving a jwt token.
                //parent.LoginSuccess();
                loginSuccess = true;
            } else {
                //authenticator.Completed += OnRefreshCompleted;
                //authenticator.Error += OnRefreshError;

                var postDictionary = new Dictionary<string, string>();
                postDictionary.Add("refresh_token", oauthData.refreshToken);
                postDictionary.Add("client_id", clientId);
                postDictionary.Add("client_secret", clientSecret);
                postDictionary.Add("grant_type", "refresh_token");

                var refreshRequest = new OAuth2Request("POST", accessTokenUrl, postDictionary, authAccount);
                await refreshRequest.GetResponseAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted) {
                        // error msg here.
                        Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication error .IsFaulted case.");
                        Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication accessUrl: " + accessTokenUrl);
                        Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication clientId: " + clientId);
                        Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication clientSecret: " + clientSecret);
                        if (authAccount != null) {
                            Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication authAccount:" + authAccount.ToString());
                            Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication authAccount via json:" + JsonConvert.SerializeObject(ThirdPartyAuthenticator.authAccount));
                        }
                    } else {
                        string json = task.Result.GetResponseText();
                        Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication response text:");
                        Debug.WriteLine(json);
                        try {
                            // need to read up on this more.
                            // what if the refresh token has expired?
                            OAuthRefreshResponseJSON newTokens = JsonConvert.DeserializeObject<OAuthRefreshResponseJSON>(json);
                            oauthData.accessToken = newTokens.accessToken;
                            oauthData.refreshToken = newTokens.refreshToken;
                            oauthData.method = method;

                            loginSuccess = true;
                            //parent.LoginSuccess();
                        } catch (Exception e) {
                            Debug.WriteLine("DHB:ThirdPartyAuthenticator:refreshAuthentication exception: " + e.ToString());
                        }
                    }
                });
            }
            if (!loginSuccess) {
                Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication loginFailed case");
                parent.handleLoginFail("Please reconnect to " + method);
            } else {
                Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication login success. grabbing token");
                Debug.WriteLine("DHB:ThirdPartyAuthentiator:refreshAuthentication method=" + method);
                try {
                    string result = await requestTokenAsync(method, oauthData.accessToken);
                    if (result.Equals("Success")) {
                        // request token takes care of housekeeping issues, like setting the email address. goto login success steps.
                        parent.LoginSuccess();
                    }
                } catch (TaskCanceledException e) {
                    parent.loggedInLabel.Text = "Unable to use existing token. Please relogin.";
                    this.navigation = navigation;
                    configForMethod(method);
                    startAuthentication(this.navigation);
                }
            }            
        }

        /// <summary>
        /// This is where we start interacting with our service for authentication in the first access case (vs refresh case).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public async void OnAuthCompleted(object sender, EventArgs args) {
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted");

            if (((AuthenticatorCompletedEventArgs)args).Account == null) {
                Device.OnPlatform(Android: () => { navigation.PopModalAsync(); });  // check and see if this line is actually needed... yes it is.
                Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted User backed out of oauth. Exit and return to regular login ui.");
                return;
            }

            // this is the success case.
            parent.Content = parent.createPreConnectAutoLoginLayout();
            Device.OnPlatform(Android: () => { navigation.PopModalAsync(); });  // check and see if this line is actually needed... yes it is.
            //await navigation.PopModalAsync();

            // and now returning to our regularly scheduled programming
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted Successful login!");
            if (oauthData == null) {
                oauthData = new OAUTHDataJSON();
            }
            oauthData.accessToken = ((AuthenticatorCompletedEventArgs)args).Account.Properties["access_token"];
            Debug.WriteLine("AccessToken:"+ oauthData.accessToken.ToString());
            Debug.WriteLine("Auth Account:" + ((AuthenticatorCompletedEventArgs)args).Account.ToString());
            try {
                if (((AuthenticatorCompletedEventArgs)args).Account.Properties.ContainsKey("refresh_token")) {
                    oauthData.refreshToken = ((AuthenticatorCompletedEventArgs)args).Account.Properties["refresh_token"];
                    Debug.WriteLine("RefreshToken:" + oauthData.refreshToken);
                } else {
                    if (method.Equals(METHOD_FACEBOOK)) {
                        // fbook uses a really long-lived access token...
                        oauthData.refreshToken = oauthData.accessToken;
                    }
                }
                if (((AuthenticatorCompletedEventArgs)args).Account.Properties.ContainsKey("expires_in")) {
                    //
                    double secsTillExpiration = Convert.ToDouble(((AuthenticatorCompletedEventArgs)args).Account.Properties["expires_in"]);
                    oauthData.expiration = DateTime.Now.AddSeconds(secsTillExpiration);
                    Debug.WriteLine("Expiration:" + oauthData.expiration);
                }
                oauthData.method = method;

                authAccount = ((AuthenticatorCompletedEventArgs)args).Account;
                // we have refresh tokens and auth servers get snippy about relogins... make sure maintain login is true.
                GlobalStatusSingleton.maintainLogin = true;
                Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted have auth; going for token");
                string result = await requestTokenAsync(method, oauthData.accessToken);
                if (result.Equals("Success")) {
                    // request token takes care of housekeeping issues, like setting the email address. goto login success steps.
                    parent.LoginSuccess();
                }
            } catch (Exception e) {
                Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted EXCEPTION!:" + e.ToString());
            }
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthCompleted done");
        }

        /// <summary>
        /// This is where we get a JWT token for image improv services.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected static async Task<string> requestTokenAsync(string method, string token) {
            string result = "Perfect";
            //OAUTHTokenRequestJSON loginInfo = new OAUTHTokenRequestJSON();
            //loginInfo.method = method;
            //loginInfo.token = token;
            LoginRequestJSON loginInfo = new LoginRequestJSON();
            loginInfo.username = method;
            loginInfo.password = token;

            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                Debug.WriteLine("DHB:ThirdPartyAuthenticator:requestTokenAsync json:" + jsonQuery);
                HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, OAUTH_API_CALL);
                tokenRequest.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                HttpResponseMessage tokenResult = await client.SendAsync(tokenRequest);
                if (tokenResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    GlobalStatusSingleton.authToken
                        = JsonConvert.DeserializeObject<AuthenticationToken>
                        (await tokenResult.Content.ReadAsStringAsync());
                    GlobalStatusSingleton.loggedIn = true;
                    GlobalStatusSingleton.username = GlobalStatusSingleton.authToken.email;
                    result = "Success";
                }
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine(err.ToString());
                result = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                // do something!!
                Debug.WriteLine(err.ToString());
                result = "login failure";
            }
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:requestTokenAsync done");
            return result;
        }

        public void OnAuthError(object sender, EventArgs args) {
            // do something with the error condition.
            Debug.WriteLine("DHB:ThirdPartyAuthenticator:OnAuthError login fail");
            //AuthenticatorCompletedEventArgs.
            parent.loggedInLabel.Text = method + " login failed. Please try again.";
            // should just return to login page.

            // sometimes I get a fail and a successful login.
            // if that occurs, the success process will just carry me forward.
        }
    }
}
