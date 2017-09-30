using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

using Xamarin.Forms;
using Newtonsoft.Json;

namespace ImageImprov {
    class EventCategoryImagesPage : ImageScrollingPage {
        readonly static string PHOTO = "photo";
        CameraContentPage cameraPage;

        // Want to keep photos in memory so that we don't implode when we switch between categories.
        // need submissions too...
        Dictionary<CategoryJSON, ObservableCollection<SubmissionsRow>> storedPhotos = new Dictionary<CategoryJSON, ObservableCollection<SubmissionsRow>>();

        private CategoryJSON _ActiveCategory;
        public CategoryJSON ActiveCategory {
            get { return _ActiveCategory; }
            set {
                _ActiveCategory = value;
                if (storedPhotos.ContainsKey(_ActiveCategory)) {
                    // may need to be in app thread. and may need to trigger UPDATE!

                    // not sure if this would work or not... my concern here is external functions that rely on submissions
                    myListView.ItemsSource = storedPhotos[_ActiveCategory];  
                    submissions = storedPhotos[_ActiveCategory];
                } else {
                    // should be empty... so I can just take the base submissions object as my own.
                    storedPhotos[_ActiveCategory] = new ObservableCollection<SubmissionsRow>();
                    myListView.ItemsSource = storedPhotos[_ActiveCategory];
                    submissions = storedPhotos[_ActiveCategory];
                    processImageLoadAsync(_ActiveCategory.categoryId);
                }
            }
        }

        iiBitmapView backCaret = null;

        public EventCategoryImagesPage(CameraContentPage parent) : base() {
            cameraPage = parent;
            activeApiCall = PHOTO;
        }

        public override int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < numGridRows; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                buildBackCaret();
            }
            myListView = new ListView { ItemsSource = submissions, ItemTemplate = dts, HasUnevenRows = true, SeparatorVisibility = SeparatorVisibility.None, Margin = 0, };
            myListView.ItemAppearing += OnNewCategoryAppearing;  // get more images!

            portraitView.Children.Add(myListView, 0, 0);
            Grid.SetRowSpan(myListView, numGridRows);
            portraitView.Children.Add(backCaret, 0, 0);
            Content = portraitView;
            return 1;
        }

        private void buildBackCaret() {
            backCaret = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.backbutton.png", assembly)) {
                Margin = 4,
                HorizontalOptions = LayoutOptions.Start,
            };
            TapGestureRecognizer back = new TapGestureRecognizer();
            back.Tapped += (e, a) => {
                cameraPage.switchToEventView();
            };
            backCaret.GestureRecognizers.Add(back);
        }

        public async override void OnCategoryLoad(object sender, EventArgs e) {
            // This functions is for responding to the CategoryLoad event.
            // This child class should not load every category, just select ones.
            // Override the parent to ensure nothing untoward happens.
        }

        // this replaces the default imageScrollingPage code as this renderer only shows 1 category at a time...
        // in this instance I actually need to request more images, not more categories.
        protected override void OnNewCategoryAppearing(object sender, ItemVisibilityEventArgs args) {
            // load more imgs.
        }

        protected override async Task<string> failProcessing(long lookupId) {
            string result = "fail";
            while (result.Equals("fail")) {
                result = await requestImagesAsync(lookupId, 0);  // need to update this bit.
                if (result == null) result = "fail";
                // This doesn't happen here, as if i fail on the lookup i stop.
                //if (result.Equals(EMPTY) && lookupId > 0) {
                //    lookupId -= 5;
                //      result = "fail";
                //}
                if (result.Equals("fail")) {
                    await Task.Delay(10000);
                }
            }
            return result;
        }

        public override async Task processImageLoadAsync(long lookupId) {
            loadingMoreCategories = true;

            string result = await failProcessing(lookupId);
            Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync through request call");

            if ((result.Equals(EMPTY)) && (submissions.Count == 0)) {
                SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = "No entries yet, be the first!", };
                submissions.Add(titleRow);
            } else {
                try {
                    // Results can be empty. This means we have some results already, but no more from older stuff. 
                    if (!result.Equals(EMPTY)) {
                        PhotosResponseJSON catImages = JsonConvert.DeserializeObject<PhotosResponseJSON>(result);  // need to update
                        Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync pre if stmt");
                        if ((catImages != null) && (catImages.photos != null) && (catImages.photos.Count > 0)) {
                            Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync post if stmt");
                            catImages.photos.Sort();
                            catImages.photos.Reverse();  // hmm.... harry may have done this already...
                            Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync break");

                            // title row
                            // @todo There's a race condition here... shitshers.
                            SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = _ActiveCategory.description };
#if DEBUG
                            titleRow.title += " - " + _ActiveCategory.categoryId;
#endif
                            submissions.Add(titleRow);
                            Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync about to load: " + catImages.photos.Count + " photos.");
                            //foreach (PhotoMetaJSON photo in subCategory.photos) {
                            for (int i = 0; i < catImages.photos.Count; i = i + 3) {
                                int j = i;
                                SubmissionsImageRow imgRow = new SubmissionsImageRow();
                                submissions.Add(imgRow);
                                imgRow.bitmap0 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                imgRow.bmp0Meta = catImages.photos[j];
                                PhotoLoad pl = new PhotoLoad(catImages.photos[j].pid, imgRow, 0);
                                photosToLoad.Enqueue(pl);
                                //imgRow.bitmap0 = loadBitmapAsync(subCategory.photos[j].pid).Result;

                                if (j + 1 < catImages.photos.Count) {
                                    imgRow.bitmap1 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                    imgRow.bmp1Meta = catImages.photos[j + 1];
                                    //imgRow.bitmap1 = loadBitmapAsync(subCategory.photos[j + 1].pid).Result;
                                    //Task.Run(() => imgRow.bitmap1 = loadBitmapAsync(subCategory.photos[j + 1].pid).Result);
                                    //imgRow.bmp1Meta = subCategory.photos[j + 1];
                                    pl = new PhotoLoad(catImages.photos[j + 1].pid, imgRow, 1);
                                    photosToLoad.Enqueue(pl);
                                }

                                if (j + 2 < catImages.photos.Count) {
                                    imgRow.bitmap2 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                    imgRow.bmp2Meta = catImages.photos[j + 2];
                                    pl = new PhotoLoad(catImages.photos[j + 2].pid, imgRow, 2);
                                    photosToLoad.Enqueue(pl);
                                }
                                Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync  complete.");
                            }
                            //SubmissionsBlankRow blank = new SubmissionsBlankRow();
                            //submissions.Add(blank);  // causing issues at this row. skip for now.
                        }
                        Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync  category: " + _ActiveCategory.description + " complete.");
                        nextLookupId = catImages.photos[catImages.photos.Count - 1].pid;                            
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync PhotosResponseJSON crash");
                    Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync input json:" + result);
                    Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync PhotosResponseJSON crash Done.");
                }
            }
            Debug.WriteLine("DHB:EventCategoryImagesPage:processImageLoadAsync  complete.");
            loadingMoreCategories = false;
        }

        protected static async Task<string> requestImagesAsync(long category_id, long pid = 0) {
            Debug.WriteLine("DHB:EventCategoryImagesPage:requestApiCallAsync start");
            string result = "fail";

            try {
                HttpClient client = new HttpClient();
                string submissionURL = GlobalStatusSingleton.activeURL + PHOTO + "/" + +category_id + "/next/" + pid;

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
