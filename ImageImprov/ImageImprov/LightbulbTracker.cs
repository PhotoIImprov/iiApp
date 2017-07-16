using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// LightbulbTracker is used to track the number of lightbulbs you have earned today
    /// and display them graphically.
    /// </summary>
    class LightbulbTracker : ContentView {
        private const string UNLIT_STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward_inactive.png";
        private const string LIT_STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward.png";
        private const string UNLIT_10STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward_10_inactive.png";
        private const string LIT_10STAR_IMG = "ImageImprov.IconImages.ImageMetaIcons.reward_10.png";

        List<int> changePoints = new List<int>();

        public Grid myRow = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
        IList<Image> litStars = new List<Image>();
        IList<Image> unlitStars = new List<Image>();

        public static DateTime timeOfLastEarnedLightbulb;
        public static int todaysCount = 0;

        public LightbulbTracker() {
            myRow.SizeChanged += redrawImages;
            changePoints.Add(1);  // needed in case the user was playing across the midnight barrier.
            changePoints.Add(3);
            changePoints.Add(10);
            changePoints.Add(20);

            checkTimestamp();  // happens here rather than the app.load point as object does not exist then.
        }

        public void buildUI() {
            if (todaysCount < 3) {
                build3StarRow();
            } else if (todaysCount < 10) {
                build10StarRow();
            } else if (todaysCount < 20) {
                build20StarRow();
            } else {
                buildNStarRow(todaysCount);
            }
            Content = myRow;
        }

        private Image newStarImage(string resourceName) {
            return new Image { Source = ImageSource.FromResource(resourceName), };
        }

        private void changeLighting() {
            int starsToLight = todaysCount;
            int i = 0;
            while (starsToLight >= 10) {
                litStars[i].IsVisible = true;
                unlitStars[i].IsVisible = false;
                i++;
                starsToLight -= 10;
            }
            // there are now less than 10 stars to light left...
            for (int j=i; j< starsToLight+i; j++) {
                litStars[j].IsVisible = true;
                unlitStars[j].IsVisible = false;
            }
            for (int k=starsToLight+i; k<unlitStars.Count; k++) {
                litStars[k].IsVisible = false;
                unlitStars[k].IsVisible = true;
            }
        }

        // when incrementing, there's no need to change everything!
        private void changeLastBulb() {
            int bulbsLeft = todaysCount;
            int bulbIndex = 0;
            while (bulbsLeft > 10) {
                bulbIndex++;
                bulbsLeft -= 10;
            }
            bulbIndex += (bulbsLeft - 1);
            litStars[bulbIndex].IsVisible = true;
            unlitStars[bulbIndex].IsVisible = false;
        }

        /// <summary>
        /// BUilds the stars in the generic case
        /// </summary>
        /// <param name="numStars">Total number I have earned ceilinged to the nearest 10s digit.</param>
        /// <param name="startingX"></param>
        private void createStars(int numStars, int startingX) {
            int j = 0;
            while (numStars>10) {
                litStars.Add(newStarImage(LIT_10STAR_IMG));
                unlitStars.Add(newStarImage(UNLIT_10STAR_IMG));
                myRow.Children.Add(litStars[j], startingX + j, 0);  // col, row
                myRow.Children.Add(unlitStars[j], startingX + j, 0);  // col, row
                litStars[j].IsVisible = false;
                unlitStars[j].IsVisible = false;
                j++;
                numStars -= 10;
            }
            // num stars is the right count of stars left, but wrong for indexing.
            for (int k=j; k < numStars+j; k++) {
                litStars.Add(newStarImage(LIT_STAR_IMG));
                unlitStars.Add(newStarImage(UNLIT_STAR_IMG));
                myRow.Children.Add(litStars[k], startingX+k, 0);  // col, row
                myRow.Children.Add(unlitStars[k], startingX+k, 0);  // col, row
                litStars[k].IsVisible = false;
                unlitStars[k].IsVisible = false;
            }
        }

        private void build3StarRow() {
            myRow.ColumnDefinitions.Clear();
            for (int i=0;i<5;i++) {
                //portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                myRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            litStars.Clear();
            unlitStars.Clear();
            createStars(3, 0);
            changeLighting();
        }

        private void build10StarRow() {
            myRow.ColumnDefinitions.Clear();
            for (int i = 0; i < 10; i++) {
                //portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                myRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            litStars.Clear();
            unlitStars.Clear();
            createStars(10, 0);
            changeLighting();
        }

        private void build20StarRow() {
            myRow.ColumnDefinitions.Clear();
            for (int i = 0; i < 11; i++) {
                //portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                myRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            litStars.Clear();
            unlitStars.Clear();
            createStars(20, 0);
            changeLighting();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentStars">Should be today's star count.</param>
        private void buildNStarRow(int currentStars) {
            int numCols = ((int)(currentStars / 10))+10;
            myRow.ColumnDefinitions.Clear();
            for (int i = 0; i < numCols; i++) {
                //portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                myRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            litStars.Clear();
            unlitStars.Clear();
            // ceiling to the nearest 10s digit
            int allStars = (currentStars - (currentStars % 10)) + 10;
            createStars(allStars, 0);
            changeLighting();
        }

        private void checkTimestamp() {
            if (DateTime.Now.Date != timeOfLastEarnedLightbulb.Date) {
                // A new day. Reset!
                todaysCount = 0;
            }
            timeOfLastEarnedLightbulb = DateTime.Now;
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

        public void incrementLightbulbCounter() {
            checkTimestamp();
            todaysCount++;
            //buildUI();
            if ((changePoints.Contains<int>(todaysCount)) || (todaysCount%10 == 0)) {
                buildUI();
            } else {
                changeLastBulb();
            }
        }
    }
}
