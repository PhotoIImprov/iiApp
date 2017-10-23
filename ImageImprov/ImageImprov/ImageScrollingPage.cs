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
    public abstract class ImageScrollingPage : ContentView {
        public const string IMG_FILENAME_PREFIX = "ImageImprov_";
        protected readonly static string PREVIEW = "preview";
        public const string LOADING_IMG_NAME = "ImageImprov.IconImages.ii_loading.png";
        protected const string EMPTY = "EMPTY";

        protected string activeApiCall = "";
        protected Assembly assembly = null;
        protected Grid portraitView;
        protected int numGridRows = 16;
        protected ListView myListView;
        protected ObservableCollection<SubmissionsRow> submissions = new ObservableCollection<SubmissionsRow>();
        protected DataTemplateSelector dts = new SubmissionsDataTemplateSelector();

        protected bool loadingMoreCategories = false;
        protected long nextLookupId;

        public class PhotoLoad {
            public long pid;
            public SubmissionsImageRow drawRow;
            public int index;
            public PhotoLoad(long pid, SubmissionsImageRow row, int idx) {
                this.pid = pid;
                drawRow = row;
                index = idx;
            }
        }
        protected Queue<PhotoLoad> photosToLoad = new Queue<PhotoLoad>();
        protected EventHandler processPhotoLoadQueue;

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

        public ImageScrollingPage() {
            // this starts up the process photo thread.
            processPhotoLoadQueue += OnProcessPhotoLoadQueue;
            if (processPhotoLoadQueue != null) {
                processPhotoLoadQueue(this, new EventArgs());
            }

            assembly = this.GetType().GetTypeInfo().Assembly;
            // get submissions filled somehow.
            buildUI();
        }

        public abstract int buildUI();
        public abstract /* async */ Task processImageLoadAsync(long lookupId);

        /// <summary>
        /// Need a better name for this function.
        /// Essentially, it repeats loading until I get a good load.
        /// </summary>
        /// <param name="lookupId"></param>
        /// <returns></returns>
        protected virtual async Task<string> failProcessing(long lookupId) {
            string result = "fail";
            while (result.Equals("fail")) {
                result = await requestApiCallAsync(lookupId, activeApiCall);
                if (result == null) result = "fail";
                if (result.Equals(EMPTY) && lookupId > 0) {
                    lookupId -= 5;
                    result = "fail";
                }
                if (result.Equals("fail")) {
                    await Task.Delay(10000);
                }
            }
            return result;
        }

        /// <summary>
        /// Get the entries whenever categories are loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async virtual void OnCategoryLoad(object sender, EventArgs e) {
            long lookupId = -1;
            if ((GlobalStatusSingleton.uploadingCategories != null) && (GlobalStatusSingleton.uploadingCategories.Count > 0)) {
                //lookupId = GlobalStatusSingleton.uploadingCategories[0].categoryId;
                // this is some awesome slickness:
                lookupId = GlobalStatusSingleton.uploadingCategories.Max(r => r.categoryId);
            } else if ((GlobalStatusSingleton.votingCategories != null) && (GlobalStatusSingleton.votingCategories.Count > 0)) {
                lookupId = GlobalStatusSingleton.votingCategories[0].categoryId;
            } else if ((GlobalStatusSingleton.countingCategories != null) && (GlobalStatusSingleton.countingCategories.Count > 0)) {
                lookupId = GlobalStatusSingleton.countingCategories[0].categoryId;
            } else if ((GlobalStatusSingleton.closedCategories != null) && (GlobalStatusSingleton.closedCategories.Count > 0)) {
                lookupId = GlobalStatusSingleton.closedCategories[0].categoryId;
            }
            if (lookupId == -1) return;  // no valid categories!
                                         //lookupId = 10;
            Debug.WriteLine("DHB:ImageScrollingPage:OnCategoryLoad lookup id:" + lookupId);
            await processImageLoadAsync(lookupId);
        }

        protected virtual void OnNewCategoryAppearing(object sender, ItemVisibilityEventArgs args) {
            if (loadingMoreCategories || submissions.Count == 0) {
                return;  // already getting more.
            }
            // need to check submissions count or the "No Likes" title row will spam the server...
            if ((args.Item == submissions[submissions.Count - 1]) && (submissions.Count > 1)) {
                loadingMoreCategories = true;
                processImageLoadAsync(nextLookupId);
            }
        }

        public static async Task<byte[]> requestImageAsync(long pid) {
            Debug.WriteLine("DHB:ImageScrollingPage:requestImageAsync start pid:" + pid);
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
                    Debug.WriteLine("DHB:ImageScrollingPage:requestSubmissionsAsync invalid result code: " + previewResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine("DHB:ImageScrollingPage:requestSubmissionsAsync:WebException");
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:ImageScrollingPage:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:ImageScrollingPage:requestImageAsync end pid:" + pid);
            return result;
        }

        /// <summary>
        /// created an instance in globalsingletonhelpers.
        /// @deprecated
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="attempt"></param>
        /// <returns></returns>
        protected async Task<SKBitmap> loadBitmapAsync(long pid, int attempt = 0) {
            Debug.WriteLine("DHB:ImageScrollingPage:loadBitmapAsync depth:" + attempt);
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
                    Debug.WriteLine("DHB:ImageScrollingPage:loadBitmapAsync err:" + e.ToString());
                }
            }
            if (output == null) {
                //output = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly);
                if (attempt < 10) {  // fail after 10 attempts.
                    await Task.Delay(3000);
                    await loadBitmapAsync(pid, attempt + 1);  // will recurse down till we get it.  
                } else {
                    Debug.WriteLine("DHB:ImageScrollingPage:loadBitmapAsync MaxDepth hit");
                    output = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly);
                }
            }
            return output;
        }

        protected async void OnProcessPhotoLoadQueue(object sender, EventArgs args) {
            while (true) {
                if (photosToLoad.Count > 0) {
                    PhotoLoad pl = photosToLoad.Dequeue();
                    Debug.WriteLine("DHB:ImageScrollingPage:OnProcessPhotoLoadQueue loading:" + pl.pid);
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
                            Debug.WriteLine("DHB:ImageScrollingPage:OnProcessPhotoLoadQueue invalid index");
                            break;
                    }
                } else {
                    await Task.Delay(15000);
                    // should shut this down after some period of time...
                }
            }
        }

        /// <summary>
        /// Currently only requests for categories in the category call.
        /// </summary>
        /// <param name="category_id"></param>
        /// <returns></returns>
        protected static async Task<string> requestApiCallAsync(long category_id, string activeApiCall) {
            Debug.WriteLine("DHB:ImageScrollingPage:requestApiCallAsync start");
            string result = "fail";

            try {
                HttpClient client = new HttpClient();
                string submissionURL = GlobalStatusSingleton.activeURL + activeApiCall + "/prev" + "/" + category_id + "?num_categories=5";
                //string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/next" + "/" + category_id + "?num_categories=5";
                //string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/next" + "/" + System.Convert.ToString(category_id) + "?num_categories=5";
                //string submissionURL = GlobalStatusSingleton.activeURL + SUBMISSIONS + "/next/"+category_id;

                HttpRequestMessage submissionRequest = new HttpRequestMessage(HttpMethod.Get, submissionURL);
                submissionRequest.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage subResult = await client.SendAsync(submissionRequest);
                if (subResult.StatusCode == System.Net.HttpStatusCode.OK) {
                    result = await subResult.Content.ReadAsStringAsync();
                } else if ((subResult.StatusCode == System.Net.HttpStatusCode.NoContent) || (subResult.StatusCode == System.Net.HttpStatusCode.NotFound)) {
                    result = EMPTY;
                } else {
                    // no ok back from the server! gahh.
                    Debug.WriteLine("DHB:ImageScrollingPage:requestApiCallAsync invalid result code: " + subResult.StatusCode.ToString());
                }
            } catch (System.Net.WebException err) {
                Debug.WriteLine("DHB:ImageScrollingPage:requestApiCallAsync:WebException");
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:ImageScrollingPage:requestApiCallAsync:Exception");
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:ImageScrollingPage:requestApiCallAsync end");
            return result;
        }
    }
}
