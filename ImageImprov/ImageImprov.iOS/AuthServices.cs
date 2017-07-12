using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

[assembly: Dependency(typeof(ImageImprov.iOS.AuthServices))]
namespace ImageImprov.iOS {
    class AuthServices : IAuthServices {
        public void Init() {
            global::Xamarin.Auth.Presenters.XamarinIOS.AuthenticationConfiguration.Init();
        }
    }
}
