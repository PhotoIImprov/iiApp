using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Newtonsoft.Json;

namespace ImageImprov {
    public delegate void MyLoginEventHandler(object sender, EventArgs e);
    public delegate void AnonPlayEventHandler(object sender, EventArgs e);
    public delegate void LoginSuccessEventHandler(object sender, EventArgs e);
    public delegate void RegisterNowEventHandler(object sender, EventArgs e);
    public delegate void RegisterSuccessEventHandler(object sender, EventArgs e);
    public delegate void TokenReceivedEventHandler(object sender, EventArgs e);
    public delegate void LogoutClickedEventHandler(object sender, EventArgs e);

    /// <summary>
    /// This is the starting page of the app and controls entry to everything else.
    /// Refactored version.
    /// NOTE: Anonymous play was not tested for as part of this refactoring.  That will have to be handled.
    ///    Specifically, in the old schema, this page became a loggedin home page, rather than 
    /// </summary>
    public class LoginPage : ContentView {
        const int LAYOUT_NOT_SET = 0;
        const int LAYOUT_PRE_CONNECT_AUTO_LOGIN = 1;
        const int LAYOUT_CONNECTED_AUTO = 2;
        const int LAYOUT_CONNECTED_ANON = 3;
        const int LAYOUT_PRE_CONNECT_FORCE_LOGIN = 4;
        const int LAYOUT_REGISTRATION = 5;
        const int LAYOUT_NEW_DEVICE = 6;
        int activeLayout = LAYOUT_NOT_SET;

        const string AUTH = "auth";
        const string BASE = "base";
        const string FORGOT_PWD = "forgotpwd";

        const double BACKGROUND_VERTICAL_EXTENT = 0.8;

        public static string REGISTRATION_FAILURE = "Registration failure";
        public static string BAD_PASSWORD_LOGIN_FAILURE = "Sorry, invalid username/password";
        public static string ANON_REGISTRATION_FAILURE = "Sorry, only one anonymous registration per device is supported.";

        //< loggedInLabel
        public Label loggedInLabel = new Label {
            Text = "",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.SplashBackgroundColor,
            TextColor = Color.White,
            //IsVisible = false,
        };
        //> loggedInLabel
        Label versionLabel = new Label {
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.SplashBackgroundColor,
            TextColor = Color.White,
        };

        readonly Label alreadyAMemberLabel = new Label {
            Text = "Already a member? Login below",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            TextColor = Color.Black,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
        };

        readonly Label blankRowLabel = new Label { // blank row in ui.
            Text = " ",
            TextColor = Color.Black,
        };

        Entry usernameEntry = new Entry
        {
            Placeholder = "email",
            PlaceholderColor = Color.Gray,
            Text = GlobalStatusSingleton.username,
            TextColor = Color.Black,
            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = 1,
        };
        Entry passwordEntry = new Entry
        {
            Placeholder = "Password",
            PlaceholderColor = Color.Gray,
            IsPassword = true,
            TextColor = Color.Black,
            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = 1,
        };

        //iiBitmapView signupBackground;
        iiBitmapView forgotButton;
        iiBitmapView backButton;

        // triggers a login event
        //Button connectButton;
        iiBitmapView connectButton;
        string connectButtonImgStr = "ImageImprov.IconImages.signIn_inactive.png";

        // triggers anonymous registration
        Button anonymousPlayButton;

        /// <summary>
        /// Button that triggers the facebook oauth login process.
        /// </summary>
        Image facebookLogin;
        /// <summary>
        /// Button that triggers the google oauth login process.
        /// </summary>
        Image googleLogin;

        // logs the user out
        Button logoutButton;

        Button gotoRegistrationButton;
        Button registerButton;
        Button cancelRegistrationButton;

        Label termsOfServiceLabel;
        iiWebPage tosPage;
        Label privacyPolicyLabel;
        iiWebPage privacyPolicyPage;

