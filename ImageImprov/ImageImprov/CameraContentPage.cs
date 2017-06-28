using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;  // for debug assertions.

using Xamarin.Forms;
using ExifLib;

namespace ImageImprov {
    public class CameraContentPage : ContentPage, ICamera {
        const string PHOTO = "photo";

        protected bool inPortraitMode;

        //< images.  Need both a portrait and a landscape instance due to different spans.
        // extents are bound to the object.  Since they differ for portrait and landscape I have two choices.
        // have to copies, or rebuild everything everytime.

        //Image currentSubmissionImgP = new Image();
        //Image currentSubmissionImgL = new Image();
        Image latestTakenImgP = null;
        Image latestTakenImgL = null;

        // filepath to the latest taken img
        string latestTakenPath = "";
        // To get img bytes we have to use native Android and iOS code.
        // Consequently, I pass the bytes back from the Android and iOS projects
        // so I can work with them in a cross platform manner.  Joy.
        byte[] latestTakenImgBytes = null;
        //> images

        // provides category info for today's contest to the user...
        // bound to data in GlobalStatusSingleton
        // @todo bind to dat in globalstatussingleton

        // Leverage the buttons to do this...
        //public Label categoryLabelP;
        //public Label categoryLabelL;

        // Used to inform the user of success/fail of previous submissions, etc.
        //Label lastActionResultLabelP;
        //Label lastActionResultLabelL;

        ScrollView contestStackP = new ScrollView();
        // this may be better as a dictionary...
        IList<CategoryJSON> loadedCategories = new List<CategoryJSON>();
        IList<Button> takePictureP = new List<Button>();
        IList<Button> takePictureL = new List<Button>();

        // @todo enable pictures from the camera roll
        //Button selectPictureFromCameraRoll;
        Button submitCurrentPictureP;
        Button submitCurrentPictureL;

        Grid portraitView;
        Grid landscapeView;

        KeyPageNavigator defaultNavigationButtonsP;
        KeyPageNavigator defaultNavigationButtonsL;

        //
        //   BEGIN Variables related/needed for images to place background on screen.
        //
        AbsoluteLayout layoutP = new AbsoluteLayout();  // this lets us place a background image on the screen.
        AbsoluteLayout layoutL = new AbsoluteLayout();  // this lets us place a background image on the screen.
        Assembly assembly = null;

        // shift to the submission (latestTakenImg) being the background...
        //Image backgroundImgP = null;
        //Image backgroundImgL = null;
        // This pattern is still active both before the user takes a photo and on any blank space due to letterboxing
        string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place background on screen.
        // 

        public CameraContentPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;

            /*
            categoryLabelP = new Label {
                Text = "Waiting for current category from server",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 213, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            };
            categoryLabelL = new Label {
                Text = "Waiting for current category from server",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 213, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            };
            */

            /*
            lastActionResultLabelP = new Label {
                Text = "No actions performed yet",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 213, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            };
            lastActionResultLabelL = new Label {
                Text = "No actions performed yet",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 213, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            };
            */
            /* Need a create function for these.  Now happens in the category load.
            takePictureP = new Button {
                Text = "Start Camera!",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 21, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                Command = new Command(o => ShouldTakePicture()),
            };
            takePictureL = new Button {
                Text = "Start Camera!",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 21, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                Command = new Command(o => ShouldTakePicture()),
            };
            */

            submitCurrentPictureP = new Button {
                Text = "Submit picture",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 21, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                IsVisible = false
            };
            submitCurrentPictureL = new Button {
                Text = "Submit picture",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 21, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                IsVisible = false
            };
            submitCurrentPictureP.Clicked += this.OnSubmitCurrentPicture;
            submitCurrentPictureL.Clicked += this.OnSubmitCurrentPicture;

            defaultNavigationButtonsP = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };
            defaultNavigationButtonsL = new KeyPageNavigator { ColumnSpacing = 1, RowSpacing = 1 };

