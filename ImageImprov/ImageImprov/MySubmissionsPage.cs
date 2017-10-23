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
    public class MySubmissionsPage : ImageScrollingPage {
        readonly static string SUBMISSIONS = "submissions";


        public MySubmissionsPage() : base() {
            activeApiCall = SUBMISSIONS;
        }

        public override int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < numGridRows; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                myListView = new ListView(ListViewCachingStrategy.RecycleElement) {
                    ItemsSource = submissions,
                    ItemTemplate = dts,
                    HasUnevenRows = true,
                    SeparatorVisibility = SeparatorVisibility.None,
                    Margin = 0,
                };
                myListView.ItemAppearing += OnNewCategoryAppearing;
                
            }
            portraitView.Children.Clear();
            portraitView.Children.Add(myListView, 0, 0);
            Grid.SetRowSpan(myListView, numGridRows);
            Content = portraitView;
            return 1;
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

        public override async Task processImageLoadAsync(long lookupId) {
            loadingMoreCategories = true;

            /*
            string result = "fail";
            while (result.Equals("fail")) {
                result = await requestApiCallAsync(lookupId, activeApiCall);
                if (result == null) result = "fail";
                if (result.Equals("fail")) {
                    await Task.Delay(10000);
                }
            }
            */
            string result = await failProcessing(lookupId);

            Debug.WriteLine("DHB:MySubmissionsPage:processImageLoadAsync through request call");

            if (result.Equals(EMPTY)) {
                SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = "You have not yet submitted a photo", };
                if (submissions.Count == 0) {
                    submissions.Add(titleRow);
                }
            } else {
                SubmissionsResponseJSON mySubs = JsonConvert.DeserializeObject<SubmissionsResponseJSON>(result);
                Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync pre if stmt");
                if ((mySubs != null) && (mySubs.submissions != null) && (mySubs.submissions.Count > 0)) {
                    Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync post if stmt");
                    mySubs.submissions.Sort();
                    printSubs(mySubs.submissions);
                    mySubs.submissions.Reverse();
                    Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync break");
                    printSubs(mySubs.submissions);
                    foreach (SubmissionJSON subCategory in mySubs.submissions) {
                        Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync inside foreach");
                        //if (subCategory.photos.Count > 0) && (!subCategory.category.state.Equals(CategoryJSON.CLOSED))) {
                        if ((subCategory.photos!=null) && (subCategory.photos.Count > 0)) {
                            if ((GlobalStatusSingleton.pendingCategories.Count == 0) ||
                                ((GlobalStatusSingleton.pendingCategories.Count > 0) && (subCategory.category.categoryId < GlobalStatusSingleton.pendingCategories[0].categoryId))) {
                                SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = subCategory.category.description };
#if DEBUG
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
                                        pl = new PhotoLoad(subCategory.photos[j + 1].pid, imgRow, 1);
                                        photosToLoad.Enqueue(pl);
                                    }

                                    if (j + 2 < subCategory.photos.Count) {
                                        imgRow.bitmap2 = GlobalSingletonHelpers.loadSKBitmapFromResourceName(LOADING_IMG_NAME, assembly);
                                        imgRow.bmp2Meta = subCategory.photos[j + 2];
                                        pl = new PhotoLoad(subCategory.photos[j + 2].pid, imgRow, 2);
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
            }
            // this did not fix the problem.
            //myListView.ItemsSource = null;  // testing whether this clears up my observable collection changed issue.
            //myListView.ItemsSource = submissions;  // testing whether this clears up my observable collection changed issue.
            Debug.WriteLine("DHB:MySubmissionsPage:processSubmissionsLoadAsync  complete.");
            loadingMoreCategories = false;
        }
    }
}
