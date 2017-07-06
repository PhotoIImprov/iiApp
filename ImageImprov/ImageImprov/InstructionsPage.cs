using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    class InstructionsPage : ContentView {
        Grid portraitView;
        KeyPageNavigator defaultNavigationButtonsP = null;

        Image gotoVotingImgButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.vote.png") };
        Image goHomeImgButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.home.png") };
        Image gotoCameraImgButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.camera.png") };
        Image gotoLeaderboardImgButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.leaderboard.png") };
        Image gotoSettingsImgButton = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.settings.png") };

        Label votingLabelP = new Label {
            Text = "Goto the voting page.\nVote early and vote often!\nThe more votes the better!",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Color.Black,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode=LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };

        Label generalVoting = new Label
        {
            Text = " * Tap the images from favorite to least.\n * Tap a second time to undo a selection.\n" +
                " * Your vote is auto submitted when making your third image selection.\n * Double tap to LIKE, flag as offensive, or add a custom tag! ",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Color.Black,
            Margin = 4,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            //FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };

        Label homeLabelP = new Label
        {
            Text = "Goes to the home screen",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Color.Black,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };

        Label cameraLabelP = new Label
        {
            Text = "Goto the camera page.\nEnter contests here!\nSquare images only.",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Color.Black,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };
        Label leaderboardLabelP = new Label
        {
            Text = "Goto the leaderboard!",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Color.Black,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };
        Label settingsLabelP = new Label
        {
            Text = "A settings page. Yawn.",
            HorizontalOptions = LayoutOptions.StartAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            TextColor = Color.Black,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
        };

        public InstructionsPage() {
            buildUI();
        }

        public int buildUI() {
            int res = 0;
            Device.BeginInvokeOnMainThread(() => {
                DateTime timeForThread = DateTime.Now;
                res = buildPortraitView();
                if (res == 1) {
                    Content = portraitView;
                }
            });
            return res;
        }

        public int buildPortraitView() {
            int result = 1;
            // all my elements are already members...
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 3, BackgroundColor=GlobalStatusSingleton.backgroundColor };

                for (int i = 0; i < 20; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
            }

            if (defaultNavigationButtonsP == null) {
                defaultNavigationButtonsP = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            }
            
            portraitView.Children.Add(gotoVotingImgButton, 0, 2);
            //Grid.SetRowSpan(gotoVotingImgButton, 2);
            portraitView.Children.Add(votingLabelP, 1, 1);
            Grid.SetRowSpan(votingLabelP, 3);
            portraitView.Children.Add(generalVoting, 0, 4);
            Grid.SetColumnSpan(generalVoting, 2);
            Grid.SetRowSpan(generalVoting, 5);
            portraitView.Children.Add(goHomeImgButton, 0, 9);
            portraitView.Children.Add(homeLabelP, 1, 9);
            Grid.SetRowSpan(homeLabelP, 3);
            portraitView.Children.Add(gotoCameraImgButton, 0, 11);
            //Grid.SetRowSpan(gotoCameraImgButton, 2);
            portraitView.Children.Add(cameraLabelP, 1, 10);
            Grid.SetRowSpan(cameraLabelP, 3);

            portraitView.Children.Add(gotoLeaderboardImgButton, 0, 13);
            portraitView.Children.Add(leaderboardLabelP, 1, 13);
            portraitView.Children.Add(gotoSettingsImgButton, 0, 14);
            portraitView.Children.Add(settingsLabelP, 1, 14);

            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetColumnSpan(defaultNavigationButtonsP, 2);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);

            portraitView.SizeChanged += OnPortraitViewSizeChanged;
            return result;
        }

        protected void OnPortraitViewSizeChanged(Object sender, EventArgs args) {
            View view = (View)sender;
            // view height returned will be for the full size of the grid, not the cell!
            GlobalSingletonHelpers.fixLabelHeight(generalVoting, view.Width, view.Height*(4.0/20.0),GlobalStatusSingleton.MIN_FONT_SIZE,24);
        }
    }
}
