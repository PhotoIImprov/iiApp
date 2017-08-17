using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ImageImprov {
    class SubmissionsBlankRowViewCell : ViewCell {
        SubmissionsBlankRowViewCell() {
            // null view fubars!
            View = new Label { Text = "", FontSize=2, };
        }
    }
}
