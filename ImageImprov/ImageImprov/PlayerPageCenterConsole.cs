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
    class PlayerPageCenterConsole : Grid {
        TapGestureRecognizer tapGesture;
        Image gotoLeaderboardButton;
        Image gotoPurchasesButton;
        Image gotoSettingsButton;

        PlayerContentPage parent;

        // Non-main page pages.
        LeaderboardPage leaderboardPage;
        public LeaderboardPage LeaderboardPage {
            get { return leaderboardPage;  }
        }
        //PurchasesPage purchasesPage;
        
        SettingsPage settingsPage;
        public SettingsPage SettingsPage {
            get { return settingsPage; }
        }

        public PlayerPageCenterConsole(PlayerContentPage parent) {
            this.parent = parent;
            leaderboardPage = new LeaderboardPage();
            // purchasesPage = new PurchasesPage();
            settingsPage = new SettingsPage();

            gotoLeaderboardButton= new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.leaderboard.png")
            };
            gotoPurchasesButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.purchases.png")
            };
            gotoSettingsButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.settings.png")
            };

            tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnClicked;
            gotoLeaderboardButton.GestureRecognizers.Add(tapGesture);
            gotoPurchasesButton.GestureRecognizers.Add(tapGesture);
            gotoSettingsButton.GestureRecognizers.Add(tapGesture);

            // @todo implement purchases.
            gotoPurchasesButton.IsVisible = false;

            // ColumnSpacing = 1; RowSpacing = 1;
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            Children.Add(gotoLeaderboardButton, 0, 0);  // col, row
            Children.Add(gotoPurchasesButton, 1, 0);
            Children.Add(gotoSettingsButton, 2, 0);
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            if (sender == gotoLeaderboardButton) {
                parent.Content = leaderboardPage;
            } else if (sender == gotoPurchasesButton) {
                //parent.Content = purchasesPage;
            } else if (sender == gotoSettingsButton) {
               parent.Content = settingsPage;
            }
        }
 
    }
}
