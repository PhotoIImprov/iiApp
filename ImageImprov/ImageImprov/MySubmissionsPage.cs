using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
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
    public class MySubmissionsPage : ContentView {
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

            // right now, the next two lines take 35 secs on boot.

            buildMyImages();
            Debug.WriteLine("DHB:MySubmissionsPage:MySubmissionsPage async test");
            // wait till the redraw to call this. due to the resize...
            buildUI();
            
            //buildLoadingUI();
        }

        private const double SCALE_CHANGE = 3.01;

        IList<Image> myImages = new List<Image>();

        protected async void buildMyImages() {
            Debug.WriteLine("DHB:MySubmissionsPage:buildMyImages: memory:" + PlatformSpecificCalls.GetMemoryStatus());
            //var t = await Task.Run(() => {  see if this solves my load time issue
            await Task.Run(() => {
                myImages.Clear();
                // img tracker is 1-indexed...
                //for (int i=1;i<=GlobalStatusSingleton.imgsTakenTracker;i++) {
                //IList<string> filenames = DependencyService.Get<IFileServices>().getImageImprovFileNames();
                //if (Width > -1) { // don't bother unless we are live... as the memory usage is not pretty.
                    IList<string> filenames = PlatformSpecificCalls.getImageImprovFileNames();
                    foreach (string f in filenames) {
                        byte[] raw = PlatformSpecificCalls.loadImageBytes(f);
                        if (raw != null) {
                            // height set to width as the images should be square and we want them sized as a function of the width.
                            //Image final = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(raw, ExifLib.ExifOrientation.Undefined, (int)(Width / SCALE_CHANGE), (int)(Width / SCALE_CHANGE));
                            Image final = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(raw, ExifLib.ExifOrientation.Undefined, 720, 720);
                            if (final != null) {
                                myImages.Add(final);
                            }
                        }
                        Debug.WriteLine("DHB:MySubmissionsPage:buildMyImages memory diagnostics:");
                        Debug.WriteLine("DHB:MySubmissionsPage:buildMyImages: memory:" + PlatformSpecificCalls.GetMemoryStatus());
                    }
                //}
            });
            //t.Wait();  // what if we don't wait?  never loads.

            Debug.WriteLine("DHB:MySubmissionsPage:buildMyImages startup done.");
            Debug.WriteLine("DHB:MySubmissionsPage:buildMyImages async test");
            //Debug.WriteLine("DHB:MySubmissionsPage:buildMyImages: memory:" + PlatformSpecificCalls.GetMemoryStatus());
        }

        protected void buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                /* This differs from Leaderboard page. This crashes, leaderboard doesn't, so try it the leaderboard way...
                 * no joy.
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
                */
                for (int i=0; i<20; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            Debug.WriteLine("DHB:MySubmissionsPage:buildUI: pre3 across memory:" + PlatformSpecificCalls.GetMemoryStatus());
            build3Across();
            Debug.WriteLine("DHB:MySubmissionsPage:buildUI: post3across memory:" + PlatformSpecificCalls.GetMemoryStatus());

            if (defaultNavigationButtons == null) {
                defaultNavigationButtons = new KeyPageNavigator(GlobalSingletonHelpers.getUploadingCategoryDesc()) { ColumnSpacing = 1, RowSpacing = 1 };
            }
            scroller.Content = submissionStack;

            Label titleBar = new Label {
                Text = "My submissions:",
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
                TextColor = Color.White,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            GlobalSingletonHelpers.fixLabelHeight(titleBar, Width, Height / 10.0, 20);
            Debug.WriteLine("DHB:MySubmissionsPage:buildUI: post scroller set memory:" + PlatformSpecificCalls.GetMemoryStatus());
            portraitView.Children.Add(titleBar, 0, 2);
            //Grid.SetRowSpan(titleBar, 2);

            portraitView.Children.Add(scroller, 0, 4);
            Grid.SetRowSpan(scroller, 14);  // switching order did not help...
            //portraitView.Children.Add(submissionStack, 0, 2); doing no scroller did not help
            portraitView.Children.Add(defaultNavigationButtons, 0, 18);
            Grid.SetRowSpan(defaultNavigationButtons, 2);
            Content = portraitView;
            Debug.WriteLine("DHB:MySubmissionsPage:buildUI: post content set memory:" + PlatformSpecificCalls.GetMemoryStatus());
        }

        private void build3Across() {
            submissionStack.Children.Clear();
            for (int j = 0; j < myImages.Count; j += 3) {
                // does not contrain to screen width.
                // of course not.  It's a STACK!!  Solution: set the suggested width of the images!
                StackLayout leaderRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,

                };
                myImages[j].MinimumWidthRequest = (double)((int)(Width / 3.01));
                myImages[j].HeightRequest = (double)((int)(Width / 3.01));
                leaderRow.Children.Add(myImages[j]);
                if ((j + 1) < myImages.Count) {
                    myImages[j + 1].WidthRequest = (Width / 3.01);
                    myImages[j + 1].HeightRequest = (Width / 3.01);
                    leaderRow.Children.Add(myImages[j + 1]);

                    if ((j + 2) < myImages.Count) {
                        myImages[j + 2].WidthRequest = (Width / 3.01);
                        myImages[j + 2].HeightRequest = (Width / 3.01);
                        leaderRow.Children.Add(myImages[j + 2]);
                    }
                }
                submissionStack.Children.Add(leaderRow);
            }
        }

        double lastRedrawnWidth = -2.0;
        public void redrawImages(object sender, EventArgs args) {
            if ((Width > 0) && (Width != lastRedrawnWidth)) {
                Debug.WriteLine("DHB:MySubmissionsPage:redrawImages redrawing... Width="+Width);
                //submissionStack.Children.Clear();
                lastRedrawnWidth = Width;
                if (myImages.Count == 0) {
                    buildMyImages(); //should not need this line!
                }
                buildUI();
            }
        }

        public void OnPhotoSubmit(object sender, EventArgs args) {
            // buildMyImages();  // this is a SLOOOOW function. just add the latest image
            byte[] raw = ((CameraContentPage)sender).latestTakenImgBytes;
            Image final = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(raw, ExifLib.ExifOrientation.Undefined, 720, 720);
            if (final != null) {
                myImages.Add(final);
            }
            raw = null;
            buildUI();
        }
    }
}
