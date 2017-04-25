using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;

using Xamarin.Forms;
using ExifLib;

namespace ImageImprov {
    public class CameraContentPage : ContentPage, ICamera {
        const string PHOTO = "photo";
        
        //< images
        Image currentSubmissionImg = new Image();
        readonly Image latestTakenImg = new Image();
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
        public Label categoryLabel;

        // Used to inform the user of success/fail of previous submissions, etc.
        Label lastActionResultLabel;

        Button takePicture;
        Button selectPictureFromCameraRoll;
        Button submitCurrentPicture;

        StackLayout portraitLayout;
        KeyPageNavigator defaultNavigationButtons;

        public CameraContentPage() {
            categoryLabel = new Label { Text = "Waiting for current category from server" };
            
            lastActionResultLabel = new Label { Text = "No actions performed yet" };
            takePicture = new Button {
                Text = "Take a picture to submit!",
                Command = new Command(o => ShouldTakePicture()),
            };

            submitCurrentPicture = new Button {
                Text = "Submit this image",
                IsVisible = false
            };
            submitCurrentPicture.Clicked += this.OnSubmitCurrentPicture;
            defaultNavigationButtons = new KeyPageNavigator { ColumnSpacing = 0, RowSpacing = 0 };

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
            Content = portraitLayout;
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
            categoryLabel.Text = "Today's category: " + GlobalStatusSingleton.uploadCategoryDescription;
        }


        //< ShouldTakePicture
        public event Action ShouldTakePicture = () => {  };
        //> ShouldTakePicture


        // click handler for SubmitCurrentPicture.
        protected async virtual void OnSubmitCurrentPicture(object sender, EventArgs e) {
            // prevent multiple click attempts; we heard ya
            ((Button)sender).IsEnabled = false;
            lastActionResultLabel.Text = "Uploading image to server...";

            string result = await sendSubmitAsync(latestTakenImgBytes);
            PhotoSubmitResponseJSON response = JsonConvert.DeserializeObject<PhotoSubmitResponseJSON>(result);
            if (response.message.Equals(PhotoSubmitResponseJSON.SUCCESS_MSG)) {
                // success. update the UI
                currentSubmissionImg.Source = ImageSource.FromStream(() => new MemoryStream(latestTakenImgBytes));
                lastActionResultLabel.Text = "Current submission image updated.";
            }

            ((Button)sender).IsEnabled = true;
            Content = portraitLayout; 
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
                result = "Network error. Please check your connection and try again.";
            } catch (HttpRequestException err) {
                // do something!!
                result = "login failure";
            }
            
            return result;
        }


        //< ShowImage
        public void ShowImage(string filepath, byte[] imgBytes)
        {
            submitCurrentPicture.IsVisible = true;
            latestTakenPath = filepath;
            latestTakenImg.Source = ImageSource.FromFile(filepath);
            latestTakenImgBytes = imgBytes;

            //
            /*  exiflib is exposed at this level. filestream does not appear to be.
            var jpegInfo = new JpegInfo();
            using (var myFStream = new System.IO.FileStream(file.Path, FileMode.Open)) {
                jpegInfo = ExifReader.ReadJpeg(myFStream);
                // portrait. upright. ExifLib.ExifOrientation.TopRight;
                // portrait. upside down. ExifLib.ExifOrientation.BottomLeft;
                // landscape. top to the right. ExifLib.ExifOrientation.BottomRight;
                // Landscape. Top (where the samsung is) rotated to the left. ExifLib.ExifOrientation.TopLeft;
            }
            */
        }
        //> ShowImage

    }
}
