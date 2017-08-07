using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware;

namespace ImageImprov.Droid {
    public class CameraPreview : SurfaceView, ISurfaceHolderCallback {
        //private SurfaceView.Holder mHolder;  this is saying that surfaceview has a holder.
        private Camera camera;

        public CameraPreview(Context context, Camera camera) : base(context) {
            this.camera = camera;

            this.Holder.AddCallback(this);
            //.Callback(this);
            //mHolder.setType();
        }

        public void SurfaceCreated(ISurfaceHolder holder) {
            try {
                camera.SetDisplayOrientation(90);
                camera.SetPreviewDisplay(holder);
                camera.StartPreview();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("DHB:CameraPreview:surfaceCreated exception:" + e.ToString());
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder) {
            // activity will release camera.
        }

        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int w, int h) {
            if (holder.Surface == null) {
                return;  // preview surface does not exist.
            }
            try {
                camera.StopPreview();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("DHB:CameraPreview:SurfaceChanged exception:" + e.ToString());
            }
            // @todo camera reformat
            try {
                camera.SetPreviewDisplay(this.Holder); // this is what the example has. it is NOT the passed in holder. is this an error?
                camera.StartPreview();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine("DHB:CameraPreview:SurfaceChanged exception:" + e.ToString());
            }
        }
    }
}

