using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// This page is used to show webpages that we need to show, such as terms of service or privacy policy.
    /// Using a view rather than page as I believe this will always be sub to something else, eg PlayerContent or Hamburger pages.
    /// Built as a singleton so that it  is only loaded once, regardless of where in the app i grab it.
    /// </summary>
    class iiWebPage : ContentView {
        private Button backButton;
        private WebView webView;
        private Grid portraitView;
        private Label LoadingLabel = new Label {
            TextColor = GlobalStatusSingleton.ButtonColor,
            Text = "Loading...",
        };

        // Not visible UI elements on this page.  These are for when I close.
        ContentView parent;
        View returnPoint;

        private static Dictionary<string,iiWebPage> activeWebPages;

        private iiWebPage(ContentView parent, View returnPoint) {
            this.parent = parent;
            setReturnPoint(returnPoint);
        }

        public static iiWebPage getInstance(string urlString, ContentView parent, View returnPoint) {
            iiWebPage result = null;
            if (activeWebPages == null) {
                activeWebPages = new Dictionary<string, iiWebPage>();
            }
            if (activeWebPages.ContainsKey(urlString)) {
                result = activeWebPages[urlString];
                result.setReturnPoint(returnPoint);

            } else {
                result = new ImageImprov.iiWebPage(parent, returnPoint);
                result.setupWebpage(urlString);
                activeWebPages[urlString] = result;
            }
            return result;
        }

        public void setupWebpage(string myUrl) {
            if (webView == null) {
                webView = new WebView();
                webView.Navigating += webOnNavigating;
                webView.Navigated += webOnEndNavigating;
            }
            webView.Source = myUrl;
            buildUI();
        }

        public void setReturnPoint(View returnPoint) {
            this.returnPoint = returnPoint;
            if (backButton == null) {
                backButton = new Button
                {
                    Text = "Back to Image Improv",
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    TextColor = Color.Black,
                    BackgroundColor = GlobalStatusSingleton.ButtonColor,
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                };
                backButton.Clicked += (sender, args) => {
                    // debugging. at some point returnPoint somehow transforms from the login page to the leaderboard page...
                    parent.Content = this.returnPoint;
                };
            }
        }

        public void buildUI() {
            if (portraitView == null) {
                portraitView = new Grid { ColumnSpacing = 1, RowSpacing = 1, BackgroundColor = GlobalStatusSingleton.backgroundColor, };
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                portraitView.RowDefinitions.Add(new RowDefinition { Height = new GridLength(9, GridUnitType.Star) });
            }
            portraitView.Children.Add(backButton, 0, 0);
            portraitView.Children.Add(LoadingLabel, 0, 1);
            portraitView.Children.Add(webView, 0, 1);
            Content = portraitView;
        }

        //
        // Be Nice when loading...
        //
        void webOnNavigating(object sender, WebNavigatingEventArgs e) {
            LoadingLabel.IsVisible = true;
        }

        void webOnEndNavigating(object sender, WebNavigatedEventArgs e) {
            LoadingLabel.IsVisible = false;
        }

    }
}
