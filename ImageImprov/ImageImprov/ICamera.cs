using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageImprov
{
    // implemented by the CameraContentPage
    public interface ICamera
    {
        //< ShouldTakePicture
        event Action ShouldTakePicture;
        //> ShouldTakePicture

        //< ShowImage
        void ShowImage(string filepath);
        //> ShowImage
    }
}
