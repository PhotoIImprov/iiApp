using System;
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
        public IList<string> fileSetup(string path, string filePrefix) {
            return new List<string>();
        }
    }
}