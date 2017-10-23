using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Newtonsoft.Json;

namespace ImageImprov {
    public class ProfilePage : ContentView, IProvideProfileNavigation, ILeaveZoomCallback {
        const string BADGES = "badges";

        bool profileDisplayStatus = true;
        Grid portraitView;
        public UpperProfileSection coreProfile;
        public ProfileNavRow navRow;  // so master page can access and bind for settings on/off.
        //ContentView mainBody;
        //View mainBody;
        int numGridRows = 16;

        SettingsPage settingsPage = null;
        public SettingsPage SettingsPage {
            get {
                if (settingsPage == null) {
                    settingsPage = new SettingsPage();
                }
                return settingsPage;
            }
        }

        InstructionsPage instructionsPage;
        public InstructionsPage InstructionsPage {
            get { return instructionsPage; }
        }

        MySubmissionsPage mySubmissionsPage;
        public MySubmissionsPage MySubmissionsPage {
            get { return mySubmissionsPage; }
        }

        LikesPage likesPage;
        public LikesPage LikesPage {
            get { return likesPage; }
        }

        EventsHistory_Profile eventsPage;
        public EventsHistory_Profile EventsPage {
            get { return eventsPage; }
        }

        BadgesPage badgesPage;
        public BadgesPage BadgesPage {
            get { return badgesPage; }
        }

        public View PreviousView { get; set; }

        public ProfilePage() {
            coreProfile = new UpperProfileSection();
            navRow = new ProfileNavRow(this);
            settingsPage = new SettingsPage() { IsVisible = false, };
            instructionsPage = new InstructionsPage() { IsVisible = false, };
            mySubmissionsPage = new MySubmissionsPage();
            likesPage = new LikesPage() { IsVisible = false, };
            eventsPage = new EventsHistory_Profile() { IsVisible = false, };
            badgesPage = new BadgesPage() { IsVisible = false, };

            // Going the vis/invis route.
            //mainBody = new ContentView();
            //mainBody.Content = MySubmissionsPage;
            //mainBody = MySubmissionsPage.Content;

            buildUI();
        }

        /* public int buildUI() {
             if (portraitView == null) {
                 // yes, these are unbalanced for a reason.
                 portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                 //portraitView.SizeChanged += OnPortraitViewSizeChanged;
                 for (int i = 0; i < numGridRows; i++) {
                     portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                 }
                 portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
             } else {
                 // flush the old children. adjust....
                 if (portraitView.Children != null) {
                     portraitView.Children.Clear();
                 }
             }

             int rowStart = 6;
             int rowExtent = 10;
             if (profileDisplayStatus) {
                 portraitView.Children.Add(coreProfile, 0, 0);
                 Grid.SetRowSpan(coreProfile, 4);
                 portraitView.Children.Add(navRow, 0, 4);
                 Grid.SetRowSpan(navRow, 2);

                 //portraitView.Children.Add(mainBody, 0, 6);
                 //Grid.SetRowSpan(mainBody, 10);
             } else {
                 portraitView.Children.Add(navRow, 0, 0);
                 Grid.SetRowSpan(navRow, 2);
                 rowStart = 2;
                 rowExtent = 14;
                 //portraitView.Children.Add(mainBody, 0, 2);
                 //Grid.SetRowSpan(mainBody, 14);
             }
             portraitView.Children.Add(MySubmissionsPage, 0, rowStart);
             portraitView.Children.Add(LikesPage, 0, rowStart);
             portraitView.Children.Add(EventsPage, 0, rowStart);
             portraitView.Children.Add(BadgesPage, 0, rowStart);
             portraitView.Children.Add(SettingsPage, 0, rowStart);

             Grid.SetRowSpan(MySubmissionsPage, rowExtent);
             Grid.SetRowSpan(LikesPage, rowExtent);
             Grid.SetRowSpan(EventsPage, rowExtent);
             Grid.SetRowSpan(BadgesPage, rowExtent);
             Grid.SetRowSpan(SettingsPage, rowExtent);
             Content = portraitView;
             return 1;
         }  */

