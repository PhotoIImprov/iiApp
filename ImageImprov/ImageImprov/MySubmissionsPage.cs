using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;  // for debug assertions.
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Xamarin.Forms;
using SkiaSharp;
using Newtonsoft.Json;

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
        readonly static string SUBMISSIONS = "submissions";
        readonly static string PREVIEW = "preview";

        public const string LOADING_IMG_NAME = "ImageImprov.IconImages.ii_loading.png";

        Assembly assembly = null;
        Grid portraitView;
        ListView myListView;
        ObservableCollection<SubmissionsRow> submissions = new ObservableCollection<SubmissionsRow>();
        DataTemplateSelector dts = new SubmissionsDataTemplateSelector();

        bool loadingMoreCategories = false;
        long nextLookupId;
        
        struct PhotoLoad {
            public long pid;
            public SubmissionsImageRow drawRow;
            public int index;
            public PhotoLoad(long pid, SubmissionsImageRow row, int idx) {
                this.pid = pid;
                drawRow = row;
                index = idx;
            }
        }
        Queue<PhotoLoad> photosToLoad = new Queue<PhotoLoad>();
        EventHandler processPhotoLoadQueue;

        public MySubmissionsPage() {
            // this starts up the process photo thread.
            processPhotoLoadQueue += OnProcessPhotoLoadQueue;
            if (processPhotoLoadQueue != null) {
                processPhotoLoadQueue(this, new EventArgs());
            }

            assembly = this.GetType().GetTypeInfo().Assembly;
            // get submissions filled somehow.
            buildUI();
        }

        public int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < 16; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            myListView = new ListView { ItemsSource = submissions, ItemTemplate = dts, HasUnevenRows = true, SeparatorVisibility=SeparatorVisibility.None, Margin=0, };
            myListView.ItemAppearing += OnNewCategoryAppearing;

            portraitView.Children.Add(myListView, 0, 0);
            Grid.SetRowSpan(myListView, 16);
            Content = portraitView;
            return 1;
        }



        public void OnPhotoSubmit(object sender, EventArgs args) {
            // buildMyImages();  // this is a SLOOOOW function. just add the latest image
            byte[] raw = ((CameraContentPage)sender).latestTakenImgBytes;
            Image final = GlobalSingletonHelpers.buildFixedRotationImageFromBytes(raw, ExifLib.ExifOrientation.Undefined, 720, 720);
            if (final != null) {
                //myImages.Add(final);
            }
            raw = null;
            buildUI();
        }

        /// <summary>
        /// Testing the reorder of this stuff.
        /// </summary>
        /// <param name="subs"></param>
        private void printSubs(IList<SubmissionJSON> subs) {
            Debug.WriteLine("DHB:MySubmissionsPage:printSubs");
            foreach (SubmissionJSON sub in subs) {
                Debug.WriteLine("DHB:MySubmissionsPage:printSubs Current cat id: " + sub.category.categoryId);
            }
        }

        /// <summary>
        /// Get the submissions whenever categories are loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async virtual void OnCategoryLoad(object sender, EventArgs e) {
            long lookupId = -1;
            if ((GlobalStatusSingleton.uploadingCategories!= null) && (GlobalStatusSingleton.uploadingCategories.Count > 0)) {
                //lookupId = GlobalStatusSingleton.uploadingCategories[0].categoryId;
                // this is some awesome slickness:
                lookupId = GlobalStatusSingleton.uploadingCategories.Max(r => r.categoryId);
            } else if ((GlobalStatusSingleton.votingCategories != null) &&(GlobalStatusSingleton.votingCategories.Count > 0)) {
                lookupId = GlobalStatusSingleton.votingCategories[0].categoryId;
            } else if ((GlobalStatusSingleton.countingCategories != null) && (GlobalStatusSingleton.countingCategories.Count > 0)) {
                lookupId = GlobalStatusSingleton.countingCategories[0].categoryId;
            } else if ((GlobalStatusSingleton.closedCategories != null) && (GlobalStatusSingleton.closedCategories.Count > 0)) {
                lookupId = GlobalStatusSingleton.closedCategories[0].categoryId;
            }
            if (lookupId == -1) return;  // no valid categories!
                                         //lookupId = 10;
            Debug.WriteLine("DHB:MySubmissionsPage:OnCategoryLoad lookup id:" + lookupId);
            await processSubmissionsLoadAsync(lookupId);
        }

        public async Task processSubmissionsLoadAsync(long lookupId) {
            loadingMoreCategories = true;

            string result = "fail";
            while (result.Equals("fail")) {
                result = await requestSubmissionsAsync(lookupId);
                if (result == null) result = "fail";
                if (result.Equals("fail")) {
                    await Task.Delay(10000);
                }
            }
            Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync through request call");

            SubmissionsResponseJSON mySubs = JsonConvert.DeserializeObject<SubmissionsResponseJSON>(result);
            Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync pre if stmt");
            if ((mySubs != null) && (mySubs.submissions!=null) && (mySubs.submissions.Count>0)) {
                Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync post if stmt");
                mySubs.submissions.Sort();
                printSubs(mySubs.submissions);
                mySubs.submissions.Reverse();
                Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync break");
                printSubs(mySubs.submissions);
                foreach (SubmissionJSON subCategory in mySubs.submissions) {
                    Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync inside foreach");
                    //if (subCategory.photos.Count > 0) && (!subCategory.category.state.Equals(CategoryJSON.CLOSED))) {
                    if (subCategory.photos.Count > 0) {
                        if ((GlobalStatusSingleton.pendingCategories.Count == 0) ||
                            ((GlobalStatusSingleton.pendingCategories.Count > 0) && (subCategory.category.categoryId < GlobalStatusSingleton.pendingCategories[0].categoryId))) {
                            SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = subCategory.category.description };
# if DEBUG
                            titleRow.title += " - " + subCategory.category.categoryId;
#endif
                            submissions.Add(titleRow);
                            Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync  about to load: " + subCategory.photos.Count + " photos.");
                            //foreach (PhotoMetaJSON photo in subCategory.photos) {
                            for (int i = 0; i < subCategory.photos.Count; i = i + 3) {
                                int j = i;
                                SubmissionsImageRow imgRow = new SubmissionsImageRow();
                                submissions.Add(imgRow);
                                imgRow.bitmap0 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                imgRow.bmp0Meta = subCategory.photos[j];
                                PhotoLoad pl = new PhotoLoad(subCategory.photos[j].pid, imgRow, 0);
                                photosToLoad.Enqueue(pl);
                                //imgRow.bitmap0 = loadBitmapAsync(subCategory.photos[j].pid).Result;
                                /*
                                Task.Run(async () => {
                                    Debug.WriteLine("DHB:MySubmissionsPage imgload started for img " + j);
                                    imgRow.bitmap0 = await loadBitmapAsync(subCategory.photos[j].pid);
                                    Debug.WriteLine("DHB:MySubmissionsPage imgload finished for img " + j);
                                });
                                */

                                if (j + 1 < subCategory.photos.Count) {
                                    imgRow.bitmap1 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                    imgRow.bmp1Meta = subCategory.photos[j + 1];
                                    //imgRow.bitmap1 = loadBitmapAsync(subCategory.photos[j + 1].pid).Result;
                                    //Task.Run(() => imgRow.bitmap1 = loadBitmapAsync(subCategory.photos[j + 1].pid).Result);
                                    //imgRow.bmp1Meta = subCategory.photos[j + 1];
                                    pl = new PhotoLoad(subCategory.photos[j+1].pid, imgRow, 1);
                                    photosToLoad.Enqueue(pl);
                                }

                                if (j + 2 < subCategory.photos.Count) {
                                    imgRow.bitmap2 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                    imgRow.bmp2Meta = subCategory.photos[j + 2];
                                    pl = new PhotoLoad(subCategory.photos[j+2].pid, imgRow, 2);
                                    photosToLoad.Enqueue(pl);
                                }
                                Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync  complete.");
                            }
                            //SubmissionsBlankRow blank = new SubmissionsBlankRow();
                            //submissions.Add(blank);  // causing issues at this row. skip for now.
                        }
                    }
                    Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync  category: " + subCategory.category.description + " complete.");
                }
                nextLookupId = mySubs.submissions[mySubs.submissions.Count - 1].category.categoryId;
            }
            Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync  complete.");
            loadingMoreCategories = false;
        }

        private async Task<SKBitmap> loadBitmapAsync(long pid, int attempt = 0) {
            Debug.WriteLine("DHB:MySubmissionsPage:loadBitmapAsync depth:" +attempt);
            SKBitmap output = null;
            byte[] result = await requestImageAsync(pid);
            if (result != null) {
                try {
                    /*
                    PreviewResponseJSON resp = JsonConvert.DeserializeObject<PreviewResponseJSON>(result);
                    if (resp != null) {
                        output = GlobalSingletonHelpers.SKBitmapFromBytes(resp.imgStr);
                    }
                    */
                    //output = SKBitmap.Decode(result);
                    output = GlobalSingletonHelpers.SKBitmapFromBytes(result);
                } catch (Exception e) {
                    Debug.WriteLine("DHB:MySubmissionsPage:loadBitmapAsync err:" + e.ToString());
                }
            }
            if (output == null) {
                //output = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly);
                if (attempt < 10) {  // fail after 10 attempts.
                    await Task.Delay(3000);
                    await loadBitmapAsync(pid, attempt + 1);  // will recurse down till we get it.  
                } else {
                    Debug.WriteLine("DHB:MySubmissionsPage:loadBitmapAsync MaxDepth hit");
                    output = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly);
                }
            }
            return output;
        }

        /// <summary>
        /// Currently only requests for categories in the category call.
        /// </summary>
        /// <param name="category_id"></param>
        /// <returns></returns>
        static async Task<string> requestSubmissionsAsync(long category_id) {
            Debug.WriteLine("DHB:MySubmissionsPage:requestSubmissionsAsync start");
            string result = "fail";

            try {
                HttpClient client = new HttpClient();
                string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/prev" + "/" + category_id + "?num_categories=5";
                //string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/next" + "/" + category_id + "?num_categories=5";
                //string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/next" + "/" + System.Convert.ToString(category_id) + "?num_categories=5";
                //string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/next/"+category_id;

                HttpRequestMessage submissionRequest = new HttpRequestMessage(HttpMethod.Get, submissionURL);
                submissionRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage subResult = await client.SendAsync(submissionRequest);
                if (subResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = await subResult.Content.ReadAsStringAsync();
                } else {
                    // no ok back from the server! gahh.
                    Debug.WriteLine("DHB:MySubmissionsPage:requestSubmissionsAsync invalid result code: " + subResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine("DHB:MySubmissionsPage:requestSubmissionsAsync:WebException");
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:MySubmissionsPage:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:MySubmissionsPage:requestSubmissionsAsync end");
            return result;
        }

        static async Task<byte[]> requestImageAsync(long pid) {
            Debug.WriteLine("DHB:MySubmissionsPage:requestImageAsync start pid:" + pid);
            byte[] result = null;// "fail";

            try {
                HttpClient client = new HttpClient();
                string previewURL = GlobalStatusSingleton.activeURL + PREVIEW + "/" + pid;

                HttpRequestMessage previewRequest = new HttpRequestMessage(HttpMethod.Get, previewURL);
                previewRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage previewResult = await client.SendAsync(previewRequest);
                if (previewResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    //result = await previewResult.Content.ReadAsStringAsync();
                    result = await previewResult.Content.ReadAsByteArrayAsync();
                } else {
                    // no ok back from the server! gahh.
                    Debug.WriteLine("DHB:MySubmissionsPage:requestSubmissionsAsync invalid result code: " + previewResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine("DHB:MySubmissionsPage:requestSubmissionsAsync:WebException");
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:MySubmissionsPage:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:MySubmissionsPage:requestImageAsync end pid:" + pid);
            return result;
        }

        private async void OnProcessPhotoLoadQueue(object sender, EventArgs args) {
            while (true) {
                if (photosToLoad.Count > 0) {
                    PhotoLoad pl = photosToLoad.Dequeue();
                    Debug.WriteLine("DHB:OnProcessPhotoLoadQueue loading:" + pl.pid);
                    switch (pl.index) {
                        case 0:
                            pl.drawRow.bitmap0 = await loadBitmapAsync(pl.pid);
                            break;
                        case 1:
                            pl.drawRow.bitmap1 = await loadBitmapAsync(pl.pid);
                            break;
                        case 2:
                            pl.drawRow.bitmap2 = await loadBitmapAsync(pl.pid);
                            break;
                        default:
                            Debug.WriteLine("DHB:MySubmissionsPage:OnProcessPhotoLoadQueue invalid index");
                            break;
                    }
                } else {
                    await Task.Delay(15000);
                    // should shut this down after some period of time...
                }
            }
        }

        private void OnNewCategoryAppearing(object sender, ItemVisibilityEventArgs args) {
            if (loadingMoreCategories || submissions.Count == 0) {
                return;  // already getting more.
            }
            if (args.Item == submissions[submissions.Count - 1]) {
                loadingMoreCategories = true;
                processSubmissionsLoadAsync(nextLookupId);
            }
        }
    }
}
