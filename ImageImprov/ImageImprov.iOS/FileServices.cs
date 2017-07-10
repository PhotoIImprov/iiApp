using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(ImageImprov.iOS.FileServices))]

namespace ImageImprov.iOS {
    /// <summary>
    /// iOS implementation of IFileServices.
    /// </summary>
    class FileServices : ImageImprov.IFileServices {
        public IList<string> fileSetup(string path, string filePrefix) {
            return new List<string>();
        }
    }
}