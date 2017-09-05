using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    class CameraEventTitleViewCell : ViewCell {
        Grid rowSplitter;

        Label blankLabel = new Label { BackgroundColor = GlobalStatusSingleton.backgroundColor, Text = " ", MinimumHeightRequest = 8, };
        Label eventName = new Label {
            BackgroundColor = GlobalStatusSingleton.SplashBackgroundColor,
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Start,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 32,
        };
        Label accesskeyLabel = new Label {
            BackgroundColor = GlobalStatusSingleton.SplashBackgroundColor,
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 32,
        };

        public CameraEventTitleViewCell() {
            rowSplitter = new Grid {
                ColumnSpacing = 0,
                RowSpacing = 0,
                Margin = 0,
                BackgroundColor = GlobalStatusSingleton.backgroundColor,
            };
            for (int i = 0; i < 10; i++) {
                rowSplitter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            rowSplitter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(.25, GridUnitType.Star) });
            rowSplitter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rowSplitter.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            rowSplitter.Children.Add(blankLabel, 0, 0);

            eventName.SetBinding(Label.TextProperty, "eventName");
            rowSplitter.Children.Add(eventName, 0, 1);
            Grid.SetColumnSpan(eventName, 10);

            accesskeyLabel.SetBinding(Label.TextProperty, "accessKey");
            rowSplitter.Children.Add(accesskeyLabel, 0, 2);
            Grid.SetColumnSpan(accesskeyLabel, 10);
            rowSplitter.HeightRequest = 72;
            this.View = rowSplitter;
        }

    }
}
