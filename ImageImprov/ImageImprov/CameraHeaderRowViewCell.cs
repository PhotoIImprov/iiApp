using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    public class CameraHeaderRowViewCell : ViewCell {
        Label eventName = new Label {
            BackgroundColor = GlobalStatusSingleton.backgroundColor,
            Text = "Open Categories",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            //VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.Gray,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 32,
        };

        public CameraHeaderRowViewCell() {
            eventName.SetBinding(Label.TextProperty, "title");
            eventName.HeightRequest = 32;
            this.View = eventName;
        }
    }
}
