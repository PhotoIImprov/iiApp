using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Newtonsoft.Json;
using System.Net.Http;
using SkiaSharp;


namespace ImageImprov {

    /// <summary>
    /// The view for CameraContentPage when I'm submitting a photo.
    /// </summary>
    class CameraEnterPhotoView : ContentView {

        CameraContentPage cameraPage;
        Grid portraitView;

        /* public Label challengeLabelP = new Label {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            //BackgroundColor = GlobalStatusSingleton.ButtonColor,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
            //WidthRequest = Width,
            //MinimumHeightRequest = Height / 15.0,
            //FontSize = 30, // program later
        };*/
        // value differs for tablet and phones
        double heightAdjustment = 10.0;

        iiBitmapView latestTakenImgP = null;
        Button submitCurrentPictureP;
        bool canClickSubmit = false;


        //iiBitmapView backButton = null;  // now managed through page header.
        LightbulbProgessBar bulb = new LightbulbProgessBar { IsVisible = false, HorizontalOptions = LayoutOptions.End, Margin = 3, };
        iiBitmapView alertBulb;

        // filepath to the latest taken img
        string latestTakenPath = "";

        Frame tagsFrame = new Frame() { OutlineColor = Color.Black, };
        Entry tagsEntry = new Entry {
            Placeholder = "Enter tags about your photo to submit",
            PlaceholderColor = Color.Gray,
            TextColor = Color.Black,
            FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
            BackgroundColor = Color.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            Margin = 2,
        };
        EventHandler submitOnTagEntryBegin;

        public CameraEnterPhotoView(CameraContentPage parent) {
            if (Device.Idiom == TargetIdiom.Tablet) heightAdjustment = 20.0;
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            /* try in buildUI to see if that makes a difference for ios...
            backButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly)) {
                HorizontalOptions = LayoutOptions.Start,
                Margin = 4,
            };
            TapGestureRecognizer backTap = new TapGestureRecognizer();
            backTap.Tapped += OnBackPressed;
            backButton.GestureRecognizers.Add(backTap);
            */
            // the above is not working on ios and it makes no sense as it works in Android. grrrr.
            // this didn't help.
            //if (Device.OS == TargetPlatform.iOS) {
            //  challengeLabelP.GestureRecognizers.Add(backTap);
            //}
            //Debug.WriteLine("DHB:CameraEnterPhotoView:CameraEnterPhotoView in ctor ok");

            alertBulb = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 4,
                IsVisible = false,
            };

            this.cameraPage = parent;
            submitCurrentPictureP = new Button {
                Text = "Enter this photo!",
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
                TextColor = Color.White,
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
                FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                IsVisible = false,
                //IsEnabled = false,
            };
            submitCurrentPictureP.Clicked += this.OnSubmitCurrentPicture;
            latestTakenImgP = new iiBitmapView();

            this.animate += new EventHandler(AnimationEvent);
            tagsEntry.TextChanged += tagTextChanged;
            submitOnTagEntryBegin += OnTagEntryBeginSubmit;
        }

