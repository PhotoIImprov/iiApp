using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

using Xamarin.Forms;

namespace ImageImprov
{
    public delegate void MyLoginEventHandler(object sender, EventArgs e);

    /*
     * @todo Store user's credentials for login on app restart
     * @todo Enable user credential input
     * @todo Build registration UI and interactions
     */
    class PlayerContentPage : ContentPage
    {

        //< loggedInLabel
        readonly Label loggedInLabel = new Label
        {
            Text = "Connecting...",
            HorizontalOptions = LayoutOptions.CenterAndExpand,
            VerticalOptions = LayoutOptions.CenterAndExpand,
        };
        //> loggedInLabel

        public event MyLoginEventHandler MyLogin;
        EventArgs eDummy = null;
        // This stores our oauth credentials once the client has successfully logged in.
        AuthenticationToken token;

        public PlayerContentPage()
        {
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    loggedInLabel,
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go left for voting" },
                    new Label { HorizontalTextAlignment = TextAlignment.Center, Text = "Go right to enter" },
                }
            };

            // set myself up to listen for the login events...
            this.MyLogin += new MyLoginEventHandler(OnMyLogin);
            // fire a loginRequestEvent.
            eDummy = new EventArgs(); ;
            if (MyLogin != null)
            {
                MyLogin(this, eDummy);
            }

        }

        protected async virtual void OnMyLogin(object sender, EventArgs e)
        {
            string loginResult = await requestLoginAsync();
            if (loginResult.Equals("login failure")) {
                loggedInLabel.Text = "login failure";
            } else {
                this.
                loggedInLabel.Text = "Logged in";
            }
        }

        //< requestLoginAsync
        // @todo bad password/account fail case
        // @todo no network connection fail case
        // I'm deserializing and instantly reserializing an object. consider fixing.
        static async Task<string> requestLoginAsync()
        {
            string resultMsg = "Success...";
            // test thin air first...
            //return "Test complete";
            // ok, my event handlers are working... now to send my request to the web...
            LoginRequestJSON loginInfo = new LoginRequestJSON();
            loginInfo.username = "hcollins@gmail.com";
            loginInfo.password = "pa55w0rd";

            try
            {
                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "login");
                string jsonQuery = JsonConvert.SerializeObject(loginInfo);
                request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                // string test = request.ToString();

                HttpResponseMessage result = await client.SendAsync(request);
                if (result.StatusCode == System.Net.HttpStatusCode.OK) {
                    // do I need these?
                    GlobalStatusSingleton.loginCredentials
                        = JsonConvert.DeserializeObject<LoginResponseJSON>(await result.Content.ReadAsStringAsync());

                    HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, "auth");
                    //jsonQuery = JsonConvert.SerializeObject(loginCredentials);
                    tokenRequest.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                    HttpResponseMessage tokenResult = await client.SendAsync(tokenRequest);
                    GlobalStatusSingleton.authToken
                        = JsonConvert.DeserializeObject<AuthenticationToken>
                          (await tokenResult.Content.ReadAsStringAsync());
                    GlobalStatusSingleton.loggedIn = true;
                } else {
                    // login creds are no good.
                    resultMsg = "Sorry, invalid username/password";
                }

            } catch (System.Net.WebException err) {
                resultMsg = "login failure";
            } catch (HttpRequestException err) {
                // do something!!
                resultMsg = "login failure";
            }
            return resultMsg;
        }
        //> requestLoginAsync
    }
}
