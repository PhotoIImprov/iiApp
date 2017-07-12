using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

//[assembly: Dependency(typeof(ImageImprov.Droid.AuthServices))]
[assembly: ExportRenderer(typeof (ImageImprov.OAuthLoginPage), typeof(ImageImprov.Droid.AuthServices))]
namespace ImageImprov.Droid {
    /*[Activity(Label = "CustomUrlSchemeInterceptorActivity", NoHistory = true, LaunchMode = LaunchMode.SingleTop)]
    [IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "com.googleusercontent.apps.863494635082-a617p00qkuafejrf3k7hq8s346l0u36h" },
    DataPath = "/ii_oauth2redirect")]
    */
    class AuthServices : PageRenderer { //, ImageImprov.IAuthServices {
        //public void Init() {
          //  var activity = this.Context as Activity;
            //activity.StartActivity(ThirdPartyAuthenticator.authenticator.GetUI(activity));
            //StartActivity(ThirdPartyAuthenticator.authenticator.GetUI(this));
        //}

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e) {
            base.OnElementChanged(e);
            var activity = this.Context as Activity;
            activity.StartActivity(ThirdPartyAuthenticator.authenticator.GetUI(activity));
        }

        //public void Init() {
        //global::Android.Content.Intent ui_obj = ThirdPartyAuthenticator.authenticator.GetUI(this);
        //StartActivity(ui_obj);
        /* This code only works with sdk version 23.
        // @todo grab this properly.
        Bundle bundle = null;
        global::Xamarin.Auth.Presenters.XamarinAndroid.AuthenticationConfiguration.Init(Android.App.Application.Context, bundle);
        */
        //}

        // This is an sdk23 thing.
        /*
        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            // Convert Android.Net.Url to Uri
            var uri = new Uri(Intent.Data.ToString());
            // Load redirectUrl page
            ThirdPartyAuthenticator.authenticator.OnPageLoading(uri);

            Finish();
        }
        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);
            global::Android.Content.Intent ui_obj = ThirdPartyAuthenticator.authenticator.GetUI(this);
            StartActivity(ui_obj);

        }
        */
    }
}