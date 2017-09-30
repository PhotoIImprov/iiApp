using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// This is the central bit with all the buttons that shows when a player logs in
    /// or whenever they press home
    /// </summary>
    public class PlayerPageCenterConsole : Grid {
        TapGestureRecognizer tapGesture;
        Image gotoLeaderboardButton;
        Image gotoPurchasesButton;
        Image gotoSettingsButton;
        Image gotoHelpButton;

        PlayerContentPage parent;
        
        // late build this so we get the correct username information.
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
            get { return instructionsPage;  }
        }

        MySubmissionsPage mySubmissionsPage;
        public MySubmissionsPage MySubmissionsPage {
            get { return mySubmissionsPage; }
        }

        LikesPage likesPage;
        public LikesPage LikesPage {
            get { return likesPage; }
        }

        /*
        HamburgerPage hamburgerPage = new HamburgerPage();
        public HamburgerPage HamburgerPage {
            get { return hamburgerPage; }
        }*/

        public PlayerPageCenterConsole(PlayerContentPage parent) {
            Padding = 10;

            this.parent = parent;
            // purchasesPage = new PurchasesPage();
            //settingsPage = new SettingsPage(); // be careful. Settings is null, but being built late so we have correct user info.
            instructionsPage = new InstructionsPage();
            mySubmissionsPage = new MySubmissionsPage();
            likesPage = new LikesPage();
            //hamburgerPage = new HamburgerPage();

            buildUI();
        }

        public int buildUI() {
            // ColumnSpacing = 1; RowSpacing = 1;
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinitions.Add(new RowDefinition());
            RowDefinitions.Add(new RowDefinition());
            //Children.Add(gotoLeaderboardButton, 0, 1);  // col, row
            //Children.Add(gotoPurchasesButton, 1, 1);
            //Children.Add(gotoSettingsButton, 2, 1);
            //Children.Add(gotoHelpButton, 2, 0);

            // hide all the home page stuff and replace with leaderboard page
            parent.Content = instructionsPage;
            return 1;
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            //if (sender == gotoLeaderboardButton) {
            //    parent.Content = leaderboardPage;
            //} else 
            if (sender == gotoPurchasesButton) {
                //parent.Content = purchasesPage;
            } else if (sender == gotoSettingsButton) {
                if (settingsPage == null) {
                    settingsPage = new SettingsPage();
                }
                parent.Content = settingsPage;
            } else if (sender == gotoHelpButton) {
                parent.Content = instructionsPage;
            }
        }
 
    }
}
