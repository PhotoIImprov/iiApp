using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov
{
    // this is the main page when we are using the carousel to manage the ui.
    public class MainPageSwipeUI : CarouselPage, IExposeCamera, IProvideNavigation
    {
        JudgingContentPage judgingPage;
        PlayerContentPage playerPage;
        CameraContentPage cameraPage;
        LeaderboardPage leaderboardPage;


        public MainPageSwipeUI()
        {
            //var padding = new Thickness(0, Device.OnPlatform(40, 40, 0), 0, 0);
            playerPage = new PlayerContentPage();  // player page must be built before judging page sets up listeners.
            judgingPage = new JudgingContentPage();
            cameraPage = new CameraContentPage();


            playerPage.TokenReceived += new TokenReceivedEventHandler(judgingPage.TokenReceived);

            // both judgingPage and cameraPage are guaranteed to exist at this point.
            // is categoryLoad?
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(cameraPage.OnCategoryLoad);
            //judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(leaderboardPage.OnCategoryLoad);
            judgingPage.CategoryLoadSuccess += new CategoryLoadSuccessEventHandler(playerPage.CenterConsole.LeaderboardPage.OnCategoryLoad);

            Children.Add(judgingPage);
            Children.Add(playerPage);
            Children.Add(cameraPage);
            // leaderboard is not part of the carousel.  It's reached from the player page only.
            this.CurrentPage = playerPage;
        }

        public ICamera getCamera()
        {
            return cameraPage;
        }

        public void gotoJudgingPage() {
            this.CurrentPage = judgingPage;
        }
        // This takes the user to the PlayerContentPage.
        public void gotoHomePage() {
            playerPage.goHome();
            this.CurrentPage = playerPage;
        }
        public void gotoCameraPage() {
            this.CurrentPage = cameraPage;
        }
    }
}
