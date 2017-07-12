using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Xamarin.Forms;
using SkiaSharp;

namespace ImageImprov {
    /// <summary>
    /// Cheats for now, I grabs the files from the local directory, rather than the server.
    /// I also assume that the count is correct.
    /// This is error prone as it misses imgs if the user change filenames of their pictures.
    /// Also, relies on the sleep/wake counter being accurate (which it is not if the app was
    /// deleted and reinstalled).
    /// 
    /// Went with a view rather than a ContentPage as this widget will not sit on the carousel.
    /// by that I mean that you won't be able to swipe to this page.
    /// </summary>
    class MySubmissionsPage : ContentView {
        public const string IMG_FILENAME_PREFIX = "ImageImprov_";

        StackLayout submissionStack = new StackLayout {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 2.0,
        };
        ScrollView scroller = new ScrollView();

        KeyPageNavigator defaultNavigationButtons;
        Grid portraitView;

        public MySubmissionsPage() {
            submissionStack.SizeChanged += redrawImages;
            buildMyImages();
            buildUI();
        }

        IList<Image> myImages = new List<Image>();

        protected void buildMyImages() {
            myImages.Clear();
            // img tracker is 1-indexed...
            //for (int i=1;i<=GlobalStatusSingleton.imgsTakenTracker;i++) {
            //IList<string> filenames = DependencyService.Get<IFileServices>().getImageImprovFileNames();
            IList<string> filenames = PlatformSpecificCalls.getImageImprovFileNames();
            foreach (string f in filenames) {
                byte[] raw = PlatformSpecificCalls.loadImageBytes(f);
                if (raw != null) {
                    Image final = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(raw);
                    if (final != null) {
                        myImages.Add(final);
                    }
                }
            }
        }

        protected void buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            build3Across();

            if (defaultNavigationButtons == null) {
                defaultNavigationButtons = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            }
            scroller.Content = submissionStack;
            portraitView.Children.Add(scroller, 0, 1);
            portraitView.Children.Add(defaultNavigationButtons, 0, 2);
            Content = portraitView;
        }

        private void build3Across() {
            for (int j = 0; j < myImages.Count; j += 3) {
                // does not contrain to screen width.
                // of course not.  It's a STACK!!  Solution: set the suggested width of the images!
                StackLayout leaderRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,

                };
                myImages[j].WidthRequest = (Width / 3.01);
                leaderRow.Children.Add(myImages[j]);
                if ((j + 1) < myImages.Count) {
                    myImages[j + 1].WidthRequest = (Width / 3.01);
                    leaderRow.Children.Add(myImages[j + 1]);

                    if ((j + 2) < myImages.Count) {
                        myImages[j + 2].WidthRequest = (Width / 3.01);
                        leaderRow.Children.Add(myImages[j + 2]);
                    }
                }
                submissionStack.Children.Add(leaderRow);
            }
        }

        double lastRedrawnWidth = -2.0;
        public void redrawImages(object sender, EventArgs args) {
            if ((Width > 0) && (Width != lastRedrawnWidth)) {
                submissionStack.Children.Clear();
                lastRedrawnWidth = Width;
                buildUI();
            }
        }

        public void OnPhotoSubmit(object sender, EventArgs args) {
            buildMyImages();
            buildUI();
        }
    }
}
