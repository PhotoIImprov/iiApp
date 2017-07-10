using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ImageImprov {
    /// <summary>
    /// My goal is to keep all dependency service calls in one area, so that if we want to add platforms,
    /// or something changes that is platform specific it is all in one place.
    /// </summary>
    class PlatformSpecificCalls {
        public static IList<string> dirLoad() {
            IFileServices ifs = DependencyService.Get<IFileServices>();
            return ifs.fileSetup("7", "7");
        }
    }
}
