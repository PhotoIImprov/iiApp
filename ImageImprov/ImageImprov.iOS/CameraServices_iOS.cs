using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Foundation;
using UIKit;

using System.Threading.Tasks;
using AVFoundation;
using System.Diagnostics;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using SkiaSharp;

// based on:
// https://github.com/pierceboggan/xamarin-blog-samples/tree/master/ios/avfoundation-camera/1.%20Start

namespace ImageImprov.iOS {
    public partial class CameraServices_iOS : UIViewController {  // UIViewController needed for this.View.Frame
        bool flashMode = false;
        UIButton flashButton;
        UIImage flashOffImg;
        UIImage flashOnImg;
        public static bool exitFromPhoto = false;

        //public CameraService(IntPtr handle): base(handle) { }

        public override async void ViewDidLoad() {
            Title = "Todays category is frog!";

            Debug.WriteLine("CameraService:ViewDidLoad");
            base.ViewDidLoad();
            
            await AuthorizeCameraUse();
            SetupLiveCameraStream();
            Debug.WriteLine("CameraService:ViewDidLoad end");
            // View = liveCameraStream;  works, but now we need to add more...

            // doing f-all adding to liveCameraStream. Try adding to view...
            UIPinchGestureRecognizer pinchRecognizer = new UIPinchGestureRecognizer(HandlePinchToZoomRecognizer);
            //liveCameraStream.AddGestureRecognizer(pinchRecognizer);
            View.AddGestureRecognizer(pinchRecognizer);
            //UITapGestureRecognizer tapRecognizer = new UITapGestureRecognizer(PreviewAreaTappedToChangeFocus);
            //liveCameraStream.AddGestureRecognizer(tapRecognizer);
            //View.AddGestureRecognizer(tapRecognizer);

            View.AddSubview(liveCameraStream);
            View.BackgroundColor = UIColor.White;

            // take picture button
            var takePicButton = UIButton.FromType(UIButtonType.RoundedRect);
            Debug.WriteLine("DHB:CameraService:ViewDidLoad device height:" + UIScreen.MainScreen.Bounds.Size.Height);
            UIImage buttonImg = UIImage.FromFile("camerabutton.png");
            UIImage highlightImg = UIImage.FromFile("cameraButton_active.png");
            takePicButton.SetImage(buttonImg, UIControlState.Normal);
            takePicButton.SetImage(highlightImg, UIControlState.Highlighted);
            takePicButton.Frame = new CGRect(123, 470, 74, 74);
            //Assembly assembly = this.GetType().GetTypeInfo().Assembly;
            //UIImage buttonImg = UIImage.FromResource(assembly, "Images/contests_inactive.png");
            //UIImage buttonImg = UIImage.FromResource(assembly, "Images/contests_inactive.png");
            //takePicButton.SetTitle("Tap to snap", UIControlState.Normal);
            takePicButton.TouchUpInside += TakePhotoButtonTapped;
            View.AddSubview(takePicButton);

            var label = new UILabel();
            //label.MinimumFontSize = 40;
            label.Text = GlobalStatusSingleton.uploadingCategories[0].description;
            label.TextAlignment = UITextAlignment.Center;
            label.TextColor = UIColor.White;
            //label.ToggleBoldface(null);  hmm. this blows up.
            //Debug.WriteLine("DHB:CameraServices_iOS:ViewDidLoad font name: " + label.Font.ToString());  //.SFUIText 17
            label.Font = UIFont.FromName(".SFUIText-Bold", 30);
            label.BackgroundColor = new UIColor(252.0f / 255.0f, 213.0f / 255.0f, 21.0f / 255.0f, 1.0f);
            label.Frame = new CGRect(0, 30, View.Bounds.Width, 50);
            View.AddSubview(label);

            UIButton backButton = new UIButton();
            UIImage backImg = UIImage.FromFile("backbutton.png");
            backButton.SetImage(backImg, UIControlState.Normal);
            backButton.Frame = new CGRect(5, 40, 19, 24);
            backButton.TouchUpInside += ExitWithoutPhoto;
            View.AddSubview(backButton);

            flashButton = new UIButton();
            flashOffImg = UIImage.FromFile("flash_inactive.png");
            flashOnImg = UIImage.FromFile("flash.png");
            flashButton.SetImage(flashOffImg, UIControlState.Normal);
            flashButton.SetImage(flashOnImg, UIControlState.Selected);
            flashButton.Frame = new CGRect(30, 470, 74, 74);
            flashButton.TouchUpInside += toggleFlash;
            View.AddSubview(flashButton);
        }

