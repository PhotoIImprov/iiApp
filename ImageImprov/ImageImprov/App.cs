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
        public App()
        {
            // This is the root  of the application
            // Right now I'm focusing on the Swipe UI.
            // When that is working, we'll move on to the navigation pane version.
            MainPage = new MainPageSwipeUI();
            // Whatever MainPage class I use, it MUST implement IExposeCamera!
            Debug.Assert(MainPage is IExposeCamera);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
