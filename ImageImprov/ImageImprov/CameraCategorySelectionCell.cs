using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using SkiaSharp;

namespace ImageImprov {
    public class CameraCategorySelectionCell : ViewCell {
        Grid rowSplitter;

        public static SKBitmap cameraImage;
        public const string CAMERA_IMAGE_NAME = "ImageImprov.IconImages.camera_inactive.png";

        iiBitmapView cameraButton;

        Label categoryName = new Label {
            BackgroundColor = GlobalStatusSingleton.ButtonColor,
            Text = "Loading...",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            //VerticalOptions = LayoutOptions.FillAndExpand,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            TextColor = Color.White,
            LineBreakMode = LineBreakMode.WordWrap,
            FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
            FontAttributes = FontAttributes.Bold,
        };

        public CameraCategorySelectionCell() {
            if (cameraImage ==null) {
                cameraImage = GlobalSingletonHelpers.loadSKBitmapFromResourceName(CAMERA_IMAGE_NAME , this.GetType().GetTypeInfo().Assembly);
            }
            rowSplitter = new Grid { ColumnSpacing = 0, RowSpacing = 0, Margin = 1, };
            for (int i=0; i<10; i++) {
                rowSplitter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            categoryName.SetBinding(Label.TextProperty, "categoryName");
            rowSplitter.Children.Add(categoryName, 0, 0);
            Grid.SetColumnSpan(categoryName, 10);

            cameraButton = new iiBitmapView(GlobalSingletonHelpers.loadSKBitmapFromResourceName(CAMERA_IMAGE_NAME, this.GetType().GetTypeInfo().Assembly)) {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = 3,
            };
            rowSplitter.Children.Add(cameraButton, 9, 0);  // col, row
            rowSplitter.HeightRequest = 32;
            this.View = rowSplitter;
        }

    }
}
