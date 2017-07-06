using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// This is a UI object that I've created because I need the same functionality and behavior across
    /// all of our pages.
    /// This UI object provides the functionality to go between the pages of the carousel via buttons at the bottom of the page.
    /// </summary>
    class KeyPageNavigator : Grid {
        TapGestureRecognizer tapGesture;
        Image gotoVotingImgButton;
        Image goHomeImgButton;
        Image gotoCameraImgButton;
        Label categoryLabel;

        //StackLayout defaultNavigationButtons;
        //Grid defaultNavigationButtons;
        Color navigatorColor = Color.FromRgb(100, 100, 100);

        public KeyPageNavigator(string categoryDesc = "") {
            BackgroundColor = navigatorColor;
            //Padding = 10;

            gotoVotingImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.vote.png")
            };
            goHomeImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.home.png")
            };
            gotoCameraImgButton = new Image
            {
                Source = ImageSource.FromResource("ImageImprov.IconImages.camera.png"),
            };

            categoryLabel = new Label
            {
                Text = categoryDesc,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.Black,
                BackgroundColor = navigatorColor,
                LineBreakMode = LineBreakMode.WordWrap,
                FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
            };

            tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnClicked;
            gotoVotingImgButton.GestureRecognizers.Add(tapGesture);
            goHomeImgButton.GestureRecognizers.Add(tapGesture);
            gotoCameraImgButton.GestureRecognizers.Add(tapGesture);

            // ColumnSpacing = 1; RowSpacing = 1;
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(7, GridUnitType.Star) });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            Children.Add(gotoVotingImgButton, 0, 1);
            Children.Add(goHomeImgButton, 1, 1);
            Children.Add(gotoCameraImgButton, 2, 1);
            Children.Add(categoryLabel, 2, 2);

            // This object should always be created AFTER the judging page, so this should exist...
            if (GlobalStatusSingleton.ptrToJudgingPageLoadCategory != null) {
                GlobalStatusSingleton.ptrToJudgingPageLoadCategory += new CategoryLoadSuccessEventHandler(OnCategoryLoad);
            }
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == gotoVotingImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            } else if (sender == gotoCameraImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
                // want to go instantly to this...
                //((MainPageSwipeUI)Xamarin.Forms.Application.Current.MainPage).getCamera().takePictureP.Clicked;
            } else {
                // go home for default.
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHomePage();
            }
        }

        public virtual void OnCategoryLoad(object sender, EventArgs e) {
            categoryLabel.Text = GlobalSingletonHelpers.getUploadingCategoryDesc();
        }
    }
}