        // There are 3 potential UIs to display when logging in.
        //   AutoLogin, newDevice, forceLogin
        // This is what is displayed on an automatic login setting.
        // This is also the default ui for registered users on successful login
        AbsoluteLayout preConnectAutoLoginLayout = new AbsoluteLayout();
        Layout<View> autoLoginLayout = new AbsoluteLayout();
        // This is what is displayed if this is a new device.
        StackLayout newDeviceLayout;
        // This is what is displayed if the user forces login everytime.
        StackLayout forceLoginLayout;

        // Layout used for anonymously logged in users.
        // differs from autoLoginLayout by providing a registration option.
        Grid anonLoggedInLayout;
        // used when an anonymous registered user decides to register.
        StackLayout registrationLayout;

        public event MyLoginEventHandler MyLogin;
        public event AnonPlayEventHandler AnonPlay;

        //public event GotoRegistrationEventHandler GotoRegistration;
        public event RegisterNowEventHandler RegisterNow;
        public event RegisterSuccessEventHandler RegisterSuccess;
        public event TokenReceivedEventHandler TokenReceived;
        public event LogoutClickedEventHandler LogoutClicked;

        public EventHandler ForgotClicked;

        EventArgs eDummy = null;

        static int loginAttemptCounter = 0;

        AbsoluteLayout layoutP;  // this lets us place a background image on the screen.
        Assembly assembly = null;
        iiBitmapView backgroundImgP = null;
        string backgroundFilename = "ImageImprov.IconImages.signin_background.png";


        public LoginPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;

            // set myself up to listen for the login events...
            this.MyLogin += new MyLoginEventHandler(OnMyLogin);
            this.AnonPlay += new AnonPlayEventHandler(OnAnonPlay);
            this.RegisterNow += new RegisterNowEventHandler(OnRegisterNow);
            this.RegisterSuccess += new RegisterSuccessEventHandler(OnRegisterSuccess);
            this.LogoutClicked += new LogoutClickedEventHandler(OnLogoutClicked);
            ForgotClicked += new EventHandler(OnForgotPwdClicked);
            // Note: I fire token received events, but don't consume them.

            eDummy = new EventArgs();

            setVersionLabelText();

            connectButton = new iiBitmapView {
                Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(connectButtonImgStr, assembly),
                EnsureSquare = false,
            };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += async (sender, args) => {
                await connectButton.FadeTo(0, 175);
                await connectButton.FadeTo(1, 175);
                GlobalStatusSingleton.username = usernameEntry.Text;
                GlobalStatusSingleton.password = passwordEntry.Text;
                // clear oauth data so we force(ensure) a regular password login.
                ThirdPartyAuthenticator.oauthData = null;
                if (MyLogin != null) {
                    MyLogin(this, eDummy);
                }
            };
            connectButton.GestureRecognizers.Add(tap);

