using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;  // for debug assertions.


using Newtonsoft.Json;
using Org.BouncyCastle;


using Xamarin.Forms;

namespace ImageImprov
{
    public delegate void MyLoginEventHandler(object sender, EventArgs e);
    public delegate void AnonPlayEventHandler(object sender, EventArgs e);
    public delegate void LoginSuccessEventHandler(object sender, EventArgs e);
    public delegate void RegisterNowEventHandler(object sender, EventArgs e);
    public delegate void RegisterSuccessEventHandler(object sender, EventArgs e);
    public delegate void TokenReceivedEventHandler(object sender, EventArgs e);
    public delegate void LogoutClickedEventHandler(object sender, EventArgs e);

    /*
     * @todo Store user's credentials for login on app restart
     * @todo Enable user credential input
     * @todo Build registration UI and interactions
     */
    class PlayerContentPage : ContentPage {
        public static string REGISTRATION_FAILURE = "Registration failure";
        public static string BAD_PASSWORD_LOGIN_FAILURE = "Sorry, invalid username/password";
        public static string ANON_REGISTRATION_FAILURE = "Sorry, only one anonymous registration per device is supported.";

        //< loggedInLabel
        readonly Label loggedInLabel = new Label
        {
            Text = "Connecting...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
        };
        //> loggedInLabel

        readonly Label alreadyAMemberLabel = new Label
        {
            Text = "Already a member? Login below",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
        };

        readonly Label blankRowLabel = new Label
        { // blank row in ui.
            Text = " ",
            TextColor = Color.Black,
        };

        Entry usernameEntry = new Entry { Placeholder = "email",
            Text = GlobalStatusSingleton.username,
            TextColor = Color.Black,
            HorizontalTextAlignment = TextAlignment.Start,
            HorizontalOptions = LayoutOptions.FillAndExpand,
        };
        Entry passwordEntry = new Entry { Placeholder = "Password",
            IsPassword = true,
            TextColor = Color.Black,
            HorizontalTextAlignment = TextAlignment.Start,
            HorizontalOptions = LayoutOptions.FillAndExpand,
        };

        // triggers a login event
        Button connectButton;
        // triggers anonymous registration
        Button anonymousPlayButton;

        // logs the user out
        Button logoutButton;

        Button gotoRegistrationButton;
        Button registerButton;
        Button cancelRegistrationButton;

        KeyPageNavigator defaultNavigationButtons;

        //TapGestureRecognizer tapGesture;


        // There are 3 potential UIs to display when logging in.
        // This is what is displayed on an automatic login setting.
        // This is also the default ui for registered users on successful login
        Grid preConnectAutoLoginLayout;
        Grid autoLoginLayout;
        // This is what is displayed if this is a new device.
        StackLayout newDeviceLayout;
        // This is what is displayed if the user forces login everytime.
        StackLayout forceLoginLayout;
        // Layout used for anonymously logged in users.
        // differs from autoLoginLayout by providing a registration option.
        //StackLayout anonLoggedInLayout;
        Grid anonLoggedInLayout;
        // used when an anonymous registered user decides to register.
        StackLayout registrationLayout;

        PlayerPageCenterConsole playerPageCenterConsole;
        public PlayerPageCenterConsole CenterConsole {
            get { return playerPageCenterConsole; }
        }

        public event MyLoginEventHandler MyLogin;
        public event AnonPlayEventHandler AnonPlay;

        //public event GotoRegistrationEventHandler GotoRegistration;
        public event RegisterNowEventHandler RegisterNow;
        public event RegisterSuccessEventHandler RegisterSuccess;
        public event TokenReceivedEventHandler TokenReceived;
        public event LogoutClickedEventHandler LogoutClicked;

        EventArgs eDummy = null;

        static int loginAttemptCounter = 0;

