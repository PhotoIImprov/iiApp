using System;
using System.Diagnostics;  // for debug assertions.
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;
using Newtonsoft.Json;

namespace ImageImprov
{
    public class App : Application
    {
        public const string PROPERTY_UUID = "uuid";
        public const string PROPERTY_USERNAME = "username";
        public const string PROPERTY_PWD = "pwd";
        public const string PROPERTY_MAINTAIN_LOGIN = "maintainlogin";
        public const string PROPERTY_ASPECT_OR_FILL_IMGS = "aspectOrFillImgs";
        public const string PROPERTY_MIN_BALLOTS_TO_LOAD = "minBallotsToLoad";
        public const string PROPERTY_IMGS_TAKEN_COUNT = "imgsTakenCount";
        public const string PROPERTY_ACTIVE_BALLOT = "activeBallot";
        public const string PROPERTY_QUEUE_SIZE = "ballotQueueSize";
        public const string PROPERTY_BALLOT_QUEUE = "ballotQueue";
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
            // Generally preferable to use OnResume, unless this only ever happens once...

        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            Properties[PROPERTY_UUID] = GlobalStatusSingleton.UUID;
            Properties[PROPERTY_USERNAME] = GlobalStatusSingleton.username;
            Properties[PROPERTY_PWD] = GlobalStatusSingleton.password;
            Properties[PROPERTY_MAINTAIN_LOGIN] = GlobalStatusSingleton.maintainLogin.ToString();
            Properties[PROPERTY_ASPECT_OR_FILL_IMGS] = GlobalStatusSingleton.aspectOrFillImgs.ToString();
            Properties[PROPERTY_MIN_BALLOTS_TO_LOAD] = GlobalStatusSingleton.minBallotsToLoad.ToString();
            Properties[PROPERTY_IMGS_TAKEN_COUNT] = GlobalStatusSingleton.imgsTakenTracker.ToString();

            // Is this the correct way to access this stuff?
            // I think so. Retrieval is more complex.
            Properties[PROPERTY_ACTIVE_BALLOT] = JsonConvert.SerializeObject(((MainPageSwipeUI)this.MainPage).GetActiveBallot());
            Properties[PROPERTY_QUEUE_SIZE] = ((MainPageSwipeUI)this.MainPage).GetBallotQueue().Count.ToString();
            for (int i=0;i< ((MainPageSwipeUI)this.MainPage).GetBallotQueue().Count;i++) {
                Properties[PROPERTY_BALLOT_QUEUE+"_"+i] = ((MainPageSwipeUI)this.MainPage).GetBallotQueue().Dequeue();
            }
            
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
                //GlobalStatusSingleton.UUID = properties[PROPERTY_UUID] as string;
                GlobalStatusSingleton.UUID = "";
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
            if (properties.ContainsKey(PROPERTY_ASPECT_OR_FILL_IMGS)) {
                GlobalStatusSingleton.aspectOrFillImgs = ((properties[PROPERTY_ASPECT_OR_FILL_IMGS] as string).Equals("AspectFit") ? Aspect.AspectFit : Aspect.Fill);
            }
            if (properties.ContainsKey(PROPERTY_MIN_BALLOTS_TO_LOAD)) {
                GlobalStatusSingleton.minBallotsToLoad= Convert.ToInt32(properties[PROPERTY_MIN_BALLOTS_TO_LOAD] as string);
            }
            if (properties.ContainsKey(PROPERTY_IMGS_TAKEN_COUNT)) {
                GlobalStatusSingleton.imgsTakenTracker = Convert.ToInt32(properties[PROPERTY_IMGS_TAKEN_COUNT] as string);
            }

            // this is called from the constructor, and before everything exists, so put these into the Global Static for later retrieval.
            // Put this in GlobalStatusSingleton, as I need the above properties set before building the ui.

            if (properties.ContainsKey(PROPERTY_ACTIVE_BALLOT)) {
                //((MainPageSwipeUI)this.MainPage).SetActiveBallot(properties[PROPERTY_ACTIVE_BALLOT] as string);
                GlobalStatusSingleton.persistedBallotAsString = properties[PROPERTY_ACTIVE_BALLOT] as string;
            }
            if (properties.ContainsKey(PROPERTY_QUEUE_SIZE)) {
                int queueSize = Convert.ToInt32(properties[PROPERTY_QUEUE_SIZE] as string);
                Queue<string> loadedQueue = new Queue<string>(queueSize);
                for (int i = 0; i < queueSize; i++) {
                    if (properties.ContainsKey(PROPERTY_BALLOT_QUEUE+"_"+i)) {
                        loadedQueue.Enqueue(properties[PROPERTY_BALLOT_QUEUE + "_" + i] as string);
                    }
                }
                //((MainPageSwipeUI)this.MainPage).SetBallotQueue(loadedQueue);
                GlobalStatusSingleton.persistedPreloadedBallots = loadedQueue;
            }
            
        }
    }
}
