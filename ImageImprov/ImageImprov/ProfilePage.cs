using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ImageImprov {
    public class ProfilePage : ContentView, IProvideProfileNavigation, ILeaveZoomCallback {
        public delegate void gotoPage();

        bool profileDisplayStatus = true;
        Grid portraitView;
        public UpperProfileSection coreProfile;
        public ProfileNavRow navRow;  // so master page can access and bind for settings on/off.
        ContentView mainBody;
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

        MedalsPage medalsPage;
        public MedalsPage MedalsPage {
            get { return medalsPage; }
        }

        public View PreviousView { get; set; }

        public ProfilePage() {
            coreProfile = new UpperProfileSection();
            navRow = new ProfileNavRow(this);
            settingsPage = new SettingsPage();
            instructionsPage = new InstructionsPage();
            mySubmissionsPage = new MySubmissionsPage();
            likesPage = new LikesPage();
            medalsPage = new MedalsPage();

            mainBody = new ContentView();
            mainBody.Content = MySubmissionsPage;
            buildUI();
        }

        public int buildUI() {
            if (portraitView == null) {
                // yes, these are unbalanced for a reason.
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                //portraitView.SizeChanged += OnPortraitViewSizeChanged;
                for (int i = 0; i < numGridRows; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                // flush the old children.
                portraitView.Children.Clear();
            }

            if (profileDisplayStatus) {
                portraitView.Children.Add(coreProfile, 0, 0);
                Grid.SetRowSpan(coreProfile, 4);
                portraitView.Children.Add(navRow, 0, 4);
                Grid.SetRowSpan(navRow, 2);

                portraitView.Children.Add(mainBody, 0, 6);
                Grid.SetRowSpan(mainBody, 10);
            } else {
                portraitView.Children.Add(navRow, 0, 0);
                Grid.SetRowSpan(navRow, 2);

                portraitView.Children.Add(mainBody, 0, 2);
                Grid.SetRowSpan(mainBody, 14);
            }
            Content = portraitView;
            return 1;
        }

        public void gotoSubmissionsPage() {
            mainBody.Content = MySubmissionsPage;
        }

        public void gotoLikesPage() {
            mainBody.Content = LikesPage;
        }

        public void gotoMedalsPage() {
            mainBody.Content = MedalsPage;
        }

        public void flipShowProfile() {
            profileDisplayStatus = !profileDisplayStatus;
            buildUI();
        }

        public void gotoSettingsPage() {
            navRow.HighlightedButtonIndex = ProfileNavRow.SETTINGS_INDEX;
            mainBody.Content = SettingsPage;
        }

        public void gotoInstructionsPage() {
            mainBody.Content = InstructionsPage;
        }

        // the zoom callback.
        public void returnToCaller() {
            // This will be a problem that I have to refactor out when we get to PlayerProfile Page.
            // Right now, this is the only page that has a zoom in the center console, so skirting by.
            //Content = CenterConsole.MySubmissionsPage;

            // This causes a hang for some reason...
            //mainBody.Content = PreviousView;  // i believe we will already be set to previous content.
            Content = portraitView;
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

    }
}
