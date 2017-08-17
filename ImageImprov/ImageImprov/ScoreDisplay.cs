using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// This is very similar to lightbulb tracker and used that as a starting point.
    /// Differences: Wanted to be able to control the image used as the background.
    ///     Did not want to show the unlit image, just lit to a certain number.
    /// </summary>
    class ScoreDisplay : ContentView {
        private const string UNLIT_STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward_inactive.png";
        private const string LIT_STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward.png";
        private const string LIT_10STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward_10.png";

        public Grid myRow = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
        IList<Image> litStars = new List<Image>();

        private int score;
        public int Score {
            get { return score; }
            set {
                score = value;
                buildNStarRow(score);
                Content = myRow;
            }
        }

        public ScoreDisplay(int score) {
            this.score = score;
            buildUI();
            myRow.SizeChanged += redrawImages;
        }

        public void buildUI() {
            buildNStarRow(score);
            Content = myRow;
        }

        private Image newStarImage(string resourceName) {
            return new Image { Source = ImageSource.FromResource(resourceName), Margin = 2, };
        }

        /// <summary>
        /// BUilds the stars in the generic case
        /// </summary>
        /// <param name="numStars">Total number I want to display</param>
        /// <param name="startingX"></param>
        private void createStars(int numStars, int startingX) {
            if (numStars == 0) {
                litStars.Add(newStarImage(UNLIT_STAR_IMG));
                myRow.Children.Add(litStars[0], 0, 0);
                litStars[0].IsVisible = true;
            } else {
                int j = 0;
                while (numStars > 10) {
                    litStars.Add(newStarImage(LIT_10STAR_IMG));
                    myRow.Children.Add(litStars[j], startingX + j, 0);  // col, row
                    litStars[j].IsVisible = true;
                    j++;
                    numStars -= 10;
                }
                // num stars is the right count of stars left, but wrong for indexing.
                for (int k = j; k < numStars + j; k++) {
                    litStars.Add(newStarImage(LIT_STAR_IMG));
                    myRow.Children.Add(litStars[k], startingX + k, 0);  // col, row
                    litStars[k].IsVisible = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentStars">What number to display</param>
        private void buildNStarRow(int currentStars) {
            int numCols = ((int)(currentStars / 10)) + (currentStars%10);
            if (numCols == 0) numCols = 1; // need to draw 0!
            myRow.ColumnDefinitions.Clear();
            for (int i = 0; i < numCols; i++) {
                //portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                myRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            litStars.Clear();
            createStars(currentStars, 0);
        }

        double lastRedrawnWidth = -2.0;
        public void redrawImages(object sender, EventArgs args) {
            //if ((Width > 0) && (Width != lastRedrawnWidth)) {
            // don't want spurious redraw spamming from grid shrinking and growing my draw area.
            if ((Width > 0) && (Width > lastRedrawnWidth)) {
                buildUI();
                lastRedrawnWidth = Width;
            }
        }
    }
}
