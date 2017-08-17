using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Xamarin.Forms;

namespace ImageImprov {
    // has to be a contentpage to be modally pushed/popped.
    public class VotingInstructionsOverlay : ContentPage {
        Assembly assembly = null;
        Grid portraitView;
        IList<iiBitmapView> imgs;
        IList<iiBitmapView> voteBoxes;
        IList<iiBitmapView> rankImages;

        bool active = true;
        EventHandler animate;

        public VotingInstructionsOverlay(IList<iiBitmapView> imgs) {
            this.imgs = imgs;
            this.animate += new EventHandler(AnimationEvent);
            buildUI();
        }

        private int buildUI() {
            this.BackgroundColor = Color.Transparent;
            
            assembly = this.GetType().GetTypeInfo().Assembly;
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 0, RowSpacing = 0, BackgroundColor = Color.White, };
                for (int i = 0; i < 20; i++) {  // we're a whole page modal
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                for (int i = 0; i < 12; i++) {  // we're a whole page modal
                    portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
            }
            iiBitmapView backButton = new iiBitmapView {
                Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center,
                Margin = 2,
            };
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.Tapped += OnCloseTapped;
            backButton.GestureRecognizers.Add(tap);
            portraitView.Children.Add(backButton, 0, 2);
            Grid.SetRowSpan(backButton, 2);

            buildRankImages();
            addImage(0, 1, 5);
            addImage(1, 7, 5);
            addImage(2, 1, 10);
            addImage(3, 7, 10);

            Label instructionsOne = new Label {
                Text = "Pick your favorites and earn lightbulbs!\n" 
                +"Tap the vote box starting with your favorite\n"
                +"Tap an image to zoom in, like, tag, or flag\n"
                +"Click play to enter your own photos!",
                TextColor = Color.Black, Opacity = 1,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.Center,
            };
            portraitView.Children.Add(instructionsOne, 0, 15);
            Grid.SetColumnSpan(instructionsOne, 12);
            Grid.SetRowSpan(instructionsOne, 5);

            portraitView.Opacity = .8;
            Content = portraitView;

            if (animate != null) {
                animate(this, new EventArgs());
            }

            return 1;
        }

        private void addImage(int index, int col, int row) {
            portraitView.Children.Add(imgs[index], col, row);
            Grid.SetColumnSpan(imgs[index], 4);
            Grid.SetRowSpan(imgs[index], 4);
            portraitView.Children.Add(voteBoxes[index], col + 2, row + 2);
            Grid.SetRowSpan(voteBoxes[index], 2);
            Grid.SetColumnSpan(voteBoxes[index], 2);
        }

        public void OnCloseTapped(object sender, EventArgs args) {
            active = false;
            this.Navigation.PopModalAsync();
        }

        protected void buildRankImages() {
            voteBoxes = new List<iiBitmapView>();
            for (int i = 0; i < 4; i++) {
                iiBitmapView vBox = new iiBitmapView {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(JudgingContentPage.VOTE_BOX_FILENAME, assembly),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MinimumWidthRequest = 60,
                    Margin = 2,
                };
                voteBoxes.Add(vBox);
            }

            rankImages = new List<iiBitmapView>();
            foreach (string filename in JudgingContentPage.rankFilenames) {
                iiBitmapView img = new iiBitmapView {
                    Bitmap = GlobalSingletonHelpers.loadSKBitmapFromResourceName(filename, assembly),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    MinimumWidthRequest = 60,
                    Margin = 2,
                };
                //img.GestureRecognizers.Add(tap);
                rankImages.Add(img);
            }
        }

        public async void AnimationEvent(object sender, EventArgs args) {
            portraitView.Children.Add(rankImages[0], 3, 7);
            portraitView.Children.Add(rankImages[1], 9, 7);
            portraitView.Children.Add(rankImages[2], 3, 12);
            portraitView.Children.Add(rankImages[3], 9, 12);
            while (this.active) {
                for (int i = 0; i < 4; i++) {
                    rankImages[i].Opacity = 0.0;
                    Grid.SetRowSpan(rankImages[i], 2);
                    Grid.SetColumnSpan(rankImages[i], 2);
                }
                await rankImages[0].FadeTo(1.0, 2000);
                await Task.Delay(100);
                await rankImages[1].FadeTo(1.0, 2000);
                await Task.Delay(100);
                rankImages[2].FadeTo(1.0, 2000);
                await rankImages[3].FadeTo(1.0, 2000);
                await Task.Delay(250);
            }
        }
    }
}