        public PlayerContentPage() {
            // set myself up to listen for the login events...
            this.MyLogin += new MyLoginEventHandler(OnMyLogin);
            this.AnonPlay += new AnonPlayEventHandler(OnAnonPlay);
            this.RegisterNow += new RegisterNowEventHandler(OnRegisterNow);
            this.RegisterSuccess += new RegisterSuccessEventHandler(OnRegisterSuccess);
            this.LogoutClicked += new LogoutClickedEventHandler(OnLogoutClicked);
            // Note: I fire token received events, but don't consume them.

            eDummy = new EventArgs();

            playerPageCenterConsole = new PlayerPageCenterConsole(this);

            connectButton = new Button
            {
                Text = "Connect now"
            };
            connectButton.Clicked += (sender, args) =>
            {
                GlobalStatusSingleton.username = usernameEntry.Text;
                GlobalStatusSingleton.password = passwordEntry.Text;
                if (MyLogin != null) {
                    MyLogin(this, eDummy);
                }
            };

            anonymousPlayButton = new Button
            {
                Text = "Play anonymously now"
            };
            anonymousPlayButton.Clicked += (sender, args) =>
            {
                GlobalStatusSingleton.username = GlobalStatusSingleton.UUID;
                GlobalStatusSingleton.password = getSHA224Hash(GlobalStatusSingleton.UUID);
                if (AnonPlay != null) {
                    AnonPlay(this, eDummy);
                }
            };

            // Object creation portion done...   Determine what ui to fire up! :)

            if (GlobalStatusSingleton.maintainLogin) {
                if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                    Content = createAnonLoggedInLayout();
                } else { 
                    Content = createPreConnectAutoLoginLayout();
                }
                // fire a loginRequestEvent.
                if (MyLogin != null) {
                    MyLogin(this, eDummy);
                }
            } else {
                // this is either the do not maintain login path, or the new user path.
                // neither of these paths fires a login event in the constructor.
                if ((GlobalStatusSingleton.username == null) || (GlobalStatusSingleton.username.Equals(""))) {
                    // new user/device path.
                    Content = createNewDeviceLayout();
                } else {
                    // force relogin path.
                    Content = createForceLoginLayout();
                }
            }
        }


