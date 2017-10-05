using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    public class BadgesPage : ContentView {
        // Once we have alot of badges will want to upgrade this to a ListView.
        // Till then, this works...
        Grid portraitView;

        iiBitmapView upload1;
        Label upload1Label = new Label {
            Text = "Upload an open category photo",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };

        iiBitmapView upload7;
        Label upload7Label = new Label {
            Text = "Upload an open category photo\n 7 days in a row",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };

        iiBitmapView upload30;
        Label upload30Label = new Label {
            Text = "Upload an open category photo\n 30 days in a row",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };

        iiBitmapView upload100;
        Label upload100Label = new Label {
            Text = "Upload an open category photo\n 100 days in a row",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };

        iiBitmapView vote30;
        Label vote30Label = new Label {
            Text = "Vote 30 days running",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };
        iiBitmapView vote100;
        Label vote100Label = new Label {
            Text = "Vote 100 days running",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            TextColor = Color.Black,
        };

        public BadgesPage() {
            buildUI();
        }

        private int buildUI() {
            if (portraitView == null) {
                // yes, these are unbalanced for a reason.
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                //portraitView.SizeChanged += OnPortraitViewSizeChanged;
                for (int i = 0; i < 16; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });
            } else {
                // flush the old children.
                portraitView.Children.Clear();
            }
            if (upload1 != null) {
                portraitView.Children.Add(upload1, 0, 0);
                portraitView.Children.Add(upload7, 0, 1);
                portraitView.Children.Add(upload30, 0, 2);
                portraitView.Children.Add(upload100, 0, 3);

                portraitView.Children.Add(vote30, 0, 5);
                portraitView.Children.Add(vote100, 0, 6);
            }
            portraitView.Children.Add(upload1Label, 1, 0);
            portraitView.Children.Add(upload7Label, 1, 1);
            portraitView.Children.Add(upload30Label, 1, 2);
            portraitView.Children.Add(upload100Label, 1, 3);

            portraitView.Children.Add(vote30Label, 1, 5);
            portraitView.Children.Add(vote100Label, 1, 6);

            Content = portraitView;
            return 1;
        }

        public void SetBadgesData(BadgesResponseJSON badges) {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            if (badges.firstphoto == true) {
                upload1 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedal_01.png", assembly));
            } else {
                upload1 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedals_inactive.png", assembly));
            }
            if (badges.upload7 == true) {
                upload7 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedal_02.png", assembly));
            } else {
                upload7 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedals_inactive.png", assembly));
            }
            if (badges.upload30 == true) {
                upload30 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedal_03.png", assembly));
            } else {
                upload30 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedals_inactive.png", assembly));
            }
            if (badges.upload100 == true) {
                upload100 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedal_04.png", assembly));
            } else {
                upload100 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.uploadMedals_inactive.png", assembly));
            }

            if (badges.vote30 == true) {
                vote30 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.voteMedal_01.png", assembly));
            } else {
                vote30 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.voteMedal_inactive.png", assembly));
            }
            if (badges.vote100 == true) {
                vote100 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.voteMedal_01.png", assembly));
            } else {
                vote100 = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.Medals.voteMedal_inactive.png", assembly));
            }
            Device.BeginInvokeOnMainThread(() => {
                buildUI();
            });
        }
    }
}
