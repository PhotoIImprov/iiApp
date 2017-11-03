using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov {
    public class CameraSpacerRowViewCell : ViewCell {
        BoxView horizLine = new BoxView { HeightRequest = 2.0, Margin=2, BackgroundColor = GlobalStatusSingleton.highlightColor, HorizontalOptions = LayoutOptions.FillAndExpand, };

        public CameraSpacerRowViewCell() {
            this.View = horizLine;
        }
    }
}