        protected Layout<View> createPreConnectAutoLoginLayout() {
            logoutButton = new Button { Text = "Logout" };
            logoutButton.Clicked += (sender, args) =>
            {
                if (LogoutClicked != null) {
                    LogoutClicked(this, eDummy);
                }
            };

            StackLayout upperPortionOfGrid = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    loggedInLabel,
                    //CenterConsole,
                    //new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go left for voting" },
                    //new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go right to submit photos" },
                    //maintainLoginCheckbox,
                    logoutButton,
                }
            };
            preConnectAutoLoginLayout = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            preConnectAutoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25, GridUnitType.Star) });
            preConnectAutoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            preConnectAutoLoginLayout.Children.Add(upperPortionOfGrid, 0, 0);
            //autoLoginLayout.Children.Add(defaultNavigationButtons, 0, 1);  // object, col, row

            return preConnectAutoLoginLayout;
        }


        protected Layout<View> createAutoLoginLayout() {
            logoutButton = new Button { Text = "Logout" };
            logoutButton.Clicked += (sender, args) => {
                if (LogoutClicked != null) {
                    LogoutClicked(this, eDummy);
                }
            };


            if (defaultNavigationButtons == null) {
                createDefaultNavigationButtons();
            }

            /*
            StackLayout upperPortionOfGrid = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    loggedInLabel,
                    CenterConsole,
                    //new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go left for voting" },
                    //new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go right to submit photos" },
                    //maintainLoginCheckbox,
                    logoutButton,
                }
            };
            */
            autoLoginLayout = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            //autoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Star) });
            //autoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            for (int i=0;i<10;i++) {
                autoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            //autoLoginLayout.Children.Add(upperPortionOfGrid, 0, 0);
            autoLoginLayout.Children.Add(logoutButton, 0, 0);
            autoLoginLayout.Children.Add(loggedInLabel, 0, 1);
            autoLoginLayout.Children.Add(CenterConsole, 0, 8);
            autoLoginLayout.Children.Add(defaultNavigationButtons, 0, 9);  // object, col, row

            return autoLoginLayout;
        }

        protected StackLayout usernameRow() {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "Username", TextColor = Color.Black, },
                    usernameEntry,
                }
            };
        }

        protected StackLayout passwordRow() {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "Password",TextColor = Color.Black, },
                    passwordEntry,
                }
            };
        }
        
        protected void createRegisterButton() {
            registerButton = new Button { Text = "Register" };
            registerButton.Clicked += (sender, args) =>
            {
                GlobalStatusSingleton.username = usernameEntry.Text;
                GlobalStatusSingleton.password = passwordEntry.Text;
                // call the event handler that manages the communication with server for registration.
                if (RegisterNow != null) {
                    RegisterNow(this, eDummy);
                }
            };
        }
        protected Layout<View> createDefaultNavigationButtons() {
            defaultNavigationButtons = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
            return defaultNavigationButtons;
        }

        protected StackLayout createNewDeviceLayout() {
            newDeviceLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    alreadyAMemberLabel,
                    usernameRow(),
                    passwordRow(),
                    connectButton,
                    blankRowLabel,
                    anonymousPlayButton,
                }
            };
            return newDeviceLayout;
        }

        protected StackLayout createForceLoginLayout() {
            if (registerButton == null) {
                createRegisterButton();
            }

            alreadyAMemberLabel.Text = "Enter password to play";
            forceLoginLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    alreadyAMemberLabel,
                    loggedInLabel,
                    usernameRow(),
                    passwordRow(),
                    connectButton,
                    new Label { Text = " ", TextColor = Color.Black, },
                    anonymousPlayButton,
                    new Label { Text = " ", TextColor = Color.Black, },
                    registerButton,
                }
            };
            return forceLoginLayout;
        }

        protected Layout<View> createAnonLoggedInLayout() {
            gotoRegistrationButton = new Button { Text = "Register me now!" };
            gotoRegistrationButton.Clicked += (sender, args) =>
            {
                Content = createRegistrationLayout();
            };

            StackLayout upperPortionOfGrid = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    loggedInLabel,
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go left for voting", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go right to submit photos", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = " ", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Register for these benefits: ", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "  Play from any device", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "  Play with friends", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "  Secure your account", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "  Enable password recovery", TextColor = Color.Black, },
                    gotoRegistrationButton,
                }
            };
            if (defaultNavigationButtons == null) {
                createDefaultNavigationButtons();
            }
            anonLoggedInLayout = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Star) });
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            anonLoggedInLayout.Children.Add(upperPortionOfGrid, 0, 0);
            anonLoggedInLayout.Children.Add(defaultNavigationButtons, 0, 1);  // object, col, row
            return anonLoggedInLayout;

        }

        protected StackLayout createRegistrationLayout() {
            if (registerButton == null) {
                createRegisterButton();
            }

            cancelRegistrationButton = new Button { Text = "Never mind; I'll register later" };
            cancelRegistrationButton.Clicked += (sender, args) =>
            {
                Content = anonLoggedInLayout;
            };

            registrationLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Enter your email for your login name", TextColor = Color.Black, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "And choose your password", TextColor = Color.Black, },
                    usernameRow(),
                    passwordRow(),
                    blankRowLabel,
                    registerButton,
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "  ", TextColor = Color.Black, },
                    cancelRegistrationButton,
                }
            };
            usernameEntry.Text = "";
            return registrationLayout;
        }

        // resets my ui as it may have been changed to a subpage.
        public void goHome() {
            Debug.Assert(GlobalStatusSingleton.loggedIn, "Not logged and creating a loggedin page");
            if (GlobalStatusSingleton.loggedIn == false) {
                Debug.WriteLine("DHB:PlayerContentPage:goHome fyi - not logged in");
            }

            if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                // anonymous user
                Content = createAnonLoggedInLayout();
            } else {
                Content = createAutoLoginLayout();
            }
        }

        protected async virtual void OnMyLogin(object sender, EventArgs e) {
            //string loginResult = await requestLoginAsync();
            loginAttemptCounter++;
            string loginResult = await requestTokenAsync();
            if ((loginResult.Equals("login failure")) || (loginResult.Equals(BAD_PASSWORD_LOGIN_FAILURE)) || (loginResult.Equals(ANON_REGISTRATION_FAILURE))) {
                loggedInLabel.Text = loginResult +"("+loginAttemptCounter+")";
                // if I was in autologin and failed (may have happened from a pwd theft, or because of a db wipe in testing), need to reset ui.
                Content = createForceLoginLayout();
            } else {
                if (TokenReceived != null) {
                    TokenReceived(this, eDummy);
                }
                if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                    // anonymous user
                    Content = createAnonLoggedInLayout();
                } else {
                    Content = createAutoLoginLayout();
                }
                loggedInLabel.Text = "Logged in as " + GlobalStatusSingleton.username;

            }
        }

        //< requestLoginAsync
        // @todo bad password/account fail case
        // @todo no network connection fail case
        // I'm deserializing and instantly reserializing an object. consider fixing.
        /*
        static async Task<string> requestLoginAsync() {
            string resultMsg = "Success...";

            LoginRequestJSON loginInfo = new LoginRequestJSON();
            loginInfo.username = GlobalStatusSingleton.username;
            loginInfo.password = GlobalStatusSingleton.password;

            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                bool okToken = await requestToken(client, jsonQuery);
                if (!okToken) {
                    resultMsg = BAD_PASSWORD_LOGIN_FAILURE;
                }
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                resultMsg = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                // do something!!
                resultMsg = "login failure";
            }
            return resultMsg;
        }
        //> requestLoginAsync
        */

        protected async virtual void OnAnonPlay(object sender, EventArgs e) {
            string anonRegistrationResult = await requestAnonPlayAsync();
            if (anonRegistrationResult.Equals(ANON_REGISTRATION_FAILURE)) {
                blankRowLabel.Text = anonRegistrationResult;
                anonymousPlayButton.IsEnabled = false;
                anonymousPlayButton.IsVisible = false;
            } else if (anonRegistrationResult.Equals(REGISTRATION_FAILURE)) {
                blankRowLabel.Text = anonRegistrationResult;
            } else {
                // registration success. Send a login request message.
                if (MyLogin != null) {
                    MyLogin(this, eDummy);
                }
                // The login will handle grabbing a token.

                // login event will handle updating the ui, so no need here.
                //Content = createAnonLoggedInLayout();
                loggedInLabel.Text = "Registration success! logging in...";
            }
        }

        //< requestLoginAsync
        // @todo bad password/account fail case
        // @todo no network connection fail case
        // I'm deserializing and instantly reserializing an object. consider fixing.
        static async Task<string> requestAnonPlayAsync() {
            string resultMsg = "Success...";

            // The format is the same as the login, so reuse that object.
            RegistrationRequestJSON loginInfo = new RegistrationRequestJSON();
            loginInfo.username = GlobalStatusSingleton.username;
            loginInfo.password = getSHA224Hash(GlobalStatusSingleton.username);
            // technically this should be the same as the username... add an assert to test...
            loginInfo.guid = GlobalStatusSingleton.UUID;
#if DEBUG
            if (!(GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID))) {
                throw new Exception("Username and UUID do not match in anonymous registration.");
            }