        public override void DidReceiveMemoryWarning() {
            base.DidReceiveMemoryWarning();
        }

        //async partial void TakePhotoButtonTapped() { }

        public async Task AuthorizeCameraUse() {
            Debug.WriteLine("CameraService:AuthorizeCameraUse");
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);
            if (authorizationStatus != AVAuthorizationStatus.Authorized) {
                await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
            Debug.WriteLine("CameraService:AuthorizeCameraUse");
            return;
        }

        AVCaptureSession captureSession;
        AVCaptureDevice captureDevice;
        AVCaptureDeviceInput captureDeviceInput;
        AVCaptureStillImageOutput stillImageOutput;
        AVCaptureVideoPreviewLayer videoPreviewLayer;
        UIKit.UIView liveCameraStream { get; set; }
        public EventHandler FinishedPickingMedia;

        public void SetupLiveCameraStream() {
            Debug.WriteLine("SetupLiveCameraStream start");
            captureSession = new AVCaptureSession();
            if (liveCameraStream == null) {
                Debug.WriteLine("SetupLiveCameraStream liveCameraStream was null");
                liveCameraStream = new UIView();
            }
            var viewLayer = liveCameraStream.Layer;
            nfloat w = this.View.Frame.Width;
            nfloat h = this.View.Frame.Height;
            Debug.WriteLine(" pre w:" + w + ", h:" + h);
            if (w < h) {
                h = w;
            } else if (h < w) {
                w = h;
            }
            Debug.WriteLine("post w:" + w + ", h:" + h);
            CoreGraphics.CGRect myRect = new CoreGraphics.CGRect(0f, 100f, w, h);
            //CoreGraphics.CGRect myRect = new CGRect(new CGSize(w, w));

            videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession) {
                //Frame = this.View.Frame
                // This does correctly reduce the longer side.
                // However, it then reduces the shorter side to maintain aspect ratio. oof.
                Frame = myRect,
                //VideoGravity = AVLayerVideoGravity.Resize,  // default is ResizeAspect which results in a new rectangle
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill,  // default is ResizeAspect
            };
            //videoPreviewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;

            liveCameraStream.Layer.AddSublayer(videoPreviewLayer);
            //UITapGestureRecognizer tapRecognizer = new UITapGestureRecognizer(PreviewAreaTappedToChangeFocus);
            //liveCameraStream.AddGestureRecognizer(tapRecognizer);

            //var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
            captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
            ConfigureCameraForDevice(captureDevice);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);
            captureSession.AddInput(captureDeviceInput);

            var dictionary = new NSMutableDictionary();
            dictionary[AVVideo.CodecKey] = new NSNumber((int)AVVideoCodec.JPEG);
            stillImageOutput = new AVCaptureStillImageOutput() {
                OutputSettings = new NSDictionary()
            };

