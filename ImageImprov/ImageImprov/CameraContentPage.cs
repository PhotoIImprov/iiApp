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
        Image latestTakenImgP = new Image();
        Image latestTakenImgL = new Image();

        // filepath to the latest taken img
        string latestTakenPath;
        // To get img bytes we have to use native Android and iOS code.
        // Consequently, I pass the bytes back from the Android and iOS projects
        // so I can work with them in a cross platform manner.  Joy.
        byte[] latestTakenImgBytes = null;
        //> images

        // provides category info for today's contest to the user...
        // bound to data in GlobalStatusSingleton
        // @todo bind to dat in globalstatussingleton
        public Label categoryLabelP;
        public Label categoryLabelL;

        // Used to inform the user of success/fail of previous submissions, etc.
        Label lastActionResultLabelP;
        Label lastActionResultLabelL;

        Button takePictureP;
        Button takePictureL;
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
        AbsoluteLayout layoutP;  // this lets us place a background image on the screen.
        AbsoluteLayout layoutL;  // this lets us place a background image on the screen.
        Assembly assembly = null;
        Image backgroundImgP = null;
        Image backgroundImgL = null;
        string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place background on screen.
        // 

        public CameraContentPage() {
            assembly = this.GetType().GetTypeInfo().Assembly;

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

            submitCurrentPictureP = new Button {
                Text = "Submit this image",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.Black,
                BackgroundColor = Color.FromRgb(252, 21, 21),
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                IsVisible = false
            };
            submitCurrentPictureL = new Button {
                Text = "Submit this image",
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

        protected override void OnSizeAllocated(double width, double height) {
            // need to have portrait mode local rather than global or i can't
            // tell if I've updated for this page yet or not.
            base.OnSizeAllocated(width, height);
            if ((width > height) && (inPortraitMode==true) && (landscapeView != null)) {
                inPortraitMode = false;
                GlobalStatusSingleton.inPortraitMode = false;
                //buildLandscapeView();
                //Content = landscapeView;
                if (backgroundImgL == null) {
                    backgroundImgL = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)Width, (int)Height);
                    layoutL = new AbsoluteLayout
                    {
                        Children = {
                                    { backgroundImgL, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All },
                                    { landscapeView, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All }
                                }
                    };
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
                if ((backgroundImgP == null) && (width > 0) && (portraitView != null)) {
                    backgroundImgP = GlobalSingletonHelpers.buildBackground(backgroundPatternFilename, assembly, (int)width, (int)height);
                    layoutP = new AbsoluteLayout
                    {
                        Children = {
                                    { backgroundImgP, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All },
                                    { portraitView, new Rectangle(0,0,1,1), AbsoluteLayoutFlags.All }
                                }
                    };
                }
                if (layoutP != null) {
                    Content = layoutP;
                } else if (portraitView != null) {
                    Content = portraitView;
                } // otherwise don't change content.
            }
        }

        protected int buildUI() {
            int res = 0;
            int res2 = 0;
            Device.BeginInvokeOnMainThread(() =>
            {
                res = buildPortraitView();
                res2 = buildLandscapeView();
            });
            return ((res < res2) ? res : res2);
        }

        protected int buildPortraitView() {
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
            /*
            landscapeView.Children.Add(categoryLabelL, 0, 0);  // col, row
            landscapeView.Children.Add(lastActionResultLabelL, 1, 0);
            landscapeView.Children.Add(takePictureL, 0, 1);
            landscapeView.Children.Add(submitCurrentPictureL, 1, 1);
            //landscapeView.Children.Add(currentSubmissionImgL, 0, 2);
            //Grid.SetRowSpan(currentSubmissionImgL, 7);
            //landscapeView.Children.Add(latestTakenImgL, 1, 2);
            //Grid.SetRowSpan(latestTakenImgL, 7);
            landscapeView.Children.Add(latestTakenImgL, 0, 2);
            Grid.SetRowSpan(latestTakenImgL, 14);
            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);
            Grid.SetColumnSpan(defaultNavigationButtonsL, 2);
            */
            landscapeView.Children.Add(categoryLabelL, 0, 0);  // col, row
            landscapeView.Children.Add(lastActionResultLabelL, 0, 1);
            landscapeView.Children.Add(takePictureL, 0, 2);
            landscapeView.Children.Add(submitCurrentPictureL, 0, 8);
            //landscapeView.Children.Add(currentSubmissionImgL, 0, 2);
            //Grid.SetRowSpan(currentSubmissionImgL, 7);
            //landscapeView.Children.Add(latestTakenImgL, 1, 2);
            //Grid.SetRowSpan(latestTakenImgL, 7);
            landscapeView.Children.Add(latestTakenImgL, 0, 3);
            Grid.SetRowSpan(latestTakenImgL, 5);
            landscapeView.Children.Add(defaultNavigationButtonsL, 0, 9);
            Grid.SetColumnSpan(defaultNavigationButtonsL, 1);
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
            categoryLabelP.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
            categoryLabelL.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
        }


        //< ShouldTakePicture
        public event Action ShouldTakePicture = () => {  };
        //> ShouldTakePicture


        // click handler for SubmitCurrentPicture.
        protected async virtual void OnSubmitCurrentPicture(object sender, EventArgs e) {
            // prevent multiple click attempts; we heard ya
            ((Button)sender).IsEnabled = false;
            lastActionResultLabelP.Text = "Uploading image to server...";
            lastActionResultLabelL.Text = "Uploading image to server(may take a while)...";

            string result = await sendSubmitAsync(latestTakenImgBytes);
            PhotoSubmitResponseJSON response = JsonConvert.DeserializeObject<PhotoSubmitResponseJSON>(result);
            if (response.message.Equals(PhotoSubmitResponseJSON.SUCCESS_MSG)) {
                // success. update the UI
                //currentSubmissionImgP.Source = ImageSource.FromStream(() => new MemoryStream(latestTakenImgBytes));
                //currentSubmissionImgL.Source = ImageSource.FromStream(() => new MemoryStream(latestTakenImgBytes));
                //currentSubmissionImgP = GlobalSingletonHelpers.buildFixedRotationImageFromStr(latestTakenImgBytes);
                //currentSubmissionImgL = GlobalSingletonHelpers.buildFixedRotationImageFromStr(latestTakenImgBytes);
                lastActionResultLabelP.Text = "Congratulations, you're in!";
                lastActionResultLabelL.Text = "Congratulations, you're in!";

                // @todo hmm, shifting what the imgs point to doesn't update the ui. the below seems like an expensive approach...
                // also update when image is taken when fixing this.
                //buildPortraitView();
                //buildLandscapeView();
                buildUI();
                //setView();

            }

            ((Button)sender).IsEnabled = true;

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
            //string result = "fail";
            PhotoSubmitResponseJSON resJson = new PhotoSubmitResponseJSON();
            resJson.message = "fail";
            string result = JsonConvert.SerializeObject(resJson);
            try {
                PhotoSubmitJSON submission = new PhotoSubmitJSON();

                submission.imgStr = imgBytes;
                submission.extension = "JPEG";
                submission.categoryId = GlobalStatusSingleton.uploadingCategoryId;
                //submission.userId = GlobalStatusSingleton.loginCredentials.userId;

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, PHOTO);
                //request.Headers.Add("Authorization", "JWT " + GlobalStatusSingleton.authToken.accessToken);
                request.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                string jsonQuery = JsonConvert.SerializeObject(submission);
                request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                // string test = request.ToString();

                HttpResponseMessage submitResult = await client.SendAsync(request);
                if (submitResult.StatusCode == System.Net.HttpStatusCode.Created) {
                    // tada
                    result = await submitResult.Content.ReadAsStringAsync();
                }
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine(err.ToString());
                result = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                Debug.WriteLine(err.ToString());
                // do something!!
                result = "login failure";
            }
            
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
            submitCurrentPictureL.IsVisible = true;
            latestTakenPath = filepath;
            //latestTakenImgP.Source = ImageSource.FromFile(filepath);
            //latestTakenImgL.Source = ImageSource.FromFile(filepath);
            latestTakenImgBytes = imgBytes;

            latestTakenImgP = GlobalSingletonHelpers.buildFixedRotationImageFromStr(imgBytes);
            latestTakenImgL = GlobalSingletonHelpers.buildFixedRotationImageFromStr(imgBytes);
            // @todo hmm, shifting what these point to doesn't update the ui. the below seems like an expensive approach...
            // also update when submission completes when fixing this.
            //buildPortraitView();
            //buildLandscapeView();
            buildUI();
            //setView();

            //
            /*  exiflib is exposed at this level. filestream does not appear to be.  
            var jpegInfo = new JpegInfo();
            //using (var myFStream = new System.IO.FileStream(file.Path, FileMode.Open)) {
            using (var myFStream = new System.IO.MemoryStream(imgBytes)) {
                jpegInfo = ExifReader.ReadJpeg(myFStream);
                // portrait. upright. ExifLib.ExifOrientation.TopRight;
                // portrait. upside down. ExifLib.ExifOrientation.BottomLeft;
                // landscape. top to the right. ExifLib.ExifOrientation.BottomRight;
                // Landscape. Top (where the samsung is) rotated to the left. ExifLib.ExifOrientation.TopLeft;

                imgExifO = jpegInfo.Orientation;
                imgExifWidth = jpegInfo.Width;
                imgExifHeight = jpegInfo.Height;
                
                lastActionResultLabelP.Text = "Orient:"+imgExifO.ToString()+"  W:"+imgExifWidth+", H:"+imgExifHeight;
                lastActionResultLabelL.Text = "Orient:" + imgExifO.ToString() + "  W:" + imgExifWidth + ", H:" + imgExifHeight;

                // test if we got location data...
                // yes we did.
                //double[] latitudeDMS = jpegInfo.GpsLatitude;
                //double[] longitudeDMS = jpegInfo.GpsLongitude;
                //ExifGpsLatitudeRef latitudeRef = jpegInfo.GpsLatitudeRef;
                //ExifGpsLongitudeRef longitudeRef = jpegInfo.GpsLongitudeRef;

                // attempt a correction here...
                //if ((jpegInfo.Orientation == ExifOrientation.TopRight) && (jpegInfo.)
                // does the thumbnail tell me what I want to know for correcting samsung's fubarness?
                //    Nope.
                //using (var thumbStream = new System.IO.MemoryStream(jpegInfo.ThumbnailData)) {
                //    var thumbInfo = ExifReader.ReadJpeg(thumbStream);
                //    bool falseBreak = true;
                //}
            }
            //*/
        }
        //> ShowImage

    }
}