            anonymousPlayButton = new Button {
                Text = "Play anonymously now",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
            anonymousPlayButton.Clicked += (sender, args) => {
                GlobalStatusSingleton.username = GlobalStatusSingleton.UUID;
                GlobalStatusSingleton.password = getSHA224Hash(GlobalStatusSingleton.UUID);
                if (AnonPlay != null) {
                    AnonPlay(this, eDummy);
                }
            };

            createRegisterButton();
            createFacebookButton();
            createGoogleButton();
            oauthManager = new ThirdPartyAuthenticator(this);

            // Object creation portion done...   Determine what ui to fire up! :)
            if (GlobalStatusSingleton.maintainLogin) {
                // If I'm setup to auto relogin, show that page.
                Content = createPreConnectAutoLoginLayout();
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

        // public so it is exposed to ThirdPartyAuthenticator
        public Layout<View> createPreConnectAutoLoginLayout() {
            activeLayout = LAYOUT_PRE_CONNECT_AUTO_LOGIN;
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

            Grid controls = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            //controls.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25, GridUnitType.Star) });
            for (int i = 0; i < 10; i++) {
                controls.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            if (backgroundImgP == null) {
                backgroundImgP = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(backgroundFilename, assembly),
                    EnsureSquare = false,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                };
            }

            Debug.WriteLine("DHB:LoginPage:createPreConnectAutoLoginLayout");
            controls.BackgroundColor = GlobalStatusSingleton.SplashBackgroundColor;
            controls.Children.Add(backgroundImgP, 0, 0);
            Grid.SetRowSpan(backgroundImgP, 10);
            controls.Children.Add(loggedInLabel, 0, 4);
            controls.Children.Add(versionLabel, 0, 6);
            return controls;
        }

        // @todo Refactoring. Not sure how this fits in now?
        protected Layout<View> createAutoLoginLayout() {
            activeLayout = LAYOUT_CONNECTED_AUTO;
            /*
            logoutButton = new Button
            {
                Text = "Logout",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
            logoutButton.Clicked += (sender, args) => {
                if (LogoutClicked != null) {
                    LogoutClicked(this, eDummy);
                }
            };
            */
            Grid controls = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            for (int i = 0; i < 10; i++) {
                controls.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            if (backgroundImgP == null) {
                backgroundImgP = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(backgroundFilename, assembly),
                    EnsureSquare = false,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                };
            }

            controls.Children.Add(backgroundImgP, 0, 0);
            Grid.SetRowSpan(backgroundImgP, 10);
            Debug.WriteLine("DHB:LoginPage:createAutoLoginLayout");
            //controls.Children.Add(logoutButton, 0, 1);
            controls.Children.Add(loggedInLabel, 0, 4);
            controls.Children.Add(versionLabel, 0, 5);

            controls.BackgroundColor = GlobalStatusSingleton.backgroundColor;
            return controls;
        }

        protected Layout<View> createNewDeviceLayout() {
            return createCommonLayout();
            /*
            Debug.WriteLine("DHB:LoginPage:createNewDevideLayout");
            activeLayout = LAYOUT_NEW_DEVICE;

            if (registerButton == null) {
                createRegisterButton();
            }
            if (termsOfServiceLabel == null) {
                createWebButtons();
            }

            newDeviceLayout = new StackLayout {
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    versionLabel,
                    alreadyAMemberLabel,
                    usernameRow(),
                    passwordRow(),
                    connectButton,
                    facebookLogin,
                    //googleLogin,
                    blankRowLabel,
                    //anonymousPlayButton,
                    new Label { Text = "Enter an email, password and click register to register", HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.Black },
                    registerButton,
                    new Label { Text = " ", TextColor = Color.Black, },
                    new Label { Text = " ", TextColor = Color.Black, },
                    termsOfServiceLabel,
                    privacyPolicyLabel,
                }
            };
            // @todo fix on android so this goes away!
            if (Device.OS == TargetPlatform.iOS) {
                newDeviceLayout.Children.Insert(7, googleLogin);
            }
            return newDeviceLayout;
            */
        }

        protected Layout<View> createForceLoginLayout() {
            activeLayout = LAYOUT_PRE_CONNECT_FORCE_LOGIN;
            return createCommonLayout();
            /*
            if (registerButton == null) {
                createRegisterButton();
            }
            if (termsOfServiceLabel == null) {
                createWebButtons();
            }

            alreadyAMemberLabel.Text = "Enter password to play";
            //loggedInLabel.Text = "";
            forceLoginLayout = new StackLayout {
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    versionLabel,
                    alreadyAMemberLabel,
                    loggedInLabel,
                    usernameRow(),
                    passwordRow(),
                    connectButton,
                    facebookLogin,
                    //googleLogin,
                    new Label { Text = " ", TextColor = Color.Black, },
                    //anonymousPlayButton,
                    new Label { Text = "Enter an email, password and click register to register" },
                    registerButton,
                    new Label { Text = " ", TextColor = Color.Black, },
                    new Label { Text = " ", TextColor = Color.Black, },
                    termsOfServiceLabel,
                    privacyPolicyLabel,
                }
            };
            // @todo fix on android so this goes away!
            if (Device.OS == TargetPlatform.iOS) {
                forceLoginLayout.Children.Insert(7, googleLogin);
            }
            
            return forceLoginLayout;
            */
        }

        protected Layout<View> createCommonLayout() {
            if (backgroundImgP == null) {
                backgroundImgP = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(backgroundFilename, assembly),
                    EnsureSquare = false,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                };
            }
            if (termsOfServiceLabel == null) {
                createWebButtons();
            }
            /*if (signupBackground == null) {
                signupBackground = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.signinblock.png", assembly),
                    EnsureSquare = false,
                };
            }*/
            Grid portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            for (int i = 0; i < 20; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            int colsWide = 8;
            for (int j = 0; j < colsWide; j++) {
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            portraitView.Children.Add(backgroundImgP, 0, 0);
            Grid.SetColumnSpan(backgroundImgP, colsWide);
            Grid.SetRowSpan(backgroundImgP, 20);

            portraitView.Children.Add(loggedInLabel, 0, 6);
            Grid.SetColumnSpan(loggedInLabel, colsWide);
            //portraitView.Children.Add(signupBackground, 1, 7);
            //Grid.SetColumnSpan(signupBackground, 6);
            //Grid.SetRowSpan(signupBackground, 2);
            portraitView.Children.Add(usernameEntry, 1, 7);
            Grid.SetColumnSpan(usernameEntry, 6);
            portraitView.Children.Add(passwordEntry, 1, 8);
            Grid.SetColumnSpan(passwordEntry, 6);
            portraitView.Children.Add(connectButton, 1, 10);
            Grid.SetColumnSpan(connectButton, 6);
            portraitView.Children.Add(facebookLogin, 1, 12);
            Grid.SetColumnSpan(facebookLogin, 6);
            if (Device.OS == TargetPlatform.iOS) {
                portraitView.Children.Add(googleLogin,1,13);
                Grid.SetColumnSpan(googleLogin, 6);
            }

            Label forgotPassword = new Label {
                Text = "Forgot Password?",
                TextColor = Color.Blue,
                HorizontalTextAlignment =TextAlignment.Center,
                VerticalTextAlignment =TextAlignment.End,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += async (object sender, EventArgs args) => {
                await forgotPassword.FadeTo(0, 175);
                await forgotPassword.FadeTo(1, 175);
                Content = createForgotPwdLayout();
            };
            forgotPassword.GestureRecognizers.Add(tap);

            portraitView.Children.Add(forgotPassword, 0, 15);
            Grid.SetColumnSpan(forgotPassword, 4);
            Label registerLabel = new Label {
                Text = "New? Enter an email, password above \n then tap here to play",
                TextColor =Color.Blue,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Start,
                Margin = 0,
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            };
            TapGestureRecognizer regTap = new TapGestureRecognizer();
            regTap.Tapped += async (sender, args) => {
                await registerLabel.FadeTo(0, 175);
                await registerLabel.FadeTo(1, 175);
                loggedInLabel.Text = "Registering...";
                GlobalStatusSingleton.username = usernameEntry.Text;
                GlobalStatusSingleton.password = passwordEntry.Text;
                // call the event handler that manages the communication with server for registration.
                if (RegisterNow != null) {
                    RegisterNow(this, eDummy);
                }
            };
            registerLabel.GestureRecognizers.Add(regTap);

            portraitView.Children.Add(registerLabel, 4, 15);
            Grid.SetColumnSpan(registerLabel, 4);
            Grid.SetRowSpan(registerLabel, 2);
            portraitView.Children.Add(termsOfServiceLabel, 0, 17);
            Grid.SetColumnSpan(termsOfServiceLabel, 4);
            portraitView.Children.Add(privacyPolicyLabel, 4, 17);
            Grid.SetColumnSpan(privacyPolicyLabel, 4);
            //layoutP.Children.Add(portraitView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            //return layoutP;
            return portraitView;
        }

        // @todo Refactoring. Do we still need this page?  Provides access to registration for an anon account. 
        //    Can that happen in settings?
        protected Layout<View> createAnonLoggedInLayout() {
            loggedInLabel.Text = "logged in anonymously";

            activeLayout = LAYOUT_CONNECTED_ANON;
            gotoRegistrationButton = new Button {
                Text = "Register me now!",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
            };
            gotoRegistrationButton.Clicked += (sender, args) => {
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

            anonLoggedInLayout = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14, GridUnitType.Star) });
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Star) });
            anonLoggedInLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            anonLoggedInLayout.Children.Add(upperPortionOfGrid, 0, 0);
            //anonLoggedInLayout.Children.Add(CenterConsole, 0, 1);  // object, col, row
            //anonLoggedInLayout.Children.Add(defaultNavigationButtons, 0, 2);  // object, col, row
            anonLoggedInLayout.BackgroundColor = GlobalStatusSingleton.backgroundColor;
            return anonLoggedInLayout;
        }