            captureSession.AddOutput(stillImageOutput);
            Debug.WriteLine("SetupLiveCameraStream pre running");
            captureSession.StartRunning();
            Debug.WriteLine("SetupLiveCameraStream end");

            
        }

        void ConfigureCameraForDevice(AVCaptureDevice device) {
            var error = new NSError();
            if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus)) {
                device.LockForConfiguration(out error);
                device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                device.UnlockForConfiguration();
            }
            if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure)) {
                device.LockForConfiguration(out error);
                device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                device.UnlockForConfiguration();
            }
            if (device.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance)) {
                device.LockForConfiguration(out error);
                device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
                device.UnlockForConfiguration();
            }
            // @todo AVCaptureDeviceFormat look into what are valid values for this. 
        }

        async void TakePhotoButtonTapped(object sender, EventArgs e) {
            Debug.WriteLine("DHB:CameraService:TakePhotoButtonTapped");
            try {
                AVCaptureConnection videoConnection = stillImageOutput.ConnectionFromMediaType(AVMediaType.Video);
                CMSampleBuffer sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync(videoConnection);

                NSData jpegImageAsNsData = AVCaptureStillImageOutput.JpegStillToNSData(sampleBuffer);
                byte[] jpegAsByteArray = jpegImageAsNsData.ToArray();
                //GlobalSingletonHelpers.readExifOrientation(jpegAsByteArray);
                //Debug.WriteLine("DHB:CameraServices_iOS:TakePhotoButton:TakePhotoButtonTapped orientation:" + UIDevice.CurrentDevice.Orientation);
                GlobalStatusSingleton.mostRecentImgBytes = jpegAsByteArray;
                exitFromPhoto = true;
                if (FinishedPickingMedia != null) {
                    FinishedPickingMedia(this, e);
                }
            } catch (Exception err) {
                Debug.WriteLine("DHB:Exception " + err.ToString());
            }
        }

        public void ExitWithoutPhoto(object sender, EventArgs e) {
            Debug.WriteLine("DHB:CameraService:TakePhotoButtonTapped");
            try {
                //Debug.WriteLine("DHB:CameraServices_iOS:TakePhotoButton:TakePhotoButtonTapped orientation:" + UIDevice.CurrentDevice.Orientation);
                exitFromPhoto = false;
                if (FinishedPickingMedia != null) {
                    System.EventArgs eMax = (System.EventArgs)e;
                    FinishedPickingMedia(this, e);
                }
            } catch (Exception err) {
                Debug.WriteLine("DHB:Exception " + err.ToString());
            }
        }

        public void toggleFlash(object sender, EventArgs e) {
            flashMode = !flashMode;
            flashButton.Selected = !flashButton.Selected;
            var error = new NSError();
            //Debug.WriteLine("DHB:CameraServices_iOS:toggleFlash button state:" + flashButton.State.ToString());
            if (captureDevice.HasFlash) {
                if (flashMode == true) {
                    captureDevice.LockForConfiguration(out error);
                    captureDevice.FlashMode = AVCaptureFlashMode.On;
                    captureDevice.UnlockForConfiguration();
                } else {
                    captureDevice.LockForConfiguration(out error);
                    captureDevice.FlashMode = AVCaptureFlashMode.Off;
                    captureDevice.UnlockForConfiguration();
                }
            }
        }

        // this doesn't seem to work.
        async void PreviewAreaTappedToChangeFocus(UIGestureRecognizer tap) {
            try {
                Debug.WriteLine("DHB:CameraServices_iOS:PreviewAreaTappedToChangeFocus tap");
                captureDevice.FocusPointOfInterest = tap.LocationOfTouch(0, tap.View);
            } catch (Exception err) {
                Debug.WriteLine("DHB:Exception " + err.ToString());
            }
        }

        void HandlePinchToZoomRecognizer(UIPinchGestureRecognizer pinchRecognizer) {
            Debug.WriteLine("DHB:CameraServices_iOS:HandlePinchToZoomRecognizer");
            const float pinchVelocityDividerFactor = 5.0f;
            if (pinchRecognizer.State == UIGestureRecognizerState.Changed) {
                NSError error = null;
                if (captureDevice.LockForConfiguration(out error)) {
                    float desiredZoomFactor = (float)captureDevice.VideoZoomFactor + (float)Math.Atan2(pinchRecognizer.Velocity, pinchVelocityDividerFactor);
                    captureDevice.VideoZoomFactor = (System.nfloat)Math.Max(1.0, Math.Min(desiredZoomFactor, captureDevice.ActiveFormat.VideoMaxZoomFactor));
                    captureDevice.UnlockForConfiguration();
                } else {
                    Debug.WriteLine("DHB:CameraServices_iOS:HandlePinchToZoomRecognizer error on lock.");
                }
            }
        }

        public static int calculateRotationDegrees() {
            int rotation = 0;
            if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft) {
                rotation = 0;
            } else if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.Portrait) {
                rotation = 90;
            } else if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight) {
                rotation = 180;
            } else if (UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.PortraitUpsideDown) {
                rotation = 270;
            } else {
                // can get here from faceup.
                rotation = 0;
                // throwing an exception will prevent picture snappage.
                //throw new IndexOutOfRangeException("Invalid orientation: " + UIDevice.CurrentDevice.Orientation.ToString());
                Debug.WriteLine(("DHB:CameraService_iOS:calculateRotationDegrees: Invalid orientation: " + UIDevice.CurrentDevice.Orientation.ToString()));
            }
            return rotation;
        }
    }
}