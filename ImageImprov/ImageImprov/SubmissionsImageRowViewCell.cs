using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Diagnostics;

namespace ImageImprov {
    class SubmissionsImageRowViewCell : ViewCell {
        Grid myView;
        iiBitmapView img0;
        iiBitmapView img1;
        iiBitmapView img2;

        public SubmissionsImageRowViewCell() {
            //View.MinimumHeightRequest = 120;
            Height = 120;
            if (Device.Idiom == TargetIdiom.Tablet) {
                Height = 270;
            }
            //this.ForceUpdateSize();
            myView = new Grid { ColumnSpacing = 1, RowSpacing = 1, };
            myView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            myView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            myView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            myView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            img0 = new iiBitmapView() { HorizontalOptions = LayoutOptions.FillAndExpand, MinimumHeightRequest = 120, };
            img1 = new iiBitmapView() { HorizontalOptions = LayoutOptions.FillAndExpand, MinimumHeightRequest = 120, };
            img2 = new iiBitmapView() { HorizontalOptions = LayoutOptions.FillAndExpand, MinimumHeightRequest = 120, };

            img0.SetBinding(iiBitmapView.BitmapProperty, "bitmap0");
            img1.SetBinding(iiBitmapView.BitmapProperty, "bitmap1");
            img2.SetBinding(iiBitmapView.BitmapProperty, "bitmap2");

            img0.SetBinding(iiBitmapView.PhotoMetaProperty, "bmp0Meta");
            img1.SetBinding(iiBitmapView.PhotoMetaProperty, "bmp1Meta");
            img2.SetBinding(iiBitmapView.PhotoMetaProperty, "bmp2Meta");

            TapGestureRecognizer imgTapped = new TapGestureRecognizer();
            imgTapped.Tapped += OnImgTapped;
            img0.GestureRecognizers.Add(imgTapped);
            img1.GestureRecognizers.Add(imgTapped);
            img2.GestureRecognizers.Add(imgTapped);

            myView.Children.Add(img0, 0, 0);
            myView.Children.Add(img1, 1, 0);
            myView.Children.Add(img2, 2, 0);
            myView.MinimumHeightRequest = Height;
            this.View = myView;
        }

        public void OnImgTapped(object sender, EventArgs args) {
            iiBitmapView activeImg = (iiBitmapView)sender;
            Debug.WriteLine("DHB:SubmissionsImageRowViewCell:OnImgTapped: " + Height + " min height:"+View.MinimumHeightRequest);
            if ((activeImg != null) && (activeImg.Bitmap != null)) {
                MasterPage mp = ((MasterPage)Application.Current.MainPage);
                //mp.zoomPage.ActiveMetaBallot = new BallotCandidateJSON(); // nada for now.

                iiBitmapView taggedImg = new iiBitmapView {
                    Bitmap = activeImg.Bitmap.Copy(),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                };
                mp.zoomPage.MainImage = taggedImg;
                //mp.zoomPage.buildZoomView();
                mp.zoomPage.buildZoomFromUserPhotoMeta(activeImg.PhotoMeta);
                // ahh.... there's 2 pieces of info needed now.  The parent page and subpage.
                // eg profile/subs; profile/likes; eventPage/categories
                /*
                mp.zoomPage.PreviousContent = mp.thePages.profilePage;
                //mp.zoomPage.PreviousView = mp.thePages.profilePage.Content;
                mp.thePages.profilePage.PreviousView = mp.thePages.profilePage.Content;
                Device.BeginInvokeOnMainThread(() => {
                    mp.thePages.profilePage.Content = mp.zoomPage.Content;
                });
                */
                ContentView active = null;
                if (mp.thePages.Position == MainPageSwipeUI.CAMERA_PAGE) {
                    mp.zoomPage.PreviousContent = mp.thePages.cameraPage;
                    //mp.thePages.cameraPage.PreviousView = mp.thePages.cameraPage.Content;
                    active = mp.thePages.cameraPage;
                } else if (mp.thePages.Position == MainPageSwipeUI.PROFILE_PAGE) {
                    active = mp.thePages.profilePage;
                    mp.zoomPage.PreviousContent = mp.thePages.profilePage;
                    //mp.zoomPage.PreviousView = mp.thePages.profilePage.Content;
                    mp.thePages.profilePage.PreviousView = active.Content;
                }
                Device.BeginInvokeOnMainThread(() => {
                    active.Content = mp.zoomPage.Content;
                });
            }
            Debug.WriteLine("DHB:SubmissionsImageRowViewCell:OnImgTapped: " + Height);
        }
    }
}

