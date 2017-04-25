using System;
using System.Diagnostics;  // for debug assertions.
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace ImageImprov
{
    public class App : Application
    {
        public const string PROPERTY_UUID = "uuid";
        public const string PROPERTY_USERNAME = "username";
        public const string PROPERTY_PWD = "pwd";
        public const string PROPERTY_MAINTAIN_LOGIN = "maintainlogin";
        //public const string PROPERTY_REGISTERED = "registered";

        public App()
        {
            // This is the root  of the application
            // Ensure CheckBox type exists...
            //new CheckBox();

            // load up my persistent properties.
            loadProperties();

            // Right now I'm focusing on the Swipe UI.
            // When that is working, we'll move on to the navigation pane version.
            MainPage = new MainPageSwipeUI();
            // Whatever MainPage class I use, it MUST implement IExposeCamera!
            Debug.Assert(MainPage is IExposeCamera);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            // Not sure why I would use this rather than the constructor...

        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            Properties[PROPERTY_UUID] = GlobalStatusSingleton.UUID;
            Properties[PROPERTY_USERNAME] = GlobalStatusSingleton.username;
            Properties[PROPERTY_PWD] = GlobalStatusSingleton.password;
            Properties[PROPERTY_MAINTAIN_LOGIN] = GlobalStatusSingleton.maintainLogin.ToString();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        // Currently setup correctly. 
        // set all properties to 2nd row to test anon registration.
        private void loadProperties() {
            IDictionary<string, object> properties = Application.Current.Properties;
            if (properties.ContainsKey(PROPERTY_UUID)) {
                GlobalStatusSingleton.UUID = properties[PROPERTY_UUID] as string;
                //GlobalStatusSingleton.UUID = "";
            } // else { not implemented as it gets taken care of by the device specific code.
            if (properties.ContainsKey(PROPERTY_USERNAME)) {
                GlobalStatusSingleton.username = properties[PROPERTY_USERNAME] as string;
                //GlobalStatusSingleton.username = "";
            }
            if (properties.ContainsKey(PROPERTY_PWD)) {
                GlobalStatusSingleton.password = properties[PROPERTY_PWD] as string;
                //GlobalStatusSingleton.password = "";
            }
            if (properties.ContainsKey(PROPERTY_MAINTAIN_LOGIN)) {
                GlobalStatusSingleton.maintainLogin = Convert.ToBoolean(properties[PROPERTY_MAINTAIN_LOGIN] as string);
                //GlobalStatusSingleton.maintainLogin = false;
            }
        }
    }
}
