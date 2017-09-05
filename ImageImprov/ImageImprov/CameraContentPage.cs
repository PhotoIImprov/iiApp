using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Diagnostics;  // for debug assertions.

using Xamarin.Forms;
using ExifLib;

namespace ImageImprov {
    public delegate void LoadBallotFromPhotoSubmissionEventHandler(object sender, EventArgs e);

    public class CameraContentPage : ContentView, ICamera, IManageCategories {
        public event LoadBallotFromPhotoSubmissionEventHandler LoadBallotFromPhotoSubmission;
        public const string PHOTO = "photo";

        /// <summary>
        /// Used by the camera page and all assoicated pages to track what is the current actively selected category for image submission.
        /// hmm. Since this is specific to Camera, I moved it from GlobalStatusSingleton to CameraContentPage.
        /// </summary>
        public static CategoryJSON activeCameraCategory;
        /// <summary>
        /// Calculated when switchToSubmit is called.  Used by the native cameras to draw the header the correct size.
        /// </summary>
        public static double bestFontSize = 24.0;

        CameraCategorySelectionView selectionView;
        CameraCreateCategoryView createView;
        CameraEnterPhotoView submitView;

        // To get img bytes we have to use native Android and iOS code.
        // Consequently, I pass the bytes back from the Android and iOS projects
        // so I can work with them in a cross platform manner.  Joy.
        public byte[] latestTakenImgBytes = null;
        //> images

        // provides category info for today's contest to the user...

        ScrollView contestStackP = new ScrollView();
        // this may be better as a dictionary...
        IList<CategoryJSON> loadedCategories = new List<CategoryJSON>();

        // @todo enable pictures from the camera roll
        //Button selectPictureFromCameraRoll;

        public string latestPassphrase { get; set; }
        //
        //   BEGIN Variables related/needed for images to place background on screen.
        //
        //AbsoluteLayout layoutP = new AbsoluteLayout();  // this lets us place a background image on the screen.
        Assembly assembly = null;

        // This pattern is still active both before the user takes a photo and on any blank space due to letterboxing
        //string backgroundPatternFilename = "ImageImprov.IconImages.pattern.png";
        //
        //   END Variables related/needed for images to place background on screen.
        // 

        public CameraContentPage() {
            Debug.WriteLine("DHB:CameraContentPage:CameraContentPage in ctor ok");
            assembly = this.GetType().GetTypeInfo().Assembly;
            selectionView = new CameraCategorySelectionView(this);
            createView = new CameraCreateCategoryView(this);
            submitView = new CameraEnterPhotoView(this);
            Debug.WriteLine("DHB:CameraContentPage:CameraContentPage post view creation.");

            buildUI();
        }

        protected int buildUI() {
            int res = 0;
            Device.BeginInvokeOnMainThread(() =>
            {
                res = buildPortraitView();
                //if (res == 1) {
                //    Content = portraitView;
                //}
            });
            return res;
        }

        protected int buildPortraitView() {
            switchToSelectView();
            return 1;
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
            /*
            if (GlobalStatusSingleton.uploadingCategories.Count > 0) {
                submitCurrentPictureP.Text = "Enter competition " + GlobalSingletonHelpers.getUploadingCategoryDesc();
            } else {
                submitCurrentPictureP.Text = "No open contests right now. Sorry.";
            }
            */
            /*
            if (activeCameraCategory != null) {
                submitCurrentPictureP.Text = "Enter competition " + activeCameraCategory.description;
            }
            */
            selectionView.OnCategoryLoad();
            selectionView.buildUI();
            submitView.buildUI();
            
            buildUI();
        }


        //< ShouldTakePicture
        public event Action ShouldTakePicture = () => {  };
        //> ShouldTakePicture

        public void startCamera() {
            switchToSubmitView();
            ShouldTakePicture.Invoke();
        }




        // tmp helpers during exif debug.
        //ExifOrientation imgExifO;
        //int imgExifWidth;
        //int imgExifHeight;

        //< ShowImage
        public void ShowImage(string filepath, byte[] imgBytes) {
            submitView.update(filepath, imgBytes);
        }
        //> ShowImage
        
        public void switchToSelectView() {
            Content = selectionView.Content;
        }
        public void switchToSubmitView() {
            if (activeCameraCategory != null) {
                submitView.setChallengeName(activeCameraCategory.description);
            }
            Content = submitView.Content;
        }

        public void switchToCreateCategoryView() {
            Content = createView.Content;
        }

        public void AddEvent(EventJSON cerj) {
            selectionView.AddEvent(cerj);
        }

        public void fireLoadBallotFromPhotoSubmission(BallotFromPhotoSubmissionEventArgs ballotEvt) {
            if (LoadBallotFromPhotoSubmission != null) {
                LoadBallotFromPhotoSubmission(this, ballotEvt);
            }
        }
    }
}
