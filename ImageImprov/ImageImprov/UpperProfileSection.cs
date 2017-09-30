using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    // This controls profile pic, friends, lightbulbs, etc.
    public class UpperProfileSection : ContentView {
        Grid portraitView;

        iiBitmapView profilePic;
        iiBitmapView lightbulbs;
        Label lightbulbCount = new Label {
            Text = "Full count coming soon",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = Color.White,
            TextColor = Color.Black,
            //IsVisible = false,
        };

        Label usernameLabel = new Label {
            Text = "",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
            //IsVisible = false,
        };
        Label friendInfo = new Label {
            Text = "Friends: Coming Soon",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
            //IsVisible = false,
        };
        //iiBitmapView gotoSettingsButton;

        public UpperProfileSection() {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            lightbulbs = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.reward.png", assembly));

            buildUI();
        }

        private int buildUI() {
            if (portraitView == null) {
                // yes, these are unbalanced for a reason.
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                //portraitView.SizeChanged += OnPortraitViewSizeChanged;
                for (int i = 0; i < 4; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
            } else {
                // flush the old children.
                portraitView.Children.Clear();
            }

            StackLayout sl = new StackLayout {
                Orientation = StackOrientation.Horizontal,
                Children = { lightbulbs, lightbulbCount, },
            };
            portraitView.Children.Add(usernameLabel, 2, 0);
            Grid.SetColumnSpan(usernameLabel, 2);
            portraitView.Children.Add(sl, 2, 1);
            Grid.SetColumnSpan(sl, 2);
            portraitView.Children.Add(friendInfo, 2, 0);
            Grid.SetColumnSpan(friendInfo, 2);

            Content = portraitView;
            return 1;
        }

        public virtual void TokenReceived(object sender, EventArgs e) {
            usernameLabel.Text = GlobalStatusSingleton.username;
        }

    }
}



