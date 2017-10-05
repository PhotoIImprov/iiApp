using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using SkiaSharp;

namespace ImageImprov {
    // This controls profile pic, friends, lightbulbs, etc.
    public class UpperProfileSection : ContentView {
        const string MOST_BULBS = "Most bulbs in one day: ";

        Grid portraitView;

        SKBitmap profilePicBitmap;
        iiBitmapView profilePic = new iiBitmapView() { HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.FillAndExpand };
        iiBitmapView lightbulbs;
        Label lightbulbCount = new Label {
            Text = "Loading",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
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

        Label mostBulbsInOneDay = new Label {
            Text = MOST_BULBS + "Loading",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
            //IsVisible = false,
        };


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

            portraitView.Children.Add(profilePic, 0, 0);
            Grid.SetRowSpan(profilePic, 4);
            Grid.SetColumnSpan(profilePic, 2);

            StackLayout sl = new StackLayout {
                Orientation = StackOrientation.Horizontal,
                Children = { lightbulbs, lightbulbCount, },
            };
            portraitView.Children.Add(usernameLabel, 2, 0);
            Grid.SetColumnSpan(usernameLabel, 2);
            portraitView.Children.Add(sl, 2, 1);
            Grid.SetColumnSpan(sl, 2);
            portraitView.Children.Add(mostBulbsInOneDay, 2, 2);
            Grid.SetColumnSpan(mostBulbsInOneDay, 2);
            portraitView.Children.Add(friendInfo, 2, 3);
            Grid.SetColumnSpan(friendInfo, 2);

            Content = portraitView;
            return 1;
        }

        public async void SetBadgesData(BadgesResponseJSON badges) {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            usernameLabel.Text = GlobalStatusSingleton.username;
            lightbulbCount.Text = badges.totalBulbs.ToString();
            mostBulbsInOneDay.Text = MOST_BULBS + badges.maxDailyBulbs.ToString();

            if (badges.pid != -1) {
                profilePicBitmap = await GlobalSingletonHelpers.loadBitmapAsync(assembly, badges.pid);
                profilePic.Bitmap = profilePicBitmap;
            }
        }

    }
}



