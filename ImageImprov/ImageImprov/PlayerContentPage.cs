using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace ImageImprov
{
    class PlayerContentPage : ContentPage
    {
        public PlayerContentPage()
        {
            Content = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { XAlign = TextAlignment.Center, Text = "Go left for voting" },
                    new Label { XAlign = TextAlignment.Center, Text = "Go right to enter" },
                }
            };
        }
    }
}
