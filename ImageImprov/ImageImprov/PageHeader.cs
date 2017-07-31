using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// Provides the header (generally the "image improv" text) at the top of the screen.
    /// </summary>
    class PageHeader : ContentView {
        public PageHeader() {
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
            return textLogo;
        }
    }
}
