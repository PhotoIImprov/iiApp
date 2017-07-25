using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static string GetMemoryStatus() {
            string res = "";
            IMemoryService ims = DependencyService.Get<IMemoryService>();
            if (ims != null) {
                res = ims.GetInfo().ToString();
            }
            return res;
        }

        public static void SetupNotification(string title, string message, DateTime executeTime, long requestId) {
            Debug.WriteLine("DHB:PlatformSpecificCalls setupNotification");
            INotifications ins = DependencyService.Get<INotifications>();
            if (ins != null) {
                ins.SetupNotification(title, message, executeTime, requestId);
                Debug.WriteLine("DHB:PlatformSpecificCalls setupNotification sent.");
            }
        }
    }
}
