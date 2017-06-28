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
        //StackLayout defaultNavigationButtons;
        //Grid defaultNavigationButtons;

        public KeyPageNavigator(bool horizontalOrientation=true) {
            BackgroundColor = GlobalStatusSingleton.backgroundColor;
            Padding = 10;

            gotoVotingImgButton = new Image {
                Source = ImageSource.FromResource("ImageImprov.IconImages.vote.png")
            };
            goHomeImgButton = new Image {
                Source = ImageSource.FromResource("ImageImprov.IconImages.home.png")
            };
            gotoCameraImgButton = new Image {
                Source = ImageSource.FromResource("ImageImprov.IconImages.camera.png")
            };

            tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnClicked;
            gotoVotingImgButton.GestureRecognizers.Add(tapGesture);
            goHomeImgButton.GestureRecognizers.Add(tapGesture);
            gotoCameraImgButton.GestureRecognizers.Add(tapGesture);

            if (horizontalOrientation) {
                // ColumnSpacing = 1; RowSpacing = 1;
                ColumnDefinitions.Add(new ColumnDefinition());
                ColumnDefinitions.Add(new ColumnDefinition());
                ColumnDefinitions.Add(new ColumnDefinition());
                Children.Add(gotoVotingImgButton, 0, 0);
                Children.Add(goHomeImgButton, 1, 0);
                Children.Add(gotoCameraImgButton, 2, 0);
            } else {
                // ColumnSpacing = 1; RowSpacing = 1;
                RowDefinitions.Add(new RowDefinition());
                RowDefinitions.Add(new RowDefinition());
                RowDefinitions.Add(new RowDefinition());
                Children.Add(gotoVotingImgButton, 0, 0);
                Children.Add(goHomeImgButton, 0, 1);
                Children.Add(gotoCameraImgButton, 0, 2);
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

    }
}
