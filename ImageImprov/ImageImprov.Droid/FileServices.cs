using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms;
[assembly: Dependency(typeof(ImageImprov.Droid.FileServices))]

namespace ImageImprov.Droid {
    /// <summary>
    /// Android implementation of the interface IFileServices
    /// </summary>
    class FileServices : ImageImprov.IFileServices {
        public IList<string> getImageImprovFileNames() {
            //GlobalStatusSingleton.imgPath =
            //    Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();

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
                string tmp = f.Substring(0,f.Length-4);  // cuts the ".jpg"
                int lastIndex = tmp.LastIndexOf("_")+1;  // dump the _
                int newLen = tmp.Length - lastIndex;
                tmp = tmp.Substring(lastIndex, newLen);
                int current = Convert.ToInt32(tmp);
                maxId = (current > maxId) ? current : maxId;
            }
            return maxId;
        }
    }
}