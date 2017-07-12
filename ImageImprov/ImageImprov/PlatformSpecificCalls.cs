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
        public static IList<string> getImageImprovFileNames() {
            IFileServices ifs = DependencyService.Get<IFileServices>();
            return ifs.getImageImprovFileNames();
        }

        public static byte[] loadImageBytes(string filename) {
            IFileServices ifs = DependencyService.Get<IFileServices>();
            return ifs.loadImageBytes(filename);
        }

        public static void authInit() {
            IAuthServices ias = DependencyService.Get<IAuthServices>();
            if (ias != null) {
                ias.Init();
            }
        }
    }
}
