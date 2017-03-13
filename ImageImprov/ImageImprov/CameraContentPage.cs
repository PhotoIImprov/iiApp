using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov
{
    public class CameraContentPage : ContentPage, ICamera
    {
        //< image
        readonly Image image = new Image();
        //> image

        public CameraContentPage()
        {
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children = {
                new Button {
                    Text = "Take a picture!",
                    Command = new Command(o => ShouldTakePicture()),
                },
                image,
            },
            };
        }

        //< ShouldTakePicture
        public event Action ShouldTakePicture = () => { };
        //> ShouldTakePicture

        //< ShowImage
        public void ShowImage(string filepath)
        {
            image.Source = ImageSource.FromFile(filepath);
        }
        //> ShowImage

    }
}
