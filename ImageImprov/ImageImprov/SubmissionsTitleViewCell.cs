using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ImageImprov {
    class SubmissionsTitleViewCell : ViewCell {
        Label categoryDescription;

        public SubmissionsTitleViewCell() {
            categoryDescription = new Label {
                BackgroundColor = GlobalStatusSingleton.ButtonColor,
                TextColor = Color.White,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                //WidthRequest = Width,
                MinimumHeightRequest = Height / 15.0,
                FontSize = 30, // program later
            };
            categoryDescription.SetBinding(Label.TextProperty, "title");

            this.View = categoryDescription;
        }
    }
}
