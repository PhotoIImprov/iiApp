using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;  // for debug assertions.
using System.Reflection;


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
        const int LAYOUT_NOT_SET = 0;
        const int LAYOUT_PRE_CONNECT_AUTO_LOGIN = 1;
        const int LAYOUT_CONNECTED_AUTO = 2;
        const int LAYOUT_CONNECTED_ANON = 3;
        const int LAYOUT_PRE_CONNECT_FORCE_LOGIN = 4;
        const int LAYOUT_REGISTRATION = 5;
        const int LAYOUT_NEW_DEVICE = 6;
        int activeLayout = LAYOUT_NOT_SET;

        const double BACKGROUND_VERTICAL_EXTENT = 0.8;

        public static string REGISTRATION_FAILURE = "Registration failure";
        public static string BAD_PASSWORD_LOGIN_FAILURE = "Sorry, invalid username/password";
        public static string ANON_REGISTRATION_FAILURE = "Sorry, only one anonymous registration per device is supported.";

        //< loggedInLabel
        readonly Label loggedInLabel = new Label
        {
            Text = "Connecting...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            TextColor = Color.Black,
        };
        //> loggedInLabel
        Label versionLabel = new Label
        {
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            TextColor = Color.Black,
        };

        readonly Label alreadyAMemberLabel = new Label
        {
            Text = "Already a member? Login below",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
        };

        readonly Label blankRowLabel = new Label
        { // blank row in ui.
            Text = " ",
            TextColor = Color.Black,
        };

        Entry usernameEntry = new Entry { Placeholder = "email",
            Text = GlobalStatusSingleton.username,
            TextColor = Color.Black,
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Start,
            HorizontalOptions = LayoutOptions.FillAndExpand,
        };
        Entry passwordEntry = new Entry { Placeholder = "Password",
            IsPassword = true,
            TextColor = Color.Black,
            BackgroundColor = Color.White,
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

        //Grid preConnectAutoLoginLayout;
        AbsoluteLayout preConnectAutoLoginLayout = new AbsoluteLayout();
        Layout<View> autoLoginLayout =new AbsoluteLayout();
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

        // I should look into reusing background img across pages...
        const string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        Image backgroundImg = null;

        public PlayerContentPage() {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;

            // set myself up to listen for the login events...
            this.MyLogin += new MyLoginEventHandler(OnMyLogin);
            this.AnonPlay += new AnonPlayEventHandler(OnAnonPlay);
            this.RegisterNow += new RegisterNowEventHandler(OnRegisterNow);
            this.RegisterSuccess += new RegisterSuccessEventHandler(OnRegisterSuccess);
            this.LogoutClicked += new LogoutClickedEventHandler(OnLogoutClicked);
            // Note: I fire token received events, but don't consume them.

            eDummy = new EventArgs();

            setVersionLabelText();

            playerPageCenterConsole = new PlayerPageCenterConsole(this);

            connectButton = new Button
            {
                Text = "Connect now",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
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
                Text = "Play anonymously now",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
            anonymousPlayButton.Clicked += (sender, args) =>
            {
                GlobalStatusSingleton.username = GlobalStatusSingleton.UUID;
                GlobalStatusSingleton.password = getSHA224Hash(GlobalStatusSingleton.UUID);
                if (AnonPlay != null) {
                    AnonPlay(this, eDummy);
                }
            };

            createRegisterButton();
            // Object creation portion done...   Determine what ui to fire up! :)

            if (GlobalStatusSingleton.maintainLogin) {
                if (isEmailAddress(GlobalStatusSingleton.username)) {
                    Content = createPreConnectAutoLoginLayout();
                } else {
                    // anonymous user

                    // No. Not logged in at this point!
                    //Content = createAnonLoggedInLayout();
                    // don't think I should differ here!
                    Content = createPreConnectAutoLoginLayout();
                }
                // fire a loginRequestEvent.
                if (MyLogin != null) {
                    MyLogin(this, eDummy);
                }
            } else {
                // this is either the do not maintain login path, or the new user path.
                // neither of these paths fires a login event in the constructor.

                if (isEmailAddress(GlobalStatusSingleton.username)) {
                    // force relogin path.
                    Content = createForceLoginLayout();
                } else {
                    // new user/device path.
                    Content = createNewDeviceLayout();
                }
            }
        }

        /*
        double widthCheck = 0;
        double heightCheck = 0;

        protected override void OnSizeAllocated(double width, double height) {
            // need to have portrait mode local rather than global or i can't
            // tell if I've updated for this page yet or not.
            base.OnSizeAllocated(width, height);
            
            if ((widthCheck != width) || (heightCheck != height)) {
                widthCheck = width;
                heightCheck = height;
                // force a rebuild. 
                // cant just set backgroundImg to null as it is part of the layout OnSizeAllocated is iterating over
                // and we'll throw a InvalidOperationException

                // ok. this fubars, but is also what currently allows proper background. 
                // want to push to app store, so leaving in the state that doesn't crash, but has improper background
                //backgroundImg = null;
                // hmm... i can just remove the if null constraint... -apparently, not kosher either...
                if (backgroundImg == null) {
                    if (activeLayout == LAYOUT_PRE_CONNECT_AUTO_LOGIN) {
                        Content = createPreConnectAutoLoginLayout();
                    } else if (activeLayout == LAYOUT_CONNECTED_AUTO) {
                        Content = createAutoLoginLayout();
                    } else if (activeLayout == LAYOUT_CONNECTED_ANON) {
                        Content = createAnonLoggedInLayout();
                    } else if (activeLayout == LAYOUT_PRE_CONNECT_FORCE_LOGIN) {
                        Content = createForceLoginLayout();
                    } else if (activeLayout == LAYOUT_REGISTRATION) {
                        Content = createRegistrationLayout();
                    } else if (activeLayout == LAYOUT_NEW_DEVICE) {
                        Content = createNewDeviceLayout();
                    }
                }
            }
        }
        */

        protected void buildBackground(double verticalExtent = GlobalStatusSingleton.PATTERN_FULL_COVERAGE) {
            if (backgroundImg == null) {
                int w = (int)Width;
                int h = (int)Height;
                // don't switch w or h here.  we are building the correct image for the current w,h setting.
                backgroundImg = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, this.GetType().GetTypeInfo().Assembly, w, h, verticalExtent);
            }
        }

        private void setVersionLabelText() {
            // see answer at this forum page for more details on what's in full name.
            //https://forums.xamarin.com/discussion/26522/how-to-get-application-runtime-version-build-version-using-xamarin-forms
            string version = this.GetType().GetTypeInfo().Assembly.FullName;
            string[] splitString = version.Split(',');
            versionLabel.Text = splitString[1];
#if DEBUG
            versionLabel.Text = "Debug " + versionLabel.Text;
#endif
        }

        protected Layout<View> createPreConnectAutoLoginLayout() {
            activeLayout = LAYOUT_PRE_CONNECT_AUTO_LOGIN;
            logoutButton = new Button {
                Text = "Logout",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor, };
            logoutButton.Clicked += (sender, args) =>
            {
                if (LogoutClicked != null) {
                    LogoutClicked(this, eDummy);
                }
            };

            StackLayout upperPortionOfGrid = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                Children =
                {
                    loggedInLabel,
                    versionLabel,
                    //CenterConsole,
                    //new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go left for voting" },
                    //new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go right to submit photos" },
                    //maintainLoginCheckbox,
                    logoutButton,
                }
            };
            
            Grid controls = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            controls.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25, GridUnitType.Star) });
            controls.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            controls.Children.Add(upperPortionOfGrid, 0, 0);
            //autoLoginLayout.Children.Add(defaultNavigationButtons, 0, 1);  // object, col, row
            controls.BackgroundColor = GlobalStatusSingleton.backgroundColor;
            return controls;
            /*
            preConnectAutoLoginLayout.Children.Clear();
            buildBackground();
            if (backgroundImg != null) {
                preConnectAutoLoginLayout.Children.Add(backgroundImg, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            preConnectAutoLoginLayout.Children.Add(controls, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            return preConnectAutoLoginLayout;
            */
        }


        protected Layout<View> createAutoLoginLayout() {
            activeLayout = LAYOUT_CONNECTED_AUTO;
            logoutButton = new Button {
                Text = "Logout",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
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
            Grid controls = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            //autoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18, GridUnitType.Star) });
            //autoLoginLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            for (int i=0;i<10;i++) {
                controls.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            //autoLoginLayout.Children.Add(upperPortionOfGrid, 0, 0);
            controls.Children.Add(logoutButton, 0, 1);
            controls.Children.Add(loggedInLabel, 0, 2);
            controls.Children.Add(versionLabel, 0, 3);
            controls.Children.Add(CenterConsole, 0, 7);
            Grid.SetRowSpan(CenterConsole, 2);
            controls.Children.Add(defaultNavigationButtons, 0, 9);  // object, col, row

            controls.BackgroundColor = GlobalStatusSingleton.backgroundColor;
            return controls;
            /*
            ((AbsoluteLayout)autoLoginLayout).Children.Clear();
            // always assume background is for one of the others... so rebuild.
            // none of them are reachable if I get here unless I logout, where I handle this again.
            // clear backgroundImg AFTER layout, so layout doesnot have an invalid state.
            backgroundImg = null;
            buildBackground(BACKGROUND_VERTICAL_EXTENT);
            if (backgroundImg != null) {
                ((AbsoluteLayout)autoLoginLayout).Children.Add(backgroundImg, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            ((AbsoluteLayout)autoLoginLayout).Children.Add(controls, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            return autoLoginLayout;
            */

        }

        protected StackLayout usernameRow() {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    //new Label { Text = "Username", TextColor = Color.Black, BackgroundColor=Color.White, },
                    new Label { Text = "Email", TextColor = Color.Black, BackgroundColor = GlobalStatusSingleton.backgroundColor, },
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
                    new Label { Text = "Password",TextColor = Color.Black, BackgroundColor = GlobalStatusSingleton.backgroundColor, },
                    passwordEntry,
                }
            };
        }
        
        protected void createRegisterButton() {
            registerButton = new Button {
                Text = "Register",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
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
            defaultNavigationButtons = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            return defaultNavigationButtons;
        }

        protected Layout<View> createNewDeviceLayout() {
            activeLayout = LAYOUT_NEW_DEVICE;
            newDeviceLayout = new StackLayout
            {
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    versionLabel,
                    alreadyAMemberLabel,
                    usernameRow(),
                    passwordRow(),
                    connectButton,
                    blankRowLabel,
                    //anonymousPlayButton,
                    blankRowLabel,
                    registerButton,
                }
            };
            return newDeviceLayout;
            /*
            buildBackground();
            AbsoluteLayout fullLayout = new AbsoluteLayout();
            if (backgroundImg != null) {
                ((AbsoluteLayout)fullLayout).Children.Add(backgroundImg, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            ((AbsoluteLayout)fullLayout).Children.Add(newDeviceLayout, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            //return newDeviceLayout;
            return fullLayout;
            */
        }

        protected Layout<View> createForceLoginLayout() {
            activeLayout = LAYOUT_PRE_CONNECT_FORCE_LOGIN;
            if (registerButton == null) {
                createRegisterButton();
            }

            alreadyAMemberLabel.Text = "Enter password to play";
            //loggedInLabel.Text = "";
            forceLoginLayout = new StackLayout
            {
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    versionLabel,
                    alreadyAMemberLabel,
                    loggedInLabel,
                    usernameRow(),
                    passwordRow(),
                    connectButton,
                    new Label { Text = " ", TextColor = Color.Black, },
                    //anonymousPlayButton,
                    new Label { Text = " ", TextColor = Color.Black, },
                    registerButton,
                }
            };
            return forceLoginLayout;
            /*
            buildBackground();
            AbsoluteLayout fullLayout = new AbsoluteLayout();
            if (backgroundImg != null) {
                fullLayout.Children.Add(backgroundImg, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            fullLayout.Children.Add(forceLoginLayout, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            //return forceLoginLayout;
            return fullLayout;
            */
        }

        protected Layout<View> createAnonLoggedInLayout() {
            loggedInLabel.Text = "logged in anonymously";

            activeLayout = LAYOUT_CONNECTED_ANON;
            gotoRegistrationButton = new Button {
                Text = "Register me now!",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
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
                    versionLabel,
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go left for voting           ", TextColor = Color.Black, BackgroundColor=Color.White, },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go right to submit photos    ", TextColor = Color.Black, BackgroundColor=Color.White,},
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "", TextColor = Color.Black, BackgroundColor=Color.White,},
                    new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "Register for these benefits: ", TextColor = Color.Black, BackgroundColor=Color.White,},
                    new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "  Play from any device       ", TextColor = Color.Black, BackgroundColor=Color.White,},
                    new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "  Play with friends          ", TextColor = Color.Black, BackgroundColor=Color.White,},
                    new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "  Secure your account        ", TextColor = Color.Black, BackgroundColor=Color.White,},
                    new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "  Enable password recovery   ", TextColor = Color.Black, BackgroundColor=Color.White,},
                    gotoRegistrationButton,
                }
            };
            if (defaultNavigationButtons == null) {
                createDefaultNavigationButtons();
            }
            anonLoggedInLayout = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14, GridUnitType.Star) });
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Star) });
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            anonLoggedInLayout.Children.Add(upperPortionOfGrid, 0, 0);
            anonLoggedInLayout.Children.Add(CenterConsole, 0, 1);  // object, col, row
            anonLoggedInLayout.Children.Add(defaultNavigationButtons, 0, 2);  // object, col, row
            anonLoggedInLayout.BackgroundColor = GlobalStatusSingleton.backgroundColor;
            return anonLoggedInLayout;
            /*
            AbsoluteLayout fullLayout = new AbsoluteLayout();
            backgroundImg = null;
            buildBackground(BACKGROUND_VERTICAL_EXTENT);
            if (backgroundImg != null) {
                fullLayout.Children.Add(backgroundImg, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            fullLayout.Children.Add(anonLoggedInLayout, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            //return anonLoggedInLayout;
            return fullLayout;
            */
        }

        protected Layout<View> createRegistrationLayout() {
            activeLayout = LAYOUT_REGISTRATION;
            if (registerButton == null) {
                createRegisterButton();
            }

            cancelRegistrationButton = new Button {
                Text = "Never mind; I'll register later",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
            cancelRegistrationButton.Clicked += (sender, args) =>
            {
                Content = anonLoggedInLayout;
            };

            registrationLayout = new StackLayout
            {
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
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
            /*
            AbsoluteLayout fullLayout = new AbsoluteLayout();
            backgroundImg = null; // generally coming from a page with < full extent.
            buildBackground();
            if (backgroundImg != null) {
                fullLayout.Children.Add(backgroundImg, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            fullLayout.Children.Add(registrationLayout, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);

            //return registrationLayout;
            return fullLayout;
            */
        }

        // resets my ui as it may have been changed to a subpage.
        public void goHome() {
            Debug.Assert(GlobalStatusSingleton.loggedIn, "Not logged and creating a loggedin page");
            if (GlobalStatusSingleton.loggedIn == false) {
                Debug.WriteLine("DHB:PlayerContentPage:goHome fyi - not logged in");
            }

            /*
            if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                // anonymous user
                Content = createAnonLoggedInLayout();
            } else {
                Content = createAutoLoginLayout();
            }
            */
            if (isEmailAddress(GlobalStatusSingleton.username)) {
                Content = createAutoLoginLayout();
            } else {
                // anonymous user
                Content = createAnonLoggedInLayout();
            }
        }

        private void LoginSuccess() {
            if (TokenReceived != null) {
                TokenReceived(this, eDummy);
            }
            /* unreliable as uuid can change if there are save issues or an uninstall.
            if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
                // anonymous user
                Content = createAnonLoggedInLayout();
            } else {
                Content = createAutoLoginLayout();
            }
            */
            if (isEmailAddress(GlobalStatusSingleton.username)) {
                //Content = createAutoLoginLayout();
                Content = CenterConsole.LeaderboardPage;
            } else {
                // anonymous user
                if (GlobalStatusSingleton.firstTimePlaying == true) {
                    Content = CenterConsole.InstructionsPage;
                    GlobalStatusSingleton.firstTimePlaying = false;
                } else if (Content == CenterConsole.InstructionsPage) {
                    // do nothing - this means that I logged in and the token setting occurred already.
                    // it's a time issue between multiple async event handlers.
                } else { 
                    Content = createAnonLoggedInLayout();
                }
            }
            loggedInLabel.Text = "Logged in as " + GlobalStatusSingleton.username;
        }

        protected async virtual void OnMyLogin(object sender, EventArgs e) {
            //string loginResult = await requestLoginAsync();
            loggedInLabel.Text = " Connecting... ";
            loginAttemptCounter++;
            string loginResult = await requestTokenAsync();
            if ((loginResult.Equals("login failure")) || (loginResult.Equals(BAD_PASSWORD_LOGIN_FAILURE)) || (loginResult.Equals(ANON_REGISTRATION_FAILURE))) {
                loggedInLabel.Text = loginResult +"("+loginAttemptCounter+")";
                // if I was in autologin and failed (may have happened from a pwd theft, or because of a db wipe in testing), need to reset ui.
                if (isEmailAddress(GlobalStatusSingleton.username)) {
                    Content = createForceLoginLayout();
                } else {
                    // I'm in the anon case. Therefore, I only autologin.
                    // therefore, I need to repeat until success...
                    bool nologin = true;
                    while (nologin) {
                        loginResult = await requestTokenAsync();
                        if ((loginResult.Equals("login failure")) || (loginResult.Equals(BAD_PASSWORD_LOGIN_FAILURE)) || (loginResult.Equals(ANON_REGISTRATION_FAILURE))) {
                            loggedInLabel.Text = loginResult + "(" + loginAttemptCounter + ")";
                        } else {
                            nologin = false;
                        }
                    }
                    LoginSuccess();
                }
            } else {
                LoginSuccess();

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
                // make sure we goto the instructions page.
                GlobalStatusSingleton.firstTimePlaying = true;
                //Content = CenterConsole.InstructionsPage;  // something resets me to registration page, so cant be here. is it MyLogin??

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
            if (isEmailAddress(GlobalStatusSingleton.username) || (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID))) {
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
            } else {
                loggedInLabel.Text = "Please register with a valid email address";
            }
        }

        protected /* async */ virtual void OnRegisterSuccess(object sender, EventArgs e) {
            // right now, do nothing.
            // consider moving token request here...
            // now we goto the instructions page.
            this.Content = CenterConsole.InstructionsPage;
        }


        // handles the communication with the server to register an account fully.
        // @todo actually implement this function! (this is the anon registration code)
        static async Task<string> requestRegistrationAsync() {
            Debug.WriteLine("DHB:PlayerContentPage:requestRegistrationAsync start");
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
                    Debug.WriteLine("DHB:PlayerContentPage:requestRegistrationAsync success");
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
                    Debug.WriteLine("DHB:PlayerContentPage:requestRegistrationAsync failure! statuscode: " + result.StatusCode.ToString());
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
            Debug.WriteLine("DHB:PlayerContentPage:requestRegistrationAsync done");
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
            /*
            // try with a test against this regex: ^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}$
            string pattern = "^[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,6}$";
            Match m = Regex.Match(testAddress, pattern, RegexOptions.IgnoreCase);
            return m.Success;
            */
            return GlobalSingletonHelpers.isEmailAddress(testAddress);
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
            //if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
            if (!isEmailAddress(GlobalStatusSingleton.username)) {
                // can't logout in this scenario...
                // shit. this can lead to a dead case if there's incorrect account info.
                loggedInLabel.Text = "Sorry, Anonymous users can't log out.";
                Debug.WriteLine("DHB:PlayerContentPage:OnLogoutClicked in the anon user use case.");
            } else {
                // deactivate the carousel. - happens in MainPageUISwipe, who also consumes this event
                // make sure this is the active page - happens in MainPageUISwipe, who also consumes this event
                // change to the force login page
                Device.BeginInvokeOnMainThread(() => {
                    backgroundImg = null;
                    Content = createForceLoginLayout();
                });
            }
        }

        public IDictionary<CategoryJSON, IList<LeaderboardJSON>> GetLeaderboardList() {
            return CenterConsole.LeaderboardPage.GetLeaderboardList();
        }
        public IDictionary<CategoryJSON, DateTime> GetLeaderboardTimestamps() {
            return CenterConsole.LeaderboardPage.GetLeaderboardTimestamps();
        }

    }  // class

}  // namespace