        protected Layout<View> createForgotPwdLayout() {
            if (backgroundImgP == null) {
                assembly = this.GetType().GetTypeInfo().Assembly;
                backgroundImgP = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(backgroundFilename, assembly),
                    EnsureSquare = false,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                };
            }
            if (termsOfServiceLabel == null) {
                createWebButtons();
            }
            Grid portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 0 };
            for (int i = 0; i < 20; i++) {
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }
            int colsWide = 8;
            for (int j = 0; j < colsWide; j++) {
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            portraitView.Children.Add(backgroundImgP, 0, 0);
            Grid.SetColumnSpan(backgroundImgP, colsWide);
            Grid.SetRowSpan(backgroundImgP, 20);

            portraitView.Children.Add(loggedInLabel, 0, 6);
            Grid.SetColumnSpan(loggedInLabel, colsWide);

            usernameEntry.Placeholder = "Enter your email";
            portraitView.Children.Add(usernameEntry, 2, 7);
            Grid.SetColumnSpan(usernameEntry, 4);

            if (forgotButton == null) {
                forgotButton = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.sendForgotPasswordButton_inactive.png", assembly),
                    EnsureSquare = false,
                };
                TapGestureRecognizer forgot = new TapGestureRecognizer();
                forgot.Tapped += (object sender, EventArgs args) => {
                    if (ForgotClicked != null) {
                        ForgotClicked(sender, args);
                    }
                };
                forgotButton.GestureRecognizers.Add(forgot);
            }
            portraitView.Children.Add(forgotButton, 2, 10);
            Grid.SetColumnSpan(forgotButton, 4);

            if (backButton == null) {
                backButton = new iiBitmapView() {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png",assembly),
                    EnsureSquare = false,
                };
                TapGestureRecognizer tap = new TapGestureRecognizer();
                tap.Tapped += (object Sender, EventArgs args) => {
                    Content = createCommonLayout();
                };
                backButton.GestureRecognizers.Add(tap);
            }
            portraitView.Children.Add(backButton, 0, 1);
            

            portraitView.Children.Add(termsOfServiceLabel, 0, 17);
            Grid.SetColumnSpan(termsOfServiceLabel, 4);
            portraitView.Children.Add(privacyPolicyLabel, 4, 17);
            Grid.SetColumnSpan(privacyPolicyLabel, 4);
            //layoutP.Children.Add(portraitView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            //return layoutP;
            return portraitView;
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
            cancelRegistrationButton.Clicked += (sender, args) => {
                Content = anonLoggedInLayout;
            };

            registrationLayout = new StackLayout {
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
                VerticalOptions = LayoutOptions.Center,
                Children = {
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

        protected StackLayout usernameRow() {
            return new StackLayout {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    new Label { Text = "Email", TextColor = Color.Black, BackgroundColor = GlobalStatusSingleton.backgroundColor, },
                    usernameEntry,
                }
            };
        }

        protected StackLayout passwordRow() {
            return new StackLayout {
                Orientation = StackOrientation.Horizontal,
                VerticalOptions = LayoutOptions.Center,
                Children = {
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
            registerButton.Clicked += (sender, args) => {
                GlobalStatusSingleton.username = usernameEntry.Text;
                GlobalStatusSingleton.password = passwordEntry.Text;
                // call the event handler that manages the communication with server for registration.
                if (RegisterNow != null) {
                    RegisterNow(this, eDummy);
                }
            };
        }

        View returnLayout;
        protected void createWebButtons() {
            tosPage = iiWebPage.getInstance(GlobalStatusSingleton.TERMS_OF_SERVICE_URL, this, Content);
            //tosPage.setReturnPoint(this.Content);
            termsOfServiceLabel = new Label {
                //Text = "Tap here to read our Terms of Service",
                Text = "Terms of Service",
                TextColor = Color.Blue,
                FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.End,
            };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            termsOfServiceLabel.GestureRecognizers.Add(tap);
            tap.Tapped += (sender, args) => {
                //boom.
                returnLayout = Content;
                iiWebPage newPage = iiWebPage.getInstance(GlobalStatusSingleton.TERMS_OF_SERVICE_URL, this, Content);
                Content = newPage;
            };

            privacyPolicyPage = iiWebPage.getInstance(GlobalStatusSingleton.PRIVACY_POLICY_URL, this, Content);
            privacyPolicyLabel = new Label {
                //Text = "And here for our Privacy Policy",
                Text = "Privacy Policy",
                TextColor = Color.Blue,
                FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.End,
            };
            tap = new TapGestureRecognizer();
            privacyPolicyLabel.GestureRecognizers.Add(tap);
            tap.Tapped += (sender, args) => {
                returnLayout = Content;
                iiWebPage newPage = iiWebPage.getInstance(GlobalStatusSingleton.PRIVACY_POLICY_URL, this, Content);
                Content = newPage;
            };
        }

        /// <summary>
        /// Public so that ThirdPartyAuthenticator can call back to here on auth success.
        /// </summary>
        public void LoginSuccess() {
            if (TokenReceived != null) {
                TokenReceived(this, eDummy);
            }

            // Ok! This all needs to change!!!
            // This method is unreliable.  If the user double taps, I can send this request twice.
            // but the second time through, parent will no longer be set!
            //((MasterPage)Parent).leaveLogin();
            ((MasterPage)Application.Current.MainPage).leaveLogin();

            /*
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
            */
            loggedInLabel.Text = "Logged in as " + GlobalStatusSingleton.username;
        }

        public async void handleLoginFail(string loginResult) {
            loggedInLabel.Text = loginResult + "(" + loginAttemptCounter + ")";
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
        }

        protected async virtual void OnMyLogin(object sender, EventArgs e) {
            //string loginResult = await requestLoginAsync();
            Debug.WriteLine("DHB:LoginPage:OnMyLogin");
            loggedInLabel.Text = " Connecting... ";
            if ((ThirdPartyAuthenticator.oauthData == null) || (ThirdPartyAuthenticator.oauthData.refreshToken == null) || (ThirdPartyAuthenticator.oauthData.refreshToken.Equals(""))) {
                Debug.WriteLine("DHB:LoginPage:OnMyLogin no oauth data");
                loginAttemptCounter++;
                string loginResult = await requestTokenAsync();
                Debug.WriteLine("DHB:LoginPage:OnMyLogin requestToken result:" + loginResult);
                if ((loginResult.Equals("login failure")) || (loginResult.Equals(BAD_PASSWORD_LOGIN_FAILURE)) || (loginResult.Equals(ANON_REGISTRATION_FAILURE))) {
                    handleLoginFail(loginResult);
                } else {
                    LoginSuccess();
                }
            } else {
                Debug.WriteLine("DHB:LoginPage:OnMyLogin oauthdata");
                if (oauthManager == null) {
                    Debug.WriteLine("DHB:LoginPage:OnMyLogin oauthdata wtf oauth mgr was null");
                }
                Debug.WriteLine("DHB:LoginPage:OnMyLogin oauthdata post null check");
                // oauth case. use the refresh token to grab a new access token.
                oauthManager.refreshAuthentication(this.Navigation);
            }
        }

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
                    loggedInLabel.Text = registrationResult;
                } else {
                    // registration success. Send a login request message.
                    if (MyLogin != null) {
                        MyLogin(this, eDummy);
                    }

                    // why?
                    //Content = createAutoLoginLayout();
                    loggedInLabel.Text = "Registration success! logging in...";
                    if (RegisterSuccess != null) {
                        Debug.WriteLine("DHB:LoginPage:OnRegisterNow sending RegisterSuccess event");
                        RegisterSuccess(this, eDummy);
                    }
                }
            } else {
                loggedInLabel.Text = "Please register with a valid email address";
            }
        }

        protected /* async */ virtual void OnRegisterSuccess(object sender, EventArgs e) {
            // right now, do nothing.
            // now we goto the instructions page.
            //this.Content = CenterConsole.InstructionsPage;
            //GlobalStatusSingleton.firstTimePlaying = true;
        }


        // handles the communication with the server to register an account fully.
        // @todo actually implement this function! (this is the anon registration code)
        static async Task<string> requestRegistrationAsync() {
            Debug.WriteLine("DHB:LoginPage:requestRegistrationAsync start");
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
                    Debug.WriteLine("DHB:LoginPage:requestRegistrationAsync success");
                    // @todo on switch to oauth/jwt uncomment the token code (it currently is called after the login).
                    GlobalStatusSingleton.firstTimePlaying = true;
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
                    Debug.WriteLine("DHB:LoginPage:requestRegistrationAsync failure! statuscode: " + result.StatusCode.ToString());
                    string serverResponse = await requestTokenAsync();
                    if (serverResponse.Equals("Success")) {
                        // the user entered a correct username and password. treat as a login...
                        resultMsg = serverResponse;
                    } else {
                        resultMsg = REGISTRATION_FAILURE;
                    }
                }
                ////////// still todo above here..........
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine("DHB:LoginPage:requestRegistrationAsync Webexception");
                Debug.WriteLine(err.ToString());
                resultMsg = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                // do something!!
                Debug.WriteLine("DHB:LoginPage:requestRegistrationAsync HttpException");
                Debug.WriteLine(err.ToString());
                resultMsg = REGISTRATION_FAILURE;
            }
            Debug.WriteLine("DHB:LoginPage:requestRegistrationAsync done");
            return resultMsg;
        }

        protected static async Task<string> requestTokenAsync() {
            string result = BAD_PASSWORD_LOGIN_FAILURE;
            LoginRequestJSON loginInfo = new LoginRequestJSON();
            loginInfo.username = GlobalStatusSingleton.username;
            loginInfo.password = GlobalStatusSingleton.password;

            try {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.startingURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                Debug.WriteLine("DHB:LoginPage:requestTokenAsync queryJson:" + jsonQuery);
                HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, AUTH);
                tokenRequest.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                HttpResponseMessage tokenResult = await client.SendAsync(tokenRequest);
                Debug.WriteLine("DHB:LoginPage:requestTokenAsync requestResultCode:" + System.Net.HttpStatusCode.OK);
                if (tokenResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    string tknResult = await tokenResult.Content.ReadAsStringAsync();
                    Debug.WriteLine("DHB:LoginPage:requestTokenAsync result:" + tknResult);
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    GlobalStatusSingleton.authToken = JsonConvert.DeserializeObject<AuthenticationToken>(tknResult, settings);
                    GlobalStatusSingleton.loggedIn = true;
                    GlobalStatusSingleton.username = GlobalStatusSingleton.authToken.email;
                    result = "Success";
                    Debug.WriteLine("DHB:LoginPage:requestTokenAsync good end.");
                    await requestBaseURLAsync();
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

        protected static async Task<string> requestBaseURLAsync() {
            string result = BAD_PASSWORD_LOGIN_FAILURE;

            try {
                HttpClient client = new HttpClient();

                //client.BaseAddress = new Uri(GlobalStatusSingleton.startingURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                //string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                Debug.WriteLine("DHB:LoginPage:requestBaseURLAsync queryJson:");
                HttpRequestMessage baseRequest = new HttpRequestMessage(HttpMethod.Get, GlobalStatusSingleton.startingURL+BASE);
                baseRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());
                //tokenRequest.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                HttpResponseMessage baseResult = await client.SendAsync(baseRequest);
                Debug.WriteLine("DHB:LoginPage:requestBaseURLAsync requestResultCode:" + System.Net.HttpStatusCode.OK);
                if (baseResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    string urlResult = await baseResult.Content.ReadAsStringAsync();
                    Debug.WriteLine("DHB:LoginPage:requestTokenAsync result:" + urlResult);
                    var settings = new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    URLRequestJSON urlRes = JsonConvert.DeserializeObject<URLRequestJSON>(urlResult, settings);
                    if (urlRes != null) {
                        GlobalStatusSingleton.activeURL = urlRes.baseurl;
                    } else {
                        GlobalStatusSingleton.activeURL = GlobalStatusSingleton.startingURL;
                    }
                    result = "Success";
                    Debug.WriteLine("DHB:LoginPage:requestBaseURLAsync good end.");
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

        protected virtual void OnLogoutClicked(object sender, EventArgs e) {
            // Anonymous users can't logout...
            //if (GlobalStatusSingleton.username.Equals(GlobalStatusSingleton.UUID)) {
            if (!isEmailAddress(GlobalStatusSingleton.username)) {
                // can't logout in this scenario...
                // shit. this can lead to a dead case if there's incorrect account info.
                loggedInLabel.Text = "Sorry, Anonymous users can't log out.";
                Debug.WriteLine("DHB:LoginPage:OnLogoutClicked in the anon user use case.");
            } else {
                // deactivate the carousel. - happens in MainPageUISwipe, who also consumes this event
                // make sure this is the active page - happens in MainPageUISwipe, who also consumes this event
                // change to the force login page
                Device.BeginInvokeOnMainThread(() => {
                    //backgroundImg = null;
                    GlobalStatusSingleton.loggedIn = false;
                    // need to clear the old oauth token, or the user will stay logged in.
                    ThirdPartyAuthenticator.oauthData = null;
                    Content = createForceLoginLayout();
                });
            }
        }

        ///////// BEGIN OAUTH
        ///////// BEGIN OAUTH
        ///////// BEGIN OAUTH
        ThirdPartyAuthenticator oauthManager;
        private void createGoogleButton() {
            googleLogin = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.google_login.png"), };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += async (sender, EventArgs) => {
                await googleLogin.FadeTo(0.25, 175);
                await googleLogin.FadeTo(1, 175);
                oauthManager.configForGoogle();
                oauthManager.startAuthentication(Navigation);
            };
            googleLogin.GestureRecognizers.Add(tap);
        }

        private void createFacebookButton() {
            facebookLogin = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.facebook_login.png"), };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += async (sender, EventArgs) => {
                await facebookLogin.FadeTo(0.25, 175);
                await facebookLogin.FadeTo(1, 175);
                oauthManager.configForFacebook();
                oauthManager.startAuthentication(Navigation);
            };
            facebookLogin.GestureRecognizers.Add(tap);
        }
        ///////// END OAUTH
        ///////// END OAUTH
        ///////// END OAUTH

        /// Forgot Password
        public async void OnForgotPwdClicked(object sender, EventArgs args) {
            GlobalStatusSingleton.username = usernameEntry.Text;
            string result = await requestForgotPassword();
            if (result.Equals("Success")) {
                Device.BeginInvokeOnMainThread(() => {
                    loggedInLabel.Text = "Check your inbox. An email has been sent!";
                    Content = createCommonLayout();
                });
            } else if (result.Equals("Unknown")) {
                Device.BeginInvokeOnMainThread(() => {
                    loggedInLabel.Text = "Sorry. That email address is not recognized.";
                });
            }
        }

        protected static async Task<string> requestForgotPassword() {
            string result = BAD_PASSWORD_LOGIN_FAILURE;

            try {
                HttpClient client = new HttpClient();

                //client.BaseAddress = new Uri(GlobalStatusSingleton.startingURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                Debug.WriteLine("DHB:LoginPage:requestForgotPassword queryJson:");
                HttpRequestMessage baseRequest = new HttpRequestMessage(HttpMethod.Get, GlobalStatusSingleton.startingURL + FORGOT_PWD + "?email="+ GlobalStatusSingleton.username);

                // should not have a token.
                //baseRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage baseResult = await client.SendAsync(baseRequest);
                Debug.WriteLine("DHB:LoginPage:requestForgotPassword requestResultCode:" + System.Net.HttpStatusCode.OK);
                if (baseResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    Debug.WriteLine("DHB:LoginPage:requestForgotPassword success");
                    result = "Success";
                } else if (baseResult.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    Debug.WriteLine("DHB:LoginPage:requestForgotPassword fail");
                    result = "Unknown";
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
        /// 
        // @todo Refactoring Question.  Do we still need the goHome function?
        // What calls it?
        // How do we get to the loggedOut screen?
    }
}
