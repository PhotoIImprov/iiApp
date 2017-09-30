using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// Provides the header (generally the "image improv" text) at the top of the screen.
    /// </summary>
    class PageHeader : ContentView {
        iiBitmapView settingsButton;
        iiBitmapView settingsButton_active;

        public PageHeader() {
            Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            settingsButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.settings_inactive.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 4, 
                IsVisible = false, // starts invis.
            };
            TapGestureRecognizer tapped = new TapGestureRecognizer();
            tapped.Tapped += ((e,s) => {
                MasterPage mp = ((MasterPage)Application.Current.MainPage);
                settingsButton_active.IsVisible = true;
                settingsButton.IsVisible = false;
                mp.thePages.profilePage.gotoSettingsPage();
            });
            settingsButton.GestureRecognizers.Add(tapped);

            settingsButton_active = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.settings.png", assembly)) {
                HorizontalOptions = LayoutOptions.End,
                Margin = 4,
                IsVisible = false, // starts invis.
            };

            this.Content = buildTextLogo();
        }

        private Grid buildTextLogo() {
            Grid textLogo = new Grid();
            textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20, GridUnitType.Star) });
            textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(79, GridUnitType.Star) });
            //textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(9, GridUnitType.Star) });
            textLogo.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Image textImg = new Image { Source = ImageSource.FromResource("ImageImprov.IconImages.ii_textlogo.png"), };
            BoxView horizLine = new BoxView { HeightRequest = 1.0, BackgroundColor = GlobalStatusSingleton.highlightColor, HorizontalOptions = LayoutOptions.FillAndExpand, };
            textLogo.Children.Add(textImg, 0, 1);
            textLogo.Children.Add(horizLine, 0, 2);
            textLogo.Children.Add(settingsButton, 0, 1);
            textLogo.Children.Add(settingsButton_active, 0, 1);
            return textLogo;
        }

        public int HighlightedButtonIndex {
            get { return (int)GetValue(HighlightedButton); }
            set {
                SetValue(HighlightedButton, value);
            }
        }

        private static void OnHighlightedButtonChanged(BindableObject bindable, object oldValue, object newValue) {
            var myObj = bindable as PageHeader;
            Device.BeginInvokeOnMainThread(() => {
                if (myObj.HighlightedButtonIndex == MainPageSwipeUI.PROFILE_PAGE) {
                    myObj.settingsButton.IsVisible = true;
                } else {
                    myObj.settingsButton.IsVisible = false;
                }
            });
        }

        public static readonly BindableProperty HighlightedButton
            = BindableProperty.Create("HighlightedButtonIndex", typeof(int), typeof(PageHeader), 0, BindingMode.Default, null, OnHighlightedButtonChanged);

    }
}
