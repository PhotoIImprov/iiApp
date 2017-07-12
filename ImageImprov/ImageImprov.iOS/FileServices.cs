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
        public IList<string> getImageImprovFileNames() {
            IList<string> files = new List<string>(System.IO.Directory.EnumerateFiles(GlobalStatusSingleton.imgPath, GlobalStatusSingleton.IMAGE_NAME_PREFIX + "*"));
            return files;
        }

        public byte[] loadImageBytes(string filename) {
            return System.IO.File.ReadAllBytes(filename);
        }

        public int determineNumImagesTaken() {
            // the properties method of determining this is unreliable as it gets the kibosh on reinstalls.
            IList<string> filenames = getImageImprovFileNames();
            int maxId = 0;
            // cut the .jpg. want everything after the last _.
            foreach (string f in filenames) {
                string tmp = f.Substring(0, f.Length - 4);  // cuts the ".jpg"
                int lastIndex = tmp.LastIndexOf("_") + 1;  // dump the _
                int newLen = tmp.Length - lastIndex;
                tmp = tmp.Substring(lastIndex, newLen);
                int current = Convert.ToInt32(tmp);
                maxId = (current > maxId) ? current : maxId;
            }
            return maxId;
        }
    }
}