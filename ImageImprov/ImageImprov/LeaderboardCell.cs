using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using System.Reflection;

namespace ImageImprov {
    /// <summary>
    /// Provides the visual representation of the leaderboard.
    /// </summary>
    class LeaderboardCell : ViewCell {
        Grid myView;
        Label categoryDescription;
        iiBitmapView img0;
        iiBitmapView img1;
        iiBitmapView img2;
        iiBitmapView img3;
        iiBitmapView img4;
        iiBitmapView img5;
        iiBitmapView img6;
        iiBitmapView img7;
        iiBitmapView img8;

        public LeaderboardCell() {
            double gridHeightForImages = 2;
            if (Device.Idiom == TargetIdiom.Tablet) {
                gridHeightForImages = 4;
            }
            myView = new Grid { ColumnSpacing = 1, RowSpacing = 1, };
            myView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            myView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gridHeightForImages, GridUnitType.Star) });
            myView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gridHeightForImages, GridUnitType.Star) });
            myView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(gridHeightForImages, GridUnitType.Star) });
            myView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(.25, GridUnitType.Star) });
            myView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            myView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            myView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            categoryDescription = new Label {
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
                TextColor = Color.White,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                //WidthRequest = Width,
                MinimumHeightRequest = Height / 15.0,
                FontSize = 30, // program later
            };
            //GlobalSingletonHelpers.fixLabelHeight(categoryDescription, categoryDescription.Width, categoryDescription.Height, 30);

            img0 = new iiBitmapView();
            img1 = new iiBitmapView();
            img2 = new iiBitmapView();
            img3 = new iiBitmapView();
            img4 = new iiBitmapView();
            img5 = new iiBitmapView();
            img6 = new iiBitmapView();
            img7 = new iiBitmapView();
            img8 = new iiBitmapView();

            categoryDescription.SetBinding(Label.TextProperty, "title");
            img0.SetBinding(iiBitmapView.BitmapProperty, "bitmap0");
            img1.SetBinding(iiBitmapView.BitmapProperty, "bitmap1");
            img2.SetBinding(iiBitmapView.BitmapProperty, "bitmap2");
            img3.SetBinding(iiBitmapView.BitmapProperty, "bitmap3");
            img4.SetBinding(iiBitmapView.BitmapProperty, "bitmap4");
            img5.SetBinding(iiBitmapView.BitmapProperty, "bitmap5");
            img6.SetBinding(iiBitmapView.BitmapProperty, "bitmap6");
            img7.SetBinding(iiBitmapView.BitmapProperty, "bitmap7");
            img8.SetBinding(iiBitmapView.BitmapProperty, "bitmap8");

            TapGestureRecognizer imgTapped = new TapGestureRecognizer();
            imgTapped.Tapped += OnImgTapped;
            img0.GestureRecognizers.Add(imgTapped);
            img1.GestureRecognizers.Add(imgTapped);
            img2.GestureRecognizers.Add(imgTapped);
            img3.GestureRecognizers.Add(imgTapped);
            img4.GestureRecognizers.Add(imgTapped);
            img5.GestureRecognizers.Add(imgTapped);
            img6.GestureRecognizers.Add(imgTapped);
            img7.GestureRecognizers.Add(imgTapped);
            img8.GestureRecognizers.Add(imgTapped);

            myView.Children.Add(categoryDescription, 0, 0);
            Grid.SetColumnSpan(categoryDescription, 3);
            myView.Children.Add(img0, 0, 1);
            myView.Children.Add(img1, 1, 1);
            myView.Children.Add(img2, 2, 1);
            myView.Children.Add(img3, 0, 2);
            myView.Children.Add(img4, 1, 2);
            myView.Children.Add(img5, 2, 2);
            myView.Children.Add(img6, 0, 3);
            myView.Children.Add(img7, 1, 3);
            myView.Children.Add(img8, 2, 3);

            this.View = myView;
        }

        public void OnImgTapped(object sender, EventArgs args) {
            if (((iiBitmapView)sender) != null) {
                MasterPage m1 = ((MasterPage)Application.Current.MainPage);
                MainPageSwipeUI mp = m1.thePages;
                m1.zoomPage.ActiveMetaBallot = new BallotCandidateJSON(); // nada for now.

                iiBitmapView taggedImg = new iiBitmapView {
                    Bitmap = ((iiBitmapView)sender).Bitmap.Copy()
                };
                m1.zoomPage.MainImage = taggedImg;
                m1.zoomPage.buildZoomView();
                //mp.zoomPage.PreviousContent = mp.thePages.leaderboardPage;
                //m1.zoomPage.PreviousContent = mp.leaderboardPage;
                Device.BeginInvokeOnMainThread(() => {
                    //mp.thePages.leaderboardPage.Content = mp.zoomPage.Content;
                    //mp.leaderboardPage.Content = m1.zoomPage.Content;
                    //mp.leaderboardPage.Content = m1.zoomPage;
                    m1.gotoZoomPage();
                });
            }
        }
    }
}