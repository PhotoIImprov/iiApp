using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// Workaround object for the inability to make an apple modal dialog translucent.
    /// </summary>
    public interface IOverlayable {
        void pushOverlay(ContentView overlay);
        void popOverlay();
    }
}
