using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    public interface IProvideEventDrillDown {
        /// <summary>
        /// This is the callback to my calling page.
        /// </summary>
        void switchToSelectView();
        /// <summary>
        /// This asks my calling page to display the category view.
        /// </summary>
        /// <param name="category"></param>
        void switchToCategoryImgView(CategoryJSON category);

        /// <summary>
        /// Returns the ui to the event detail page.
        /// </summary>
        void switchToEventView();
    }
}
