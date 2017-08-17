using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using System.Reflection;

namespace ImageImprov {
    /// <summary>
    /// Needs to be a page so it can be pushed and popped.
    /// Don't want to push/pop anymore. Problem is I don't want it to be MODAL (ie, want user to be able to navigate away and return).
    /// </summary>
    public class ZoomPage : ContentView {
        /// <summary>
        /// Tracks whether to display a ballot or the meta data for an image.
        /// </summary>
        Grid zoomView;
        public iiBitmapView MainImage { get; set; }
        iiBitmapView unlikedImg;
        iiBitmapView unflaggedImg;
        iiBitmapView likedImg;
        iiBitmapView flaggedImg;

        public ILeaveZoomCallback PreviousContent { get; set; }
        Button backButton = null;

        // this points back to the original calling ballot.
        // this MUST be set by the pushing page!!!
        public BallotCandidateJSON ActiveMetaBallot { get; set; }
        //
        //   END Variables related/needed for double clicking an image
        // 
        Assembly assembly = null;

        public ZoomPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;

            buildMetaButtons();
            //buildZoomView();
            //Content = zoomView;
        }

        private void buildMetaButtons() {
            unlikedImg = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.unliked.png", assembly));
            unflaggedImg = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.unflagged.png", assembly));
            likedImg = new iiBitmapView {
                Bitmap = (GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.liked.png", assembly)),
                IsVisible = false
            };
            flaggedImg = new iiBitmapView {
                Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.ImageMetaIcons.flagged.png", assembly),
                IsVisible = false
            };

            TapGestureRecognizer ulTap = new TapGestureRecognizer();
            ulTap.Tapped += (sender, args) => {
                ActiveMetaBallot.isLiked = true;
                unlikedImg.IsVisible = false;
                likedImg.IsVisible = true;
            };
            unlikedImg.GestureRecognizers.Add(ulTap);

            TapGestureRecognizer lTap = new TapGestureRecognizer();
            lTap.Tapped += (sender, args) => {
                ActiveMetaBallot.isLiked = false;
                likedImg.IsVisible = false;
                unlikedImg.IsVisible = true;
            };
            likedImg.GestureRecognizers.Add(lTap);

            TapGestureRecognizer ufTap = new TapGestureRecognizer();
            ufTap.Tapped += (sender, args) => {
                ActiveMetaBallot.isFlagged = true;
                unflaggedImg.IsVisible = false;
                flaggedImg.IsVisible = true;
            };
            unflaggedImg.GestureRecognizers.Add(ufTap);

            TapGestureRecognizer fTap = new TapGestureRecognizer();
            fTap.Tapped += (sender, args) => {
                ActiveMetaBallot.isFlagged = false;
                flaggedImg.IsVisible = false;
                unflaggedImg.IsVisible = true;
            };
            flaggedImg.GestureRecognizers.Add(fTap);

            backButton = new Button {
                //Text = buttonText,
                Text = "Back",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                //VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            };
            backButton.Clicked += (sender, args) => {
                // Need to return to caller instead.
                //MasterPage mp = ((MasterPage)Application.Current.MainPage);
                //await mp.Navigation.PopModalAsync();
                PreviousContent.returnToCaller();
            };
        }

        public int buildZoomView() {
            int result = 1;
            if (zoomView == null) {
                zoomView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < 16; i++) {
                    zoomView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                zoomView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                zoomView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                buildMetaButtons();
            } else {
                unlikedImg.IsVisible = !ActiveMetaBallot.isLiked;
                likedImg.IsVisible = ActiveMetaBallot.isLiked;
                unflaggedImg.IsVisible = !ActiveMetaBallot.isFlagged;
                flaggedImg.IsVisible = ActiveMetaBallot.isFlagged;
            }
            MainImage.HorizontalOptions = LayoutOptions.FillAndExpand;
            //mainImage.Aspect = Aspect.AspectFill;  this is an old image setting, not a iiBitmapView setting

            zoomView.Children.Clear();
            zoomView.Children.Add(MainImage, 0, 0);
            Grid.SetRowSpan(MainImage, 12);
            Grid.SetColumnSpan(MainImage, 2);
            zoomView.Children.Add(unlikedImg, 0, 12);
            zoomView.Children.Add(likedImg, 0, 12);
            zoomView.Children.Add(unflaggedImg, 1, 12);
            zoomView.Children.Add(flaggedImg, 1, 12);
            zoomView.Children.Add(backButton, 0, 14);

            Grid.SetColumnSpan(backButton, 2);
            Grid.SetRowSpan(backButton, 2);

            Content = zoomView;
            return result;
            //return Content;
        }

        /// <summary>
        /// Builds the zoom page when this is a zoom by the user on their own image.
        /// </summary>
        /// <param name="photoMeta"></param>
        /// <returns></returns>
        public int buildZoomFromUserPhotoMeta(PhotoMetaJSON photoMeta) {
            int result = 1;
            if (zoomView == null) {
                zoomView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < 16; i++) {
                    zoomView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                zoomView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                zoomView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                //buildMetaButtons();   // no like/flag your own images.
            }

            MainImage.HorizontalOptions = LayoutOptions.FillAndExpand;
            //MainImage.Margin = 0;
            //mainImage.Aspect = Aspect.AspectFill;  this is an old image setting, not a iiBitmapView setting

            zoomView.Children.Clear();
            zoomView.Children.Add(MainImage, 0, 0);
            Grid.SetRowSpan(MainImage, 12);
            Grid.SetColumnSpan(MainImage, 2);

            //// Unique to Photo Meta
            // Lightbulbs for likes and score!
            if (photoMeta != null) {
                ScoreDisplay likeScore = new ScoreDisplay((int)photoMeta.likes);
                StackLayout likeRow = new StackLayout {
                    HorizontalOptions = LayoutOptions.Center,
                    Orientation = StackOrientation.Horizontal,
                    Children = { new Label { Text = "Likes: ", TextColor = Color.Black, FontSize = 18, }, likeScore, }
                };
                ScoreDisplay score = new ScoreDisplay((int)photoMeta.score);
                StackLayout scoreRow = new StackLayout {
                    HorizontalOptions = LayoutOptions.Center,
                    Orientation = StackOrientation.Horizontal,
                    Children = { new Label { Text = "Score: ", TextColor = Color.Black, FontSize = 18, }, score, }
                };
                zoomView.Children.Add(likeRow, 0, 12);
                Grid.SetColumnSpan(likeRow, 2);
                zoomView.Children.Add(scoreRow, 0, 13);
                Grid.SetColumnSpan(scoreRow, 2);
                //Label temp = new Label { Text = "Votes:" + photoMeta.votes, TextColor = Color.Black };
                //zoomView.Children.Add(temp, 1, 13);
            }
            //// Unique to Photo Meta

            zoomView.Children.Add(backButton, 0, 14);
            Grid.SetColumnSpan(backButton, 2);
            Grid.SetRowSpan(backButton, 2);

            Content = zoomView;
            return result;
        }
    }
}
