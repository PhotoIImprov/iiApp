using System;
using System.Text.RegularExpressions;
using System.Diagnostics;  // for debug assertions.
using Xamarin.Forms;

using SkiaSharp;
using System.IO;
using System.Reflection;
using ExifLib;

namespace ImageImprov {
    // a collection of static helper functions that are used throughout the app.
    public static class GlobalSingletonHelpers {
        public static string imageToByteArray(Image img) {
            var file = img.Source;
            return "hai";
            /*
            using (var memStream = new MemoryStream()) {
                file.GetStream().CopyTo(memStream);
                file.Dispose();
                return memStream.ToArray();
            }
            */
                
        }

        public static string getAuthToken() {
            return ("JWT " + GlobalStatusSingleton.authToken.accessToken);
        }

        public static string stripHyphens(string input) {
            string result = "";
            try {
                result = Regex.Replace(input, "-", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
            } catch (RegexMatchTimeoutException) {
                //result = input;  // not a huge fan of this approach...
                result = String.Empty; // or this one.
            }
            return result;
        }

        /// <summary>
        /// Given an input Aspect converts to a bool with
        /// Aspect.AspectFit == true, Aspect.Fill == false and Aspect.AspectFill == false.
        /// This is done to map to the user setting checkbox.
        /// </summary>
        /// <param name="inAspect"></param>
        /// <returns></returns>
        public static bool AspectSettingToBool(Aspect inAspect) {
            return ((inAspect == Aspect.AspectFit) ? true : false);
        }
        
        /// <summary>
        /// Reverse look up of the AspectSettingToBool conversion.
        /// </summary>
        /// <param name="checkedStatus"></param>
        /// <returns></returns>
        public static Aspect BoolToAspectSetting(bool checkedStatus) {
            return (checkedStatus ? Aspect.AspectFit : Aspect.Fill);
        }

        //
        //
        //   BEGIN IMAGE PROCESSING HELPERS
        //   BEGIN IMAGE PROCESSING HELPERS
        //   BEGIN IMAGE PROCESSING HELPERS
        //
        //

        /// <summary>
        /// Convenience method for converting a given SKImage to a Xamarin Image UI Component.
        /// </summary>
        /// <param name="inImg">A valid SKImage</param>
        /// <returns>A Xamarin.Forms.Image object based on the passed in SKImage</returns>
        public static Image SKImageToXamarinImage(SKImage inImg) {
            // yes, there should be a way to do this without the multiple back and forth to bytes.
            // no, it's not worth my time to try and figure it out.
            var data = inImg.Encode(SKEncodedImageFormat.Jpeg, 100);
            MemoryStream ms = new MemoryStream();
            data.SaveTo(ms);
            var bytes = ms.ToArray();
            Image outImage = new Image();
            outImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            return outImage;
        }

        public static void UpdateXamarinImageFromSKImage(Image existing, SKImage newImg) {
            // yes, there should be a way to do this without the multiple back and forth to bytes.
            // no, it's not worth my time to try and figure it out.
            var data = newImg.Encode(SKEncodedImageFormat.Jpeg, 100);
            MemoryStream ms = new MemoryStream();
            data.SaveTo(ms);
            var bytes = ms.ToArray();
            existing.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            //return outImage;
        }

        public static SKBitmap loadSKBitmapFromResourceName(string resourceName, Assembly assembly) {
            SKBitmap result = null;
            using (var resource = assembly.GetManifestResourceStream(resourceName))
            using (var stream = new SKManagedStream(resource)) {
                result = SKBitmap.Decode(stream);
            }
            return result;
        }

        public static Image buildBackground(string patternSource, Assembly assembly, int Width, int Height,
            double bottomAdjustment = GlobalStatusSingleton.PATTERN_PCT, double sideAdjustment = GlobalStatusSingleton.PATTERN_FULL_COVERAGE) {
            Image result = null;
            if ((Width == -1) || (Height == -1)) {
                return result;
            }

            using (var resource = assembly.GetManifestResourceStream(patternSource))
            using (var stream = new SKManagedStream(resource)) {
                var bitmap = SKBitmap.Decode(stream);
                int tilesWide = (int)((Width * sideAdjustment) / bitmap.Width);
                int tilesHigh = (int)((Height * bottomAdjustment) / bitmap.Height);

                try {
                    using (var tempSurface = SKSurface.Create(new SKImageInfo((int)Width, (int)Height))) {
                        var canvas = tempSurface.Canvas;
                        canvas.Clear(SKColors.White);
                        
                        SKBitmap bottomEdge = new SKBitmap();
                        SKBitmap rightEdge = new SKBitmap();
                        SKBitmap corner = new SKBitmap();
                        int excessH = (int)(Height * bottomAdjustment) - (tilesHigh * bitmap.Height);
                        int excessW = (int)(Width * sideAdjustment) - (tilesWide * bitmap.Width);
                        if (excessH > 0) {
                            bitmap.ExtractSubset(bottomEdge, new SKRectI(0, 0, bitmap.Width, excessH));
                        }
                        if (excessW > 0) {
                            bitmap.ExtractSubset(rightEdge, new SKRectI(0, 0, excessW, bitmap.Height));
                        }
                        if ((excessH > 0) && (excessW > 0)) {
                            bitmap.ExtractSubset(corner, new SKRectI(0, 0, excessW, excessH));
                        }

                        for (int i = 0; i < tilesWide; i++) {
                            for (int j = 0; j < tilesHigh; j++) {
                                canvas.DrawBitmap(bitmap, SKRect.Create(i * bitmap.Width, j * bitmap.Height, bitmap.Width, bitmap.Height));
                            }
                            // this covers the bottom except lower right corner.
                            if (Height > tilesHigh * bitmap.Height) {
                                canvas.DrawBitmap(bottomEdge, SKRect.Create(i * bitmap.Width, tilesHigh * bitmap.Height, bitmap.Width, excessH));
                            }
                        }

                        // this is the far side, but not lower right corner.
                        if (Width > tilesWide * bitmap.Width) {
                            for (int k = 0; k < tilesHigh; k++) {
                                canvas.DrawBitmap(rightEdge, SKRect.Create(tilesWide * bitmap.Width, k * bitmap.Height, excessW, bitmap.Height));
                            }
                        }

                        // and finally the bottom right corner.
                        if ((Height > tilesHigh * bitmap.Height) && (Width > tilesWide * bitmap.Width)) {
                            canvas.DrawBitmap(corner, SKRect.Create(tilesWide * bitmap.Width, tilesHigh * bitmap.Height, excessW, excessH));
                        }
                        SKImage skImage = tempSurface.Snapshot();
                        result = SKImageToXamarinImage(skImage);
                    }
                } catch (Exception e) {
                    string msg = e.ToString();
                }
            }
            return result;
        }

        public static Image buildBackgroundFromBytes(byte[] patternSource, Assembly assembly, int Width, int Height, 
            double bottomAdjustment = GlobalStatusSingleton.PATTERN_PCT, double sideAdjustment = GlobalStatusSingleton.PATTERN_FULL_COVERAGE) 
        {
            Image result = null;
            if ((Width == -1) || (Height == -1)) {
                return result;
            }

            SKBitmap bitmap = new SKBitmap(new SKImageInfo(Width, Height));
            var bitmapFullSize = buildFixedRotationSKBitmapFromBytes(patternSource);
            if ((bitmapFullSize.Width>Width) || (bitmapFullSize.Height>Height)) {
                // passed in image is larger than the screen.  shrink to fit.
                bitmapFullSize.Resize(bitmap, SKBitmapResizeMethod.Box);
            } else {
                bitmap = bitmapFullSize;
            }
            int tilesWide = (int)((Width*sideAdjustment) / bitmap.Width);
            int tilesHigh = (int)((Height* bottomAdjustment) / bitmap.Height);
            try {
                using (var tempSurface = SKSurface.Create(new SKImageInfo((int)Width, (int)Height))) {
                    var canvas = tempSurface.Canvas;
                    canvas.Clear(SKColors.White);

                    SKBitmap bottomEdge = new SKBitmap();
                    SKBitmap rightEdge = new SKBitmap();
                    SKBitmap corner = new SKBitmap();
                    int excessH = (int)(Height*bottomAdjustment) - (tilesHigh * bitmap.Height);
                    int excessW = (int)(Width*sideAdjustment) - (tilesWide * bitmap.Width);
                    if (excessH > 0) {
                        bitmap.ExtractSubset(bottomEdge, new SKRectI(0, 0, bitmap.Width, excessH));
                    }
                    if (excessW > 0) {
                        bitmap.ExtractSubset(rightEdge, new SKRectI(0, 0, excessW, bitmap.Height));
                    }
                    if ((excessH > 0) && (excessW > 0)) {
                        bitmap.ExtractSubset(corner, new SKRectI(0, 0, excessW, excessH));
                    }

                    for (int i = 0; i < tilesWide; i++) {
                        for (int j = 0; j < tilesHigh; j++) {
                            canvas.DrawBitmap(bitmap, SKRect.Create(i * bitmap.Width, j * bitmap.Height, bitmap.Width, bitmap.Height));
                        }
                        // this covers the bottom except lower right corner.
                        if (Height > tilesHigh * bitmap.Height) {
                            canvas.DrawBitmap(bottomEdge, SKRect.Create(i * bitmap.Width, tilesHigh * bitmap.Height, bitmap.Width, excessH));
                        }
                    }
                    // this is the far side, but not lower right corner.
                    if (Width > tilesWide * bitmap.Width) {
                        for (int k = 0; k < tilesHigh; k++) {
                            canvas.DrawBitmap(rightEdge, SKRect.Create(tilesWide * bitmap.Width, k * bitmap.Height, excessW, bitmap.Height));
                        }
                    }
                    // and finally the bottom right corner.
                    if ((Height > tilesHigh * bitmap.Height) && (Width > tilesWide * bitmap.Width)) {
                        canvas.DrawBitmap(corner, SKRect.Create(tilesWide * bitmap.Width, tilesHigh * bitmap.Height, excessW, excessH));
                    }
                    SKImage skImage = tempSurface.Snapshot();
                    result = SKImageToXamarinImage(skImage);
                }
            } catch (Exception e) {
                string msg = e.ToString();
            }
            return result;
        }

        public static SKImage MergeImages(SKBitmap backgroundImg, SKBitmap foregroundImg) {
            SKImage finalImage = null;
            const int EXTRA_INSET = 10;
            try {
                using (var tempSurface = SKSurface.Create(new SKImageInfo(backgroundImg.Width, backgroundImg.Height))) {
                    var canvas = tempSurface.Canvas;
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawBitmap(backgroundImg, SKRect.Create(0, 0, backgroundImg.Width, backgroundImg.Height));
                    if ((backgroundImg.Width < (foregroundImg.Width + EXTRA_INSET)) || (backgroundImg.Height < (foregroundImg.Height + EXTRA_INSET))) {
                        // foreground is larger in at least 1 dimension.  Just don't merge in this case.
                        finalImage = tempSurface.Snapshot();
                    } else {
                        int wOffset = backgroundImg.Width - foregroundImg.Width - EXTRA_INSET;
                        int hOffset = backgroundImg.Height - foregroundImg.Height - EXTRA_INSET;
                        canvas.DrawBitmap(foregroundImg, SKRect.Create(wOffset, hOffset, foregroundImg.Width, foregroundImg.Height));
                        finalImage = tempSurface.Snapshot();
                    }
                }
            } catch (Exception e) {
                string msg = e.ToString();
            }
            return finalImage;
        }

        public static SKBitmap SKBitmapFromBytes(byte[] imgBits) {
            SKBitmap bitmap = null;
            MemoryStream mems = new MemoryStream(imgBits);
            bitmap = SKBitmap.Decode(mems);

            // a quickie test. it worked. yay 
            /*
            if (bitmap != null) {
                int w = bitmap.Width;
                int h = bitmap.Height;
            }
            */
            return bitmap;
        }

        public static SKBitmap buildFixedRotationSKBitmapFromBytes(byte[] imgBits) {
            SKBitmap rotatedBmp = null;
            using (var resource = new MemoryStream(imgBits)) {
                //using (var stream = new SKManagedStream(resource)) {
                //using (var stream = new MemoryStream((resource)) {
                ExifOrientation imgExifO = ExifLib.ExifOrientation.TopLeft;
                try {
                    JpegInfo jpegInfo = ExifReader.ReadJpeg(resource);
                    // What exif lib associates each orientation with num in spec:
                    // ExifLib.ExifOrientation.TopRight == 6;   // i need to rotate clockwise 90
                    // ExifLib.ExifOrientation.BottomLeft == 8;  // i need to rotate ccw 90
                    // ExifLib.ExifOrientation.BottomRight == 3; // i need to rotate 180
                    // ExifLib.ExifOrientation.TopLeft ==1;  // do nada.

                    // What each image I set the exif on resulted in:
                    // (note: what I set should be correct as it displays right in programs that adjust for exif)
                    // Unchd: 1
                    // ImgRotLeft: 6
                    // ImgRotRight: 8
                    // ImgRot180: 3
                    // Cool. These all tie out with images in Dave Perret article.
                    imgExifO = jpegInfo.Orientation;
                    //int imgExifWidth = jpegInfo.Width;
                    //int imgExifHeight = jpegInfo.Height;

                    //string res = "Orient:" + imgExifO.ToString() + "  W:" + imgExifWidth + ", H:" + imgExifHeight;
                    //string res2 = res + "dummy";
                } catch (Exception e) {
                    Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationSKBitmapFromStr bad exif read");
                    Debug.WriteLine(e.ToString());
                }

                try {
                    SKBitmap baseBmp = SKBitmapFromBytes(imgBits);
                    //SKBitmap rotatedBmp = null;
                    if (imgExifO == ExifLib.ExifOrientation.TopRight) {
                        rotatedBmp = new SKBitmap(baseBmp.Height, baseBmp.Width);
                        using (var canvas = new SKCanvas(rotatedBmp)) {
                            canvas.Translate(rotatedBmp.Width, 0);
                            canvas.RotateDegrees(90);
                            canvas.DrawBitmap(baseBmp, 0, 0);
                        }
                    } else if (imgExifO == ExifLib.ExifOrientation.BottomLeft) {
                        rotatedBmp = new SKBitmap(baseBmp.Height, baseBmp.Width);
                        using (var canvas = new SKCanvas(rotatedBmp)) {
                            // currently upside down. with w, 90.
                            // failures:   -W, 270    w,-90    -W,90     0,90  0,270
                            //   h, 270  -> soln is to think about the corner I'm told is important...
                            //canvas.Translate(-rotatedBmp.Width, 0);
                            canvas.Translate(0, rotatedBmp.Height);
                            canvas.RotateDegrees(270);
                            canvas.DrawBitmap(baseBmp, 0, 0);
                        }
                    } else if (imgExifO == ExifLib.ExifOrientation.BottomRight) {
                        rotatedBmp = new SKBitmap(baseBmp.Width, baseBmp.Height);
                        using (var canvas = new SKCanvas(rotatedBmp)) {
                            canvas.Translate(rotatedBmp.Width, rotatedBmp.Height);
                            canvas.RotateDegrees(180);
                            canvas.DrawBitmap(baseBmp, 0, 0);
                        }
                    } else {
                        rotatedBmp = baseBmp;
                    }
                } catch (Exception e) {
                    string msg = e.ToString();
                }
            }
            return rotatedBmp;
        }

        public static Image buildFixedRotationImageFromBytes(byte[] inImg) {
            Image result = new Image();
            SKBitmap rotatedBmp = buildFixedRotationSKBitmapFromBytes(inImg);
            if (rotatedBmp != null) {
                result = SKImageToXamarinImage(SKImage.FromBitmap((SKBitmap)rotatedBmp));
            }
            return result;
        }

        /// <summary>
        /// Additionally sets the rotation(orientation) in the candidate object.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public static Image buildFixedRotationImage(BallotCandidateJSON candidate) {
            Image result = new Image();
            SKBitmap rotatedBmp = buildFixedRotationSKBitmapFromBytes(candidate.imgStr);
            if (rotatedBmp != null) { 
                // > means square images are treated as landscape.
                if (rotatedBmp.Height > rotatedBmp.Width) {
                    candidate.isPortrait = BallotCandidateJSON.PORTRAIT;
                } else {
                    candidate.isPortrait = BallotCandidateJSON.LANDSCAPE;
                }
                result = SKImageToXamarinImage(SKImage.FromBitmap((SKBitmap)rotatedBmp));
            }
            return result;
        }
        //
        //
        //   END IMAGE PROCESSING HELPERS
        //   END IMAGE PROCESSING HELPERS
        //   END IMAGE PROCESSING HELPERS
        //
        //
    }
}
