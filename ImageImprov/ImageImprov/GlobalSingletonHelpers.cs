using System;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace ImageImprov {
    // a collection of static helper functions that are used throughout the app.
    public static class GlobalSingletonHelpers {
        public static string imageToByteArray(Image img) {
            var file = img.Source;
            return "hai";
            /*
            using (var memStream = new MemoryStream()) {
                file.GetStream().CopyTo(memStream);
                file.Dispose();
                return memStream.ToArray();
            }
            */
                
        }

        public static string getAuthToken() {
            return ("JWT " + GlobalStatusSingleton.authToken.accessToken);
        }

        public static string stripHyphens(string input) {
            string result = "";
            try {
                result = Regex.Replace(input, "-", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
            } catch (RegexMatchTimeoutException) {
                //result = input;  // not a huge fan of this approach...
                result = String.Empty; // or this one.
            }
            return result;
        }

        /// <summary>
        /// Given an input Aspect converts to a bool with
        /// Aspect.AspectFit == true, Aspect.Fill == false and Aspect.AspectFill == false.
        /// This is done to map to the user setting checkbox.
        /// </summary>
        /// <param name="inAspect"></param>
        /// <returns></returns>
        public static bool AspectSettingToBool(Aspect inAspect) {
            return ((inAspect == Aspect.AspectFit) ? true : false);
        }
        
        /// <summary>
        /// Reverse look up of the AspectSettingToBool conversion.
        /// </summary>
        /// <param name="checkedStatus"></param>
        /// <returns></returns>
        public static Aspect BoolToAspectSetting(bool checkedStatus) {
            return (checkedStatus ? Aspect.AspectFit : Aspect.Fill);
        }

    }
}