        public int buildUI() {
            if (portraitView == null) {
                // yes, these are unbalanced for a reason.
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                //portraitView.SizeChanged += OnPortraitViewSizeChanged;
                for (int i = 0; i < numGridRows; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                placeWidgets();
            }

            int rowStart = 6;
            int rowExtent = 10;
            if (profileDisplayStatus) {
                //portraitView.Children.Add(coreProfile, 0, 0);
                coreProfile.IsVisible = true;
                Grid.SetRow(coreProfile, 0);
                Grid.SetColumn(coreProfile, 0);
                Grid.SetRowSpan(coreProfile, 4);

                //portraitView.Children.Add(navRow, 0, 4);
                Grid.SetRow(navRow, 4);
                Grid.SetColumn(navRow, 0);
                Grid.SetRowSpan(navRow, 2);
            } else {
                // hmm... and what happens to coreprofile???
                coreProfile.IsVisible = false;
                //portraitView.Children.Add(navRow, 0, 0);
                Grid.SetRow(navRow, 0);
                Grid.SetColumn(navRow, 0);
                Grid.SetRowSpan(navRow, 2);
                rowStart = 2;
                rowExtent = 14;
                //portraitView.Children.Add(mainBody, 0, 2);
                //Grid.SetRowSpan(mainBody, 14);
            }
            //portraitView.Children.Add(MySubmissionsPage, 0, rowStart);
            Grid.SetRow(MySubmissionsPage, rowStart);
            Grid.SetColumn(MySubmissionsPage, 0);
            //portraitView.Children.Add(LikesPage, 0, rowStart);
            Grid.SetRow(LikesPage, rowStart);
            Grid.SetColumn(LikesPage, 0);

            //portraitView.Children.Add(EventsPage, 0, rowStart);
            Grid.SetRow(EventsPage, rowStart);
            Grid.SetColumn(EventsPage, 0);

            //portraitView.Children.Add(BadgesPage, 0, rowStart);
            Grid.SetRow(BadgesPage, rowStart);
            Grid.SetColumn(BadgesPage, 0);

            //portraitView.Children.Add(SettingsPage, 0, rowStart);
            Grid.SetRow(SettingsPage, rowStart);
            Grid.SetColumn(SettingsPage, 0);


            Grid.SetRowSpan(MySubmissionsPage, rowExtent);
            Grid.SetRowSpan(LikesPage, rowExtent);
            Grid.SetRowSpan(EventsPage, rowExtent);
            Grid.SetRowSpan(BadgesPage, rowExtent);
            Grid.SetRowSpan(SettingsPage, rowExtent);
            Content = portraitView;
            return 1;
        }

        protected void placeWidgets(int rowStart = 6) {
            portraitView.Children.Add(coreProfile, 0, 0);
            portraitView.Children.Add(navRow, 0, 4);
            portraitView.Children.Add(MySubmissionsPage, 0, rowStart);
            portraitView.Children.Add(LikesPage, 0, rowStart);
            portraitView.Children.Add(EventsPage, 0, rowStart);
            portraitView.Children.Add(BadgesPage, 0, rowStart);
            portraitView.Children.Add(SettingsPage, 0, rowStart);
        }

        public void gotoSubmissionsPage() {
            //mainBody = MySubmissionsPage.Content;
            navRow.NavHighlightIndex = 0;
            MySubmissionsPage.IsVisible = true;
            LikesPage.IsVisible = false;
            EventsPage.IsVisible = false;
            BadgesPage.IsVisible = false;
            SettingsPage.IsVisible = false;
        }

        public void gotoLikesPage() {
            //mainBody.Content = LikesPage;
            //mainBody = LikesPage.Content;
            navRow.NavHighlightIndex = 1;
            MySubmissionsPage.IsVisible = false;
            LikesPage.IsVisible = true;
            EventsPage.IsVisible = false;
            BadgesPage.IsVisible = false;
            SettingsPage.IsVisible = false;
        }

        public void gotoEventsHistoryPage() {
            //mainBody = EventsPage.Content;
            navRow.NavHighlightIndex = 2;
            MySubmissionsPage.IsVisible = false;
            LikesPage.IsVisible = false;
            EventsPage.IsVisible = true;
            BadgesPage.IsVisible = false;
            SettingsPage.IsVisible = false;
        }

        public void gotoBadgesPage() {
            //mainBody = BadgesPage.Content;
            navRow.NavHighlightIndex = 3;
            MySubmissionsPage.IsVisible = false;
            LikesPage.IsVisible = false;
            EventsPage.IsVisible = false;
            BadgesPage.IsVisible = true;
            SettingsPage.IsVisible = false;
        }

        public void flipShowProfile() {
            profileDisplayStatus = !profileDisplayStatus;
            buildUI();
        }

        public void gotoSettingsPage() {
            navRow.NavHighlightIndex = ProfileNavRow.SETTINGS_INDEX;
            MySubmissionsPage.IsVisible = false;
            LikesPage.IsVisible = false;
            EventsPage.IsVisible = false;
            BadgesPage.IsVisible = false;
            SettingsPage.IsVisible = true;
            //mainBody = SettingsPage.Content;
        }

        public void gotoInstructionsPage() {
            //mainBody = InstructionsPage.Content;
            // @todo Turn this instructions page back on.
        }

        // the zoom callback.
        public void returnToCaller() {
            // This will be a problem that I have to refactor out when we get to PlayerProfile Page.
            // Right now, this is the only page that has a zoom in the center console, so skirting by.
            //Content = CenterConsole.MySubmissionsPage;

            // This causes a hang for some reason...
            //mainBody.Content = PreviousView;  // i believe we will already be set to previous content.
            Content = portraitView;
            //((MasterPage)Application.Current.MainPage).thePages.Position = MainPageSwipeUI.PROFILE_PAGE;
            /*
            if (PreviousView == MySubmissionsPage) {
                gotoSubmissionsPage();
            } else if (PreviousView == LikesPage) {
                gotoLikesPage();
            } else if (PreviousView == MedalsPage) {
                gotoMedalsPage();
            } else {
                gotoSubmissionsPage();
            }*/
        }

        // why doesn't badges code handle this?
        public virtual async void TokenReceived(object sender, EventArgs e) {
            //coreProfile.usernameLabel.Text = GlobalStatusSingleton.username;
            string jsonQuery = "";
            string result = "fail";
            while (result.Equals("fail")) {
                result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Get, BADGES, jsonQuery);
                if (result.Equals("fail")) {
                    await Task.Delay(10000);
                }
            }  
            if (!result.Equals("fail")) {
                BadgesResponseJSON badges = JsonConvert.DeserializeObject<BadgesResponseJSON>(result);
                if (badges != null) {
                    coreProfile.SetBadgesData(badges);
                    badgesPage.SetBadgesData(badges);
                }
            } else {
                Debug.WriteLine("DHB:CameraCategorySelectionView:OnEventsLoadRequest event apicall failed!");
            }
        }

    }
}