        public int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 2, BackgroundColor = GlobalStatusSingleton.backgroundColor };
                for (int i = 0; i < 12; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                /* OnBackPressed is still used (it's the fcn ptr back into page header). backcaret is now on the header.
                Assembly assembly = this.GetType().GetTypeInfo().Assembly;
                backButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly)) {
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = 4,
                };
                TapGestureRecognizer backTap = new TapGestureRecognizer();
                backTap.Tapped += OnBackPressed;
                backButton.GestureRecognizers.Add(backTap);
                //challengeLabelP.GestureRecognizers.Add(backTap);
                */

            } else {
                // flush the old children.
                portraitView.Children.Clear();
                portraitView.IsEnabled = true;
            }
            //portraitView.Children.Add(challengeLabelP, 0, 0);
            //portraitView.Children.Add(backButton, 0, 0);

            if (latestTakenImgP != null) {
                portraitView.Children.Add(latestTakenImgP, 0, 0);
                Grid.SetRowSpan(latestTakenImgP, 12);
            }
            portraitView.Children.Add(tagsFrame, 0, 12);
            portraitView.Children.Add(tagsEntry, 0, 12);
            //Label dummy = new Label { Text = "You are on camera page", TextColor=Color.Black };
            //portraitView.Children.Add(dummy, 0, 8);
            portraitView.Children.Add(submitCurrentPictureP, 0, 15);
            //Grid.SetRowSpan(submitCurrentPictureP, 2);
            //portraitView.Children.Add(lastActionResultLabelP, 0, 9);

            portraitView.Children.Add(bulb, 0, 15);
            //Grid.SetRowSpan(bulb, 2);
            portraitView.Children.Add(alertBulb, 0, 15);
            //Grid.SetRowSpan(alertBulb, 2);
            Content = portraitView;
            return 1;
        }

        /*
        public void setChallengeName(string description) {
            challengeLabelP.Text = description;
            GlobalSingletonHelpers.fixLabelHeight(challengeLabelP, Width, Height / heightAdjustment);
            CameraContentPage.bestFontSize = challengeLabelP.FontSize;
        } */

        public void update(string filepath, byte[] imgBytes) {
            submitCurrentPictureP.IsVisible = true;
            //submitCurrentPictureL.IsVisible = true;
            latestTakenPath = filepath;

            // This works... but is not neccessarily a square image.
            //latestTakenImgBytes = imgBytes;
            //latestTakenImgBytes = GlobalSingletonHelpers.SquareImage(imgBytes);

            //latestTakenImgP = //GlobalSingletonHelpers.buildFixedRotationImageFromBytes(latestTakenImgBytes);
            latestTakenImgP.Bitmap = GlobalStatusSingleton.latestImg;
            //submitCurrentPictureP.Text = "Enter photo to: " + GlobalSingletonHelpers.getUploadingCategoryDesc();
            submitCurrentPictureP.Text = "Enter photo to: " + CameraContentPage.activeCameraCategory.description;
            //submitCurrentPictureP.IsEnabled = true;

            //buildUI(); // rebuilds are bad. does it work without?
        }

        // click handler for SubmitCurrentPicture.
        protected async virtual void OnSubmitCurrentPicture(object sender, EventArgs e) {
            Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture start");
            if (canClickSubmit == false) {
                tagsEntry.FontAttributes = FontAttributes.Bold;
                await tagsEntry.FadeTo(0, 350);
                await tagsEntry.FadeTo(1, 350);
                await Task.Delay(150);
                tagsEntry.FontAttributes = FontAttributes.None;
                return;
            }
            alertBulb.IsVisible = false;

            // check that button is enabled... xamarin has weak-fu here.
            // now doing special handling...
            //if (((Button)sender).IsEnabled == false) { return; }

            // prevent multiple click attempts; we heard ya
            //submitCurrentPictureP.IsEnabled = false;
            //submitCurrentPictureL.IsEnabled = false;
            canClickSubmit = false;
            submitCurrentPictureP.Text = "Submitting!";

            Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture pre async call");
            if (animate != null) {
                animate(this, new EventArgs());
            }

            debug_checkImgSquare();

            //string result = await sendSubmitAsync(latestTakenImgBytes);
            //string result = await sendSubmitAsync(GlobalStatusSingleton.mostRecentImgBytes);
            while (submitResult.Equals("")) {
                await Task.Delay(250);
            }
            this.animationActive = false;
            Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture post async call");
            try {
                BallotJSONExtended response = JsonConvert.DeserializeObject<BallotJSONExtended>(submitResult);
                Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture post json deserialize");
                if (response.pid > 0) {
                    await photoUpdate(response.pid);
                }
                if (response.ballots != null) {
                    //if (response.message.Equals(PhotoSubmitResponseJSON.SUCCESS_MSG)) {
                    // success. update the UI
                    submitCurrentPictureP.Text = "Congratulations, you're in!";
                    //submitCurrentPictureL.Text = "Congratulations, you're in!";

                    // buildUI();  this now generates the null ref pointer bug. remove. as i think it's irrelevant to how the ui 
                    //   now works anyway.
                    //setView();
                    Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture end");
                    cameraPage.switchToSelectView();
                    //BallotFromPhotoSubmissionEventArgs ballotEvt = new BallotFromPhotoSubmissionEventArgs { ballotString = result, };
                    var ballotSubset = new { response.category, response.ballots };
                    BallotFromPhotoSubmissionEventArgs ballotEvt = new BallotFromPhotoSubmissionEventArgs { ballotString = JsonConvert.SerializeObject(ballotSubset), };
                    cameraPage.fireLoadBallotFromPhotoSubmission(ballotEvt);
                } else {
                    // try a photo deserialize. no ballot was returned, so probably no enough pics!
                    // actually... we already processed the fail case in submitAsync... so here we are good.
                    submitCurrentPictureP.Text = "Congratulations, you're in!";
                    cameraPage.switchToSelectView();
                }
            } catch (Exception err) {
                Debug.WriteLine("DHB:CameraContentPage:OnSubmitCurrentPicture invalid response json: " + submitResult);
                Debug.WriteLine(err.ToString());
                submitCurrentPictureP.Text = "Entry failed - try again";
                //submitCurrentPictureP.IsEnabled = true;
                canClickSubmit = true;
                bulb.IsVisible = false;
                animationActive = false;
                alertBulb.IsVisible = true;
                //buildUI();
            } finally {
                submitResult = "";
                tagsEntry.Text = "";
                uploadStarted = false;  // reset so we can try again.
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
            Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync start");
            //string result = "fail";
            PhotoSubmitResponseJSON resJson = new PhotoSubmitResponseJSON();
            resJson.message = "fail";
            string result = JsonConvert.SerializeObject(resJson);
            try {
                PhotoSubmitJSON submission = new PhotoSubmitJSON();

                submission.imgStr = imgBytes;
                submission.extension = "JPEG";
                //Debug.Assert(GlobalStatusSingleton.uploadingCategories.Count > 0, "DHB:ASSERT!:CameraContentPage:sendSubmitAsync Invalid uploading categories!");
                //Debug.WriteLine(GlobalStatusSingleton.uploadingCategories.Count > 0, "DHB:ASSERT!:CameraContentPage:sendSubmitAsync Invalid uploading categories!");
                //submission.categoryId = GlobalStatusSingleton.uploadingCategories[0].categoryId;
                if (CameraContentPage.activeCameraCategory == null) {
                    result = "No category selected";
                } else {
                    //submission.categoryId = GlobalStatusSingleton.uploadingCategories[0].categoryId;
                    submission.categoryId = CameraContentPage.activeCameraCategory.categoryId;
                    Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync catid: " + CameraContentPage.activeCameraCategory.categoryId);
                    //submission.userId = GlobalStatusSingleton.loginCredentials.userId;
                    // moved this up in case serialization was causing the client to be disposed prematurely...
                    string jsonQuery = JsonConvert.SerializeObject(submission);
                    Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync query serialized");
                    Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync query[0,99]: " + jsonQuery.Substring(0, 100));

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, CameraContentPage.PHOTO);
                    //request.Headers.Add("Authorization", "JWT " + GlobalStatusSingleton.authToken.accessToken);
                    request.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                    request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                    HttpContent baseContent = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                    MemoryStream dummyStream = new MemoryStream();
                    ProgressableStreamContent trackableSend = new ProgressableStreamContent(baseContent, 4096, (sent, total) => { Debug.WriteLine("Uploading {0}/{1}", sent, total); }, imgBytes);
                    //request.Content = trackableSend;
                    Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync request obj built");
                    // string test = request.ToString();

                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));


                    Debug.WriteLine("DHB:CameraContentPage:sendSubmitAsync pre send");
                    HttpResponseMessage submitResult = await client.SendAsync(request);
                    Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync post send");
                    if ((submitResult.StatusCode == System.Net.HttpStatusCode.Created) ||
                        (submitResult.StatusCode == System.Net.HttpStatusCode.OK)) {
                        // tada
                        Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync returned with code:" + submitResult.StatusCode.ToString());
                        result = await submitResult.Content.ReadAsStringAsync();
                        Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync result read in");
                    } else {
                        Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync send statusCode=" + submitResult.StatusCode.ToString());
                        result = "unknown failure";
                    }
                }
            } catch (System.Net.WebException err) {
                // The server was down last time this happened.  Is that the case now, when you are rereading this?
                // Or, is it a connection fail?
                Debug.WriteLine(err.ToString());
                Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync webexception");
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
            Debug.WriteLine("DHB:CameraEnterPhotoView:sendSubmitAsync end");
            return result;
        }

        public void OnBackPressed(object Sender, EventArgs args) {
            // can only come to CreateCategory from the select view, therefore I know 
            // that's where i want to return.
            // not working on ios for some reason. am i getting here?
            Debug.WriteLine("DHB:CameraEnterPhotoView:OnBackPressed");
            this.animationActive = false;
            cameraPage.switchToSelectView();
            //header.backCaretVis(); in theory, handled by back caret itself now... ;)
        }

        bool animationActive = false;
        EventHandler animate;

        public async void AnimationEvent(object sender, EventArgs args) {
            animationActive = true;  // need to reset when coming back in.
            bulb.IsVisible = true;

            while (animationActive) {
                bulb.InvalidateSurface();
                bulb.pct += 0.025;
                await Task.Delay(250);
            }
            bulb.IsVisible = false;
        }

        private void debug_checkImgSquare() {
            SKBitmap test = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(GlobalStatusSingleton.mostRecentImgBytes);
            //SKBitmap test2 = GlobalSingletonHelpers.buildFixedRotationSKBitmapFromBytes(GlobalStatusSingleton.latestImg);
            if (test != null) {
                Debug.WriteLine("DHB:CameraEnterPhotoView:debug_checkImgSquare: w:" + test.Width + ", h:" + test.Height);
            } else {
                Debug.WriteLine("DHB:CameraEnterPhotoView:debug_checkImgSquare: test null??!?");
            }
            bool fake = false;
        }

        bool uploadStarted = false;
        public void tagTextChanged(object sender, TextChangedEventArgs e) {
            //var oldText = e.OldTextValue;
            var newText = e.NewTextValue;
            // Note: the event is triggered by clearing the text as well! :)
            if ((!uploadStarted) && (!newText.Equals(""))) {
                uploadStarted = true;
                if (submitOnTagEntryBegin!= null) {
                    submitOnTagEntryBegin(this, new EventArgs());
                }
            }
            if (newText.Length > 2) {
                //submitCurrentPictureP.IsEnabled = true;
                canClickSubmit = true;
            }
        }

        string submitResult = "";
        public async void OnTagEntryBeginSubmit(object sender, EventArgs args) {
            submitResult = await sendSubmitAsync(GlobalStatusSingleton.mostRecentImgBytes);
            Debug.WriteLine("DHB:CameraEnterPhotoView:OnTagEntryBeginSubmit result: " + submitResult);
        }

        private async Task<string> photoUpdate(long pid) {
            string result = "";
            PhotoUpdateJSON pJSON = new PhotoUpdateJSON();
            //pJSON.flag = saveData.flaggedImg.IsVisible;
            //pJSON.like = saveData.likedImg.IsVisible;
            //pJSON.tags = saveData.tagEntry.Text;   @todo parse and set tags.
            string[] splitters = { " ", "," };
            string[] tagsAsStrings = tagsEntry.Text.Split(splitters, StringSplitOptions.RemoveEmptyEntries);
            pJSON.tags = new List<string>(tagsAsStrings);
            string jsonQuery = JsonConvert.SerializeObject(pJSON);
            if (jsonQuery != null) {
                string apiCall = "update/photo/" + pid;
                result = await GlobalSingletonHelpers.requestFromServerAsync(HttpMethod.Put, apiCall, jsonQuery);
            }
            return result;
        }
    }
}