#endif
            GlobalStatusSingleton.password = loginInfo.password;

            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "register");
                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                // string test = request.ToString();

                HttpResponseMessage result = await client.SendAsync(request);
                if (result.StatusCode == System.Net.HttpStatusCode.Created) {
                    // anon event handler will fire the login request when this returns.
                    // @todo on switch to oauth/jwt uncomment the token code (it currently is called after the login).
                    //resultMsg = await requestLoginAsync();
                    /*  a or b...
                    bool okToken = await requestToken(client, jsonQuery);
                    if (!okToken) {
                        // fix this at somepoint
                        resultMsg = BAD_PASSWORD_LOGIN_FAILURE;
                    }
                    */
                    // anon players must use autologin.
                    GlobalStatusSingleton.maintainLogin = true;
                    //resultMsg is unchanged.
                } else {
                    // login creds are no good.
                    resultMsg = ANON_REGISTRATION_FAILURE;
                }
                ////////// still todo above here..........
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine(err.ToString());
                resultMsg = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                // do something!!
                Debug.WriteLine(err.ToString());
                resultMsg = REGISTRATION_FAILURE;
            }
            return resultMsg;
        }


        protected async virtual void OnRegisterNow(object sender, EventArgs e) {
            string registrationResult = await requestRegistrationAsync();
            if (registrationResult.Equals(REGISTRATION_FAILURE)) {
                blankRowLabel.Text = registrationResult;
            } else {
                // registration success. Send a login request message.
                if (MyLogin != null) {
                    MyLogin(this, eDummy);
                }

                Content = createAutoLoginLayout();
                loggedInLabel.Text = "Registration success! logging in...";
                if (RegisterSuccess != null) {
                    RegisterSuccess(this, eDummy);
                }
            }
        }

        protected /* async */ virtual void OnRegisterSuccess(object sender, EventArgs e) {
            // right now, do nothing.
            // consider moving token request here...
        }


        // handles the communication with the server to register an account fully.
        // @todo actually implement this function! (this is the anon registration code)
        static async Task<string> requestRegistrationAsync() {
            string resultMsg = "Success...";

            RegistrationRequestJSON loginInfo = new RegistrationRequestJSON();
            loginInfo.username = GlobalStatusSingleton.username;
            loginInfo.password = GlobalStatusSingleton.password;
            loginInfo.guid = GlobalStatusSingleton.UUID;

            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "register");
                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");

                HttpResponseMessage result = await client.SendAsync(request);
                if (result.StatusCode == System.Net.HttpStatusCode.Created) {
                    // @todo on switch to oauth/jwt uncomment the token code (it currently is called after the login).
                    resultMsg = await requestTokenAsync();
                    /* a or b
                    bool okToken = await requestToken(client, jsonQuery);
                    if (!okToken) {
                        // fix this at somepoint
                        resultMsg = BAD_PASSWORD_LOGIN_FAILURE;
                    }
                    //resultMsg is unchanged.
                    */
                } else {
                    // login creds are no good.
                    resultMsg = REGISTRATION_FAILURE;
                }
                ////////// still todo above here..........
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine(err.ToString());
                resultMsg = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                // do something!!
                Debug.WriteLine(err.ToString());
                resultMsg = REGISTRATION_FAILURE;
            }
            return resultMsg;
        }

        protected static async Task<string> requestTokenAsync() {
            string result = BAD_PASSWORD_LOGIN_FAILURE;
            LoginRequestJSON loginInfo = new LoginRequestJSON();
            loginInfo.username = GlobalStatusSingleton.username;
            loginInfo.password = GlobalStatusSingleton.password;

            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, "auth");
                tokenRequest.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                HttpResponseMessage tokenResult = await client.SendAsync(tokenRequest);
                if (tokenResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    GlobalStatusSingleton.authToken
                        = JsonConvert.DeserializeObject<AuthenticationToken>
                        (await tokenResult.Content.ReadAsStringAsync());
                    GlobalStatusSingleton.loggedIn = true;
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
            return result;
        }

        // See VSProj/HashTest for a working example.
        private static string getSHA224Hash(string key) {
            string hashedText = "";
            byte[] theBytes = System.Text.Encoding.UTF8.GetBytes(key);

            Org.BouncyCastle.Crypto.Digests.Sha224Digest digester = new Org.BouncyCastle.Crypto.Digests.Sha224Digest();
            byte[] retValue = new byte[digester.GetDigestSize()];
            digester.BlockUpdate(theBytes, 0, theBytes.Length);
            digester.DoFinal(retValue, 0);

            foreach (byte b in retValue) {
                // converts the byte into a 2 digit hex value.
                hashedText += b.ToString("X2").ToLower();
            }

            return hashedText;
        }

        /// <summary>
        /// Given an string determines whether it is a validly formatted email address.
        /// </summary>
        /// <param name="testAddress">The string to test against</param>
        /// <returns>True for an email address, false otherwise</returns>
        private static bool isEmailAddress(string testAddress) {
            // try with a test against this regex: ^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}$
            return true;
        }

        /* lives and works in teh KeyPageNavigator
        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == gotoVotingImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            } else if (sender == gotoCameraImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
            } // else ignore goHomeImgButton
        }
        */

        protected virtual void OnLogoutClicked(object sender, EventArgs e) {
            // Anonymous users can't logout...
            if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                // can't logout in this scenario...
                loggedInLabel.Text = "Sorry, Anonymous users can't log out.";
            } else {
                // deactivate the carousel. - happens in MainPageUISwipe, who also consumes this event
                // make sure this is the active page - happens in MainPageUISwipe, who also consumes this event
                // change to the force login page
                Device.BeginInvokeOnMainThread(() => {
                    Content = createForceLoginLayout();
                });
            }
        }
    }  // class

}  // namespace
