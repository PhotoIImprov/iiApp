using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov {
    /// <summary>
    /// Interface that android and ios dependency services leverage to expose the platform
    /// specific implementations of the FacebookSDK.
    /// </summary>
    public interface I_ii_FacebookLogin {
        void Init();
        void SetLoginCallback(LoginPage callback);
        void startFacebookLogin();
        bool loginCheck();
        void logout();
    }
}
