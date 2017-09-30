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
    /// Leverages the like api call to show all the images a user has "liked"/Favorited/Bookmarked.
    /// </summary>
    public class LikesPage : ImageScrollingPage {
        readonly static string LIKES = "like";

        public LikesPage() : base() {
            activeApiCall = LIKES;
        }

        public override int buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                for (int i = 0; i < numGridRows; i++) {
                    portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }
                portraitView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            myListView = new ListView { ItemsSource = submissions, ItemTemplate = dts, HasUnevenRows = true, SeparatorVisibility = SeparatorVisibility.None, Margin = 0, };
            myListView.ItemAppearing += OnNewCategoryAppearing;

            portraitView.Children.Add(myListView, 0, 0);
            Grid.SetRowSpan(myListView, numGridRows);
            Content = portraitView;
            return 1;
        }


        public override async Task processImageLoadAsync(long lookupId) {
            loadingMoreCategories = true;

            string result = await failProcessing(lookupId);
            Debug.WriteLine("DHB:LikesPage:processImageLoadAsync through request call");

            if ((result.Equals(EMPTY)) && (submissions.Count == 0)) {
                SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = "No favorites yet", };
                submissions.Add(titleRow);
            } else {
                try {
                    // Results can be empty. This means we have some results already, but no more from older stuff. 
                    if (!result.Equals(EMPTY)) {
                        LikesResponseJSON myLikes = JsonConvert.DeserializeObject<LikesResponseJSON>(result);
                        Debug.WriteLine("DHB:LikesPage:processImageLoadAsync pre if stmt");
                        if ((myLikes != null) && (myLikes.likes != null) && (myLikes.likes.Count > 0)) {
                            Debug.WriteLine("DHB:LikesPage:processImageLoadAsync post if stmt");
                            myLikes.likes.Sort();
                            myLikes.likes.Reverse();  // hmm.... harry may have done this already...
                            Debug.WriteLine("DHB:LikesPage:processImageLoadAsync break");
                            foreach (LikesJSON subCategory in myLikes.likes) {
                                Debug.WriteLine("DHB:LikesPage:processImageLoadAsync inside foreach");
                                if ((subCategory.photos != null) && (subCategory.photos.Count > 0)) {
                                    SubmissionsTitleRow titleRow = new SubmissionsTitleRow { title = subCategory.category.description };
#if DEBUG
                                    titleRow.title += " - " + subCategory.category.categoryId;
#endif
                                    submissions.Add(titleRow);
                                    Debug.WriteLine("DHB:LikesPage:processImageLoadAsync about to load: " + subCategory.photos.Count + " photos.");
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
                                        Debug.WriteLine("DHB:LikesPage:processImageLoadAsync  complete.");
                                    }
                                    //SubmissionsBlankRow blank = new SubmissionsBlankRow();
                                    //submissions.Add(blank);  // causing issues at this row. skip for now.
                                }
                                Debug.WriteLine("DHB:LikesPage:processImageLoadAsync  category: " + subCategory.category.description + " complete.");
                            }
                            nextLookupId = myLikes.likes[myLikes.likes.Count - 1].category.categoryId;
                        }
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("DHB:LikesPage:processImageLoadAsync LikesResponseJSON crash");
                    Debug.WriteLine("DHB:LikesPage:processImageLoadAsync input json:" + result);
                    Debug.WriteLine("DHB:LikesPage:processImageLoadAsync LikesResponseJSON crash Done.");
                }
            }
            Debug.WriteLine("DHB:LikesPage:processImageLoadAsync  complete.");
            loadingMoreCategories = false;
        }
    }
}