using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov
{
    // this is the main page when we are using the carousel to manage the ui.
    public class MainPageSwipeUI : CarouselPage, IExposeCamera
    {
        JudgingContentPage judgingPage;
        PlayerContentPage playerPage;
        CameraContentPage cameraPage;


        public MainPageSwipeUI()
        {
            //var padding = new Thickness(0, Device.OnPlatform(40, 40, 0), 0, 0);
            judgingPage = new JudgingContentPage();
            playerPage = new PlayerContentPage();
            cameraPage = new CameraContentPage();

            Children.Add(judgingPage);
            Children.Add(playerPage);
            Children.Add(cameraPage);
            this.CurrentPage = playerPage;
        }

        public ICamera getCamera()
        {
            return cameraPage;
        }
    }
}
