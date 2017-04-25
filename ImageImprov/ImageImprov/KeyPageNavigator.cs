﻿using System;
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

        public KeyPageNavigator() {
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

            // ColumnSpacing = 1; RowSpacing = 1;
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
            Children.Add(gotoVotingImgButton, 0, 0);
            Children.Add(goHomeImgButton, 1, 0);
            Children.Add(gotoCameraImgButton, 2, 0);
        }

        public void OnClicked(object sender, EventArgs e) {
            // I need to know which image.  
            // From there I vote... (?)
            if (sender == gotoVotingImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoJudgingPage();
            } else if (sender == gotoCameraImgButton) {
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoCameraPage();
            } else {
                // go home for default.
                ((IProvideNavigation)Xamarin.Forms.Application.Current.MainPage).gotoHomePage();
            }
        }

    }
}