            /*
            portraitLayout = new StackLayout {
                VerticalOptions = LayoutOptions.Center,
                Children = {
                    categoryLabel,
                    takePicture,
                    currentSubmissionImg,
                    latestTakenImg,
                    submitCurrentPicture,
                    lastActionResultLabel,
                    defaultNavigationButtons,
                },
            };
            */
            // only have one active at a time as grid properties bind to the ui elements, meaning
            // an element can only be defined correctly for one rotation at a time.
            //buildPortraitView();
            //buildLandscapeView();
            buildUI();
            //setView();
        }

        /*
        protected override void OnSizeAllocated(double width, double height) {
            // need to have portrait mode local rather than global or i can't
            // tell if I've updated for this page yet or not.
            base.OnSizeAllocated(width, height);
            if ((width > height) && (inPortraitMode==true) && (landscapeView != null)) {
                inPortraitMode = false;
                GlobalStatusSingleton.inPortraitMode = false;
                //buildLandscapeView();
                //Content = landscapeView;
                if (latestTakenImgL == null) {
                    // build default background
                    latestTakenImgL = GlobalSingletonHelpers.buildBackground(
                        backgroundPatternFilename, assembly, (int)width, (int)height);
                    // unfortunately - this means I have to trigger a layout rebuild.
                    buildLandscapeView();
                }
                if (layoutL != null) {
                    Content = layoutL;
                } else if (landscapeView != null) {
                    Content = landscapeView;
                }

            } else if ((height>width) && (inPortraitMode==false) && (portraitView!= null)) {
                inPortraitMode = true;
                GlobalStatusSingleton.inPortraitMode = true;
                //buildPortraitView();
                //Content = portraitView;
                if (latestTakenImgP == null) {
                    latestTakenImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Width, (int)Height);
                    // unfortunately - this means I have to trigger a layout rebuild.
                    buildPortraitView();
                }
                if (layoutP != null) {
                    Content = layoutP;
                } else if (portraitView != null) {
                    Content = portraitView;
                } // otherwise don't change content.
            }
        }
        */

        protected int buildUI() {
            /*
            int res = 0;
            int res2 = 0;
            Device.BeginInvokeOnMainThread(() =>
            {
                res = buildPortraitView();
                res2 = buildLandscapeView();
            });
            return ((res < res2) ? res : res2);
            */
            int res = 0;
            Device.BeginInvokeOnMainThread(() =>
            {
                res = buildPortraitView();
                if (res == 1) {
                    //Content = layoutP;
                    Content = portraitView;
                }
            });
            return res;
        }

        protected int buildPortraitView() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor=GlobalStatusSingleton.backgroundColor };
                for (int i = 0; i < 20; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            //portraitView.Children.Add(categoryLabelP, 0, 0);  // col, row
            StackLayout buttonStack = new StackLayout();
            foreach (Button b in takePictureP) {
                buttonStack.Children.Add(b);
            }
            contestStackP.Content = buttonStack;
            portraitView.Children.Add(contestStackP, 0, 0);
            Grid.SetRowSpan(contestStackP, 4);
            if (latestTakenImgP != null) {
                portraitView.Children.Add(latestTakenImgP, 0, 4);
                Grid.SetRowSpan(latestTakenImgP, 12);
            }
            portraitView.Children.Add(submitCurrentPictureP, 0, 16);
            Grid.SetRowSpan(submitCurrentPictureP, 2);
            //portraitView.Children.Add(lastActionResultLabelP, 0, 9);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 18);
            Grid.SetRowSpan(defaultNavigationButtonsP, 2);

            /* w,h are not consistent when rotated ie h=616 portrait, but w=640 landscape
             * so build default background in OnSizeAllocated, and image based background here.
            //if (latestTakenPath.Equals("")) {
            if (latestTakenImgP == null) { 
                //int w = (Width > -1) ? (int)Width : 720;
                //int h = ((Height > -1) ? (int)Height : 1280);
                int w = (int)Width;
                int h = (int)Height;
                // we maybe in landscape mode. switch w and h if we are.
                if (w > h) {
                    int tmp = w;
                    w = h;
                    h = tmp;
                }
                latestTakenImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, w, h);
            } else {
                */

            /* img no longer the full background.
            if (!latestTakenPath.Equals("")) {
                // we are now only in portrait mode, making this an uncomplicated calculation:
                double heightPct = Width / Height;
                latestTakenImgP = GlobalSingletonHelpers.buildBackgroundFromBytes(latestTakenImgBytes, assembly, (int)Width, (int)Height, 
                    heightPct, GlobalStatusSingleton.PATTERN_FULL_COVERAGE);
            }
            layoutP.Children.Clear();
            if (latestTakenImgP != null) {
                layoutP.Children.Add(latestTakenImgP, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            layoutP.Children.Add(portraitView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            */

            return 1;
        }

        /*
        protected int buildPortraitViewOld() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                for (int i = 0; i < 11; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            portraitView.Children.Add(categoryLabelP, 0, 0);  // col, row
            portraitView.Children.Add(takePictureP, 0, 1);
            //portraitView.Children.Add(currentSubmissionImgP, 0, 2);
            //Grid.SetRowSpan(currentSubmissionImgP, 3);
            portraitView.Children.Add(latestTakenImgP, 0, 2);
            Grid.SetRowSpan(latestTakenImgP, 6);
            portraitView.Children.Add(submitCurrentPictureP, 0, 8);
            portraitView.Children.Add(lastActionResultLabelP, 0, 9);
            portraitView.Children.Add(defaultNavigationButtonsP, 0, 10);

            return 1;
        }
        */
        protected int buildLandscapeView() {
            if (landscapeView == null) {
                landscapeView = new Grid { ColumnSpacing = 1, RowSpacing = 1 };
                for (int i = 0; i < 10; i++) {
                    landscapeView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                // 2 columns, 50% each
                //landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                // I can add none, but if i add one, then i just have 1. So here's 2. :)
                //landscapeView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            } else {
                // flush the old children.
                landscapeView.Children.Clear();
                landscapeView.IsEnabled = true;
            }
            //landscapeView.Children.Add(categoryLabelL, 0, 0);  // col, row
            StackLayout buttonStack = new StackLayout();
            foreach (Button b in takePictureL) {
                buttonStack.Children.Add(b);
            }
            landscapeView.Children.Add(buttonStack, 0, 1);
            //landscapeView.Children.Add(lastActionResultLabelL, 0, 1);
            //landscapeView.Children.Add(takePictureL, 0, 2);
            landscapeView.Children.Add(submitCurrentPictureL, 0, 8);
            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);
            //Grid.SetColumnSpan(defaultNavigationButtonsL, 1);

            /* Moved to OnSizeAllocated to handle the disparate w,h amounts by rotation.
            //if (latestTakenPath.Equals("")) {
            if (latestTakenImgL == null) { 
                // unfortunately, this just results in a missized bitmap.
                //int w = (Width > -1) ? (int)Width : 1280;
                //int h = ((Height > -1) ? (int)Height : 720);
                int w = (int)Width;
                int h = (int)Height;
                // we maybe in portrait mode. switch w and h if we are.
                // actually w and h aren't the w->h->w is not true. (ie the pixel ranges change with rotation).
                // this is why background works when I set it in OnSizeAllocated, but not here!
                if (h > w) {
                    //int tmp = w;
                    //w = h;
                    //h = tmp;
                    // skip!
                } else {
                    latestTakenImgL = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, w, h, GlobalStatusSingleton.PATTERN_PCT, GlobalStatusSingleton.PATTERN_FULL_COVERAGE);
                }
            } else {
                */
            if (!latestTakenPath.Equals("")) { 
                // user has taken a photo, so rebuild the background with it.
                latestTakenImgL = GlobalSingletonHelpers.buildBackgroundFromBytes(latestTakenImgBytes, assembly, (int)Width, (int)Height,
                    GlobalStatusSingleton.PATTERN_FULL_COVERAGE, GlobalStatusSingleton.PATTERN_FULL_COVERAGE);
            }
            layoutL.Children.Clear();
            if (latestTakenImgL != null) {
                layoutL.Children.Add(latestTakenImgL, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            }
            layoutL.Children.Add(landscapeView, new Rectangle(0, 0, 1, 1), AbsoluteLayoutFlags.All);
            return 1;
        }

        protected void setView() {
            inPortraitMode = GlobalStatusSingleton.IsPortrait(this);
            if (inPortraitMode) {
                Content = portraitView;
            } else {
                Content = landscapeView;
            }
        }

        public bool hasCamera() {
            // @todo learn how to check for camera exists on device
            // @todo learn how to get permission for data/camera on device
            /* determine how to do this...
            if (!App.Current.IsCameraAvailable() || !App.Current.IsTakePhotoSupported()) {
                await DisplayAlert("No Camera", ":( No camera avaialble.", "OK");
                return false;
            }
            */
            return true;
        }

        public virtual void OnCategoryLoad(object sender, EventArgs e) {
            //categoryLabelP.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
            //categoryLabelL.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
            if (GlobalStatusSingleton.uploadingCategories.Count > 0) {
                //categoryLabelP.Text = "Today's category: " + GlobalStatusSingleton.uploadingCategories[0].description;
                //categoryLabelL.Text = "Today's category: " + GlobalStatusSingleton.uploadingCategories[0].description;
                if ((takePictureL.Count >0) && (loadedCategories.Count == 0)) {
                    // clear out the no contests button
                    takePictureP.Clear();
                    takePictureL.Clear();
                }
                foreach (CategoryJSON category in GlobalStatusSingleton.uploadingCategories) {
                    // make sure we aren't doubling up...
                    if (!loadedCategories.Contains(category)) {
                        Button pButton = new Button
                        {
                            Text = " " + category.description + " - Take Picture Now! ",
                            HorizontalOptions = LayoutOptions.CenterAndExpand,
                            VerticalOptions = LayoutOptions.FillAndExpand,
                            TextColor = Color.Black,
                            BackgroundColor = Color.FromRgb(252, 213, 21),
                            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                            Command = new Command(o => ShouldTakePicture()),
                        };
                        takePictureP.Add(pButton);
                        Button lButton = new Button
                        {
                            Text = " " + category.description + " - Take Picture Now! ",
                            HorizontalOptions = LayoutOptions.CenterAndExpand,
                            VerticalOptions = LayoutOptions.FillAndExpand,
                            TextColor = Color.Black,
                            BackgroundColor = Color.FromRgb(252, 213, 21),
                            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                            Command = new Command(o => ShouldTakePicture()),
                        };
                        takePictureL.Add(lButton);
                        loadedCategories.Add(category);
                    }
                }
            } else {
                //categoryLabelP.Text = "No open contest";
                //categoryLabelL.Text = "No open contest";
                Button pButton = new Button {
                    Text = "No open contests",
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    TextColor = Color.Black,
                    BackgroundColor = Color.FromRgb(252, 21, 21),
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    IsEnabled = false,
                };
                takePictureP.Add(pButton);
                Button lButton = new Button
                {
                    Text = "No open contests",
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    TextColor = Color.Black,
                    BackgroundColor = Color.FromRgb(252, 21, 21),
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    IsEnabled = false,
                };
                takePictureL.Add(lButton);
                // loadedCategories stays empty, and we use the difference to know to clear this out.
            }
            buildUI();
        }


        //< ShouldTakePicture
        public event Action ShouldTakePicture = () => {  };
        //> ShouldTakePicture


        // click handler for SubmitCurrentPicture.
        protected async virtual void OnSubmitCurrentPicture(object sender, EventArgs e) {
            Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture start");
            // check that button is enabled... xamarin has weak-fu here.
            if (((Button)sender).IsEnabled==false) { return; }

            // prevent multiple click attempts; we heard ya
            submitCurrentPictureP.IsEnabled = false;
            submitCurrentPictureL.IsEnabled = false;
            //lastActionResultLabelP.Text = "Uploading image to server...";
            //lastActionResultLabelL.Text = "Uploading image to server(may take a while)...";
            //submitCurrentPictureP.Text = "Uploading image to server...";
            submitCurrentPictureP.Text = "Submitting!";
            //submitCurrentPictureL.Text = "Uploading image to server(may take a while)...";

            Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture pre async call");
            string result = await sendSubmitAsync(latestTakenImgBytes);
            Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture post async call");
            try {
                PhotoSubmitResponseJSON response = JsonConvert.DeserializeObject<PhotoSubmitResponseJSON>(result);
                Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture post json deserialize");
                if (response.message.Equals(PhotoSubmitResponseJSON.SUCCESS_MSG)) {
                    // success. update the UI
                    //currentSubmissionImgP.Source = ImageSource.FromStream(() => new MemoryStream(latestTakenImgBytes));
                    //currentSubmissionImgL.Source = ImageSource.FromStream(() => new MemoryStream(latestTakenImgBytes));
                    //currentSubmissionImgP = GlobalSingletonHelpers.buildFixedRotationImageFromStr(latestTakenImgBytes);
                    //currentSubmissionImgL = GlobalSingletonHelpers.buildFixedRotationImageFromStr(latestTakenImgBytes);
                    //lastActionResultLabelP.Text = "Congratulations, you're in!";
                    //lastActionResultLabelL.Text = "Congratulations, you're in!";
                    submitCurrentPictureP.Text = "Congratulations, you're in!";
                    submitCurrentPictureL.Text = "Congratulations, you're in!";

                    // @todo hmm, shifting what the imgs point to doesn't update the ui. the below seems like an expensive approach...
                    // also update when image is taken when fixing this.
                    //buildPortraitView();
                    //buildLandscapeView();
                    buildUI();
                    //setView();
                    Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture end");
                }
            } catch (Exception err) {
                Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture invalid response json: "+result);
                Debug.WriteLine(err.ToString());
                submitCurrentPictureP.Text = "Submit failed - try again";
                submitCurrentPictureL.Text = "Submit failed";
                submitCurrentPictureP.IsEnabled = true;
                buildUI();
            }

            // only enable on picture taking.
            //((Button)sender).IsEnabled = true;

            // was this line needed?
            //Content = portraitLayout; 
            return;
        }


        /// <summary>
        /// connects to the server and sends the user's current submission.
        /// </summary>
        /// <param name="imgBytes">bytes of the image we are sending.</param>
        /// <returns>The JSON success string on success, an err msg on failure. </returns>
        protected static async Task<string> sendSubmitAsync(byte[] imgBytes) {
            Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync start");
            //string result = "fail";
            PhotoSubmitResponseJSON resJson = new PhotoSubmitResponseJSON();
            resJson.message = "fail";
            string result = JsonConvert.SerializeObject(resJson);
            try {
                PhotoSubmitJSON submission = new PhotoSubmitJSON();

                submission.imgStr = imgBytes;
                submission.extension = "JPEG";
                Debug.Assert(GlobalStatusSingleton.uploadingCategories.Count > 0, "DHB:ASSERT!:CameraContentPage:sendSubmitAsync Invalid uploading categories!");
                submission.categoryId = GlobalStatusSingleton.uploadingCategories[0].categoryId;
                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync catid: " + GlobalStatusSingleton.uploadingCategories[0].categoryId.ToString());
                //submission.userId = GlobalStatusSingleton.loginCredentials.userId;
                // moved this up in case serialization was causing the client to be disposed prematurely...
                string jsonQuery = JsonConvert.SerializeObject(submission);
                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync query serialized");
                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync query[0,99]: "+jsonQuery.Substring(0,100));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, PHOTO);
                //request.Headers.Add("Authorization", "JWT " + GlobalStatusSingleton.authToken.accessToken);
                request.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync request obj built");
                // string test = request.ToString();

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync pre send");
                HttpResponseMessage submitResult = await client.SendAsync(request);
                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync post send");
                if (submitResult.StatusCode == System.Net.HttpStatusCode.Created) {
                    // tada
                    Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync ok return");
                    result = await submitResult.Content.ReadAsStringAsync();
                    Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync result read in");
                } else {
                    Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync send statusCode="+submitResult.StatusCode.ToString());
                    result = "unknown failure";
                }
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine(err.ToString());
                Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync webexception");
                result = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                Debug.WriteLine(err.ToString());
                // do something!!
                result = "login failure";
            } catch (Exception err) {
                Debug.WriteLine(err.ToString());
                // do something!!
                result = "unknown failure";
            }
            Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync end");
            return result;
        }


        // tmp helpers during exif debug.
        //ExifOrientation imgExifO;
        //int imgExifWidth;
        //int imgExifHeight;

        //< ShowImage
        public void ShowImage(string filepath, byte[] imgBytes)
        {
            submitCurrentPictureP.IsVisible = true;
            //submitCurrentPictureL.IsVisible = true;
            latestTakenPath = filepath;
            //latestTakenImgP.Source = ImageSource.FromFile(filepath);
            //latestTakenImgL.Source = ImageSource.FromFile(filepath);

            // This works... but is not neccessarily a square image.
            //latestTakenImgBytes = imgBytes;
            latestTakenImgBytes = GlobalSingletonHelpers.SquareImage(imgBytes);

            latestTakenImgP = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(latestTakenImgBytes);
            //latestTakenImgL = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(imgBytes);
            submitCurrentPictureP.Text = "Submit picture";
            submitCurrentPictureP.IsEnabled = true;
            //submitCurrentPictureL.Text = "Submit picture";
            //submitCurrentPictureL.IsEnabled = true;

            buildUI();
            //setView();
        }
        //> ShowImage

    }
}
