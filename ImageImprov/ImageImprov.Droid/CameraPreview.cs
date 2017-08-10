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
        private float dist;

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

        public override bool OnTouchEvent(MotionEvent evt) {
            // Get the pointer ID
            Camera.Parameters cParams = camera.GetParameters();
            MotionEventActions action = evt.Action;


            if (evt.PointerCount > 1) {
                // handle multi-touch events
                    
                if (action == MotionEventActions.PointerDown) {
                    dist = getFingerSpacing(evt);
                } else if ((action == MotionEventActions.Move) && cParams.IsZoomSupported) {
                    camera.CancelAutoFocus();
                    handleZoom(evt, cParams);
                }
            } else {
                // handle single touch events
                if (action == MotionEventActions.Up) {
                    handleFocus(evt, cParams);
                }
            }
            return true;
        }

        private void handleZoom(MotionEvent evt, Camera.Parameters cParams) {
            int maxZoom = cParams.MaxZoom;
            int zoom = cParams.Zoom;
            float newDist = getFingerSpacing(evt);
            if (newDist > dist) {
                //zoom in
                if (zoom < maxZoom)   zoom++;
            } else if (newDist < dist) {
                //zoom out
                if (zoom > 0)   zoom--;
            }
            dist = newDist;
            cParams.Zoom = zoom;
            camera.SetParameters(cParams);
        }

        public class DroidAutoFocusCallback : Java.Lang.Object, Camera.IAutoFocusCallback {
            public void OnAutoFocus(bool b, Camera camera) {
                // currently set to auto-focus on single touch
            }
        }

        public void handleFocus(MotionEvent evt, Camera.Parameters cParams) {
            int pointerId = evt.GetPointerId(0);
            int pointerIndex = evt.FindPointerIndex(pointerId);
            // Get the pointer's current position
            float x = evt.GetX(pointerIndex);
            float y = evt.GetY(pointerIndex);

            IList<String> supportedFocusModes = cParams.SupportedFocusModes;
            if (supportedFocusModes != null && supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto)) {
                DroidAutoFocusCallback cb = new DroidAutoFocusCallback();
                camera.AutoFocus(cb);
            }
        }

        /* Determine the space between the first two fingers */
        private float getFingerSpacing(MotionEvent evt) {
            float x = evt.GetX(0) - evt.GetX(1);
            float y = evt.GetY(0) - evt.GetY(1);
            return (float)Math.Sqrt(x * x + y * y);
        }
    }
}

