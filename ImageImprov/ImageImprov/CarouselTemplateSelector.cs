using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using Xamarin.Forms;

namespace ImageImprov {
    class CarouselTemplateSelector : DataTemplateSelector {
        Dictionary<View, DataTemplate> activeTemplates = new Dictionary<View, DataTemplate>();

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container) {
            //DataTemplate dt = new DataTemplate();
            DataTemplate result = null;
            View civ = (View)item;
            Debug.WriteLine("CarouselTemplateSelector:OnSelectTemplate switch to: " + item.ToString());
            if (activeTemplates.ContainsKey(civ)) {
                result = activeTemplates[civ];
            } else {
                activeTemplates[civ] = new DataTemplate(() => { return civ; });
                result = activeTemplates[civ];
            }
            return result;
        }
    }
}
