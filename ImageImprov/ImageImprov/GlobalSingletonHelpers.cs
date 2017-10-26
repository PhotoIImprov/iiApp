using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;  // for debug assertions.
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

using SkiaSharp;
using System.IO;
using System.Linq;
using System.Reflection;
using ExifLib;
using System.Net.Http;
using Newtonsoft.Json;

namespace ImageImprov {
    // a collection of static helper functions that are used throughout the app.
    public static class GlobalSingletonHelpers {
        public const string EMPTY = "EMPTY";

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

        public static bool isEmailAddress(string testAddress) {
            // try with a test against this regex: ^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,6}$
            string pattern = "^[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,6}$";
            Match m = Regex.Match(testAddress.Trim(), pattern, RegexOptions.IgnoreCase);
            return m.Success;
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long GetMillisecondsSinceUnixEpoch(DateTime forThisTime) {
            return (long)((forThisTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds);
        }

        public static PropertyInfo GetProperty(object source, string propertyName) {
            var property = source.GetType().GetRuntimeProperties().FirstOrDefault(p => string.Equals(p.Name, propertyName));
            return property;
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

        public static async Task<bool> SendLogData(string logInfo) {
            Debug.WriteLine("DHB:GlobalSingletonHelpers:SendLogData start");
            string result = "fail";

            try {
                // only send log data if we are authenticated.
                if (GlobalStatusSingleton.authToken != null) {
                    HttpClient client = new HttpClient();

                    client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "log");
                    LoggingJSON log = new LoggingJSON();
                    log.msg = logInfo;
                    string jsonQuery = JsonConvert.SerializeObject(log);
                    request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                    request.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                    HttpResponseMessage sendResult = await client.SendAsync(request);
                    if (sendResult.StatusCode == System.Net.HttpStatusCode.OK) {
                        // do I need these?
                        result = await sendResult.Content.ReadAsStringAsync();
                    }
                }
            } catch (Exception e) {
                // we're doing nothing in the fail case.
                Debug.WriteLine(e.ToString());
            }
            return true;
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

        /// <summary>
        /// Alters the passed in existing Image to have the data of newImg.
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="newImg"></param>
        public static void UpdateXamarinImageFromSKImage(Image existing, SKImage newImg) {
            // yes, there should be a way to do this without the multiple back and forth to bytes.
            // no, it's not worth my time to try and figure it out.
            var data = newImg.Encode(SKEncodedImageFormat.Jpeg, 100);
            MemoryStream ms = new MemoryStream();
            data.SaveTo(ms);
            var bytes = ms.ToArray();
            // test setting min sizing to eliminate flicker...
            //  nothing noticeable.
            //existing.MinimumWidthRequest = existing.Width;
            //existing.MinimumHeightRequest = existing.Height;
            // neither is doing in the main thread - I was already doing in the main thread. Thus the lack of difference.
            //Device.BeginInvokeOnMainThread(() => {
            existing.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            //});
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

        public static SKBitmap loadSKBitmapFromFilename(string filename) {
            SKBitmap unrotated = SKBitmap.Decode(filename);
            // This won't work as we discard the exif info on load.  Also, given the filename it already knew it was grabbing a jpg.
            //SKBitmap rotated = buildFixedRotationSKBitmapFromBytes(unrotated.Bytes);
            return unrotated;
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
            double bottomAdjustment = GlobalStatusSingleton.PATTERN_PCT, double sideAdjustment = GlobalStatusSingleton.PATTERN_FULL_COVERAGE) {
            Image result = null;
            if ((Width == -1) || (Height == -1)) {
                return result;
            }
            if (patternSource == null) {
                return result;
            }

            SKBitmap bitmap = new SKBitmap(new SKImageInfo(Width, Height));
            var bitmapFullSize = buildFixedRotationSKBitmapFromBytes(patternSource);
            if ((bitmapFullSize.Width > Width) || (bitmapFullSize.Height > Height)) {
                // passed in image is larger than the screen.  shrink to fit.
                bitmapFullSize.Resize(bitmap, SKBitmapResizeMethod.Box);
            } else {
                bitmap = bitmapFullSize;
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imgBits">These are not actually the raw bits, but a jpeg encoded set of bits.</param>
        /// <returns></returns>
        public static SKBitmap SKBitmapFromBytes(byte[] imgBits) {
            SKBitmap bitmap = null;
            MemoryStream mems = new MemoryStream(imgBits);
            // meh. not sure how to do this if i don't already have it decoded and sized...
            //SKImageInfo skii = new SKImageInfo(-1, -1, SKColorType.Rgb565);
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

        public static SKBitmap buildFixedRotationSKBitmapFromBytes(byte[] imgBits, ExifOrientation imgExifO = ExifOrientation.Undefined) {
            SKBitmap rotatedBmp = null;
            if (imgBits != null) {
                DateTime step0 = DateTime.Now;
                using (var resource = new MemoryStream(imgBits)) {
                    //using (var stream = new SKManagedStream(resource)) {
                    //using (var stream = new MemoryStream((resource)) {
                    DateTime step1 = DateTime.Now;
                    if (imgExifO == ExifOrientation.Undefined) {
                        imgExifO = ExifLib.ExifOrientation.TopLeft;
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
                            Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationSKBitmapFromBytes bad exif read");
                            Debug.WriteLine(e.ToString());
                        }
                    }
                    DateTime step2 = DateTime.Now;
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
                        Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationSKBitmapFromBytes success");
                    } catch (Exception e) {
                        string msg = e.ToString();
                    }
                    DateTime step3 = DateTime.Now;
                    //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationSKBitmapFromBytes step1:" + (step1 - step0));
                    //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationSKBitmapFromBytes step2:" + (step2 - step1));
                    //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationSKBitmapFromBytes step3:" + (step3 - step2));
                }
            }
            return rotatedBmp;
        }

        public static Image buildFixedRotationImageFromBytes(byte[] inImg, ExifOrientation imgExifO = ExifOrientation.Undefined, int width = -1, int height = -1) {
            if (inImg == null) { return null; }
            Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes");
            DateTime step0 = DateTime.Now;
            Image result = new Image();
            DateTime step1 = DateTime.Now;
            SKBitmap rotatedBmp = buildFixedRotationSKBitmapFromBytes(inImg, imgExifO);
            Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes rotBmp done");
            if ((width > -1) && (height > 1) && (rotatedBmp != null)) {
                SKImageInfo sizing = new SKImageInfo(width, height);
                rotatedBmp = rotatedBmp.Resize(sizing, SKBitmapResizeMethod.Hamming);
            }
            Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes resize done");
            DateTime step2 = DateTime.Now;
            if (rotatedBmp != null) {
                result = SKImageToXamarinImage(SKImage.FromBitmap((SKBitmap)rotatedBmp));
            }
            Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes resize done");
            DateTime step3 = DateTime.Now;
            //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes step1:" + (step1 - step0));
            //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes step2:" + (step2 - step1));
            //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes step3:" + (step3 - step2));
            return result;
        }

        public static IList<Image> buildTwoFixedRotationImageFromBytes(byte[] inImg, ExifOrientation imgExifO = ExifOrientation.Undefined) {
            IList<Image> res = new List<Image>(2);
            DateTime step0 = DateTime.Now;
            //Image result = new Image();
            res.Add(new Image());
            res.Add(new Image());
            DateTime step1 = DateTime.Now;
            SKBitmap rotatedBmp = buildFixedRotationSKBitmapFromBytes(inImg, imgExifO);
            DateTime step2 = DateTime.Now;
            if (rotatedBmp != null) {
                //result = SKImageToXamarinImage(SKImage.FromBitmap((SKBitmap)rotatedBmp));
                SKImage img = SKImage.FromBitmap((SKBitmap)rotatedBmp);
                res[0] = SKImageToXamarinImage(img);
                res[1] = SKImageToXamarinImage(img);
            }
            DateTime step3 = DateTime.Now;
            //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes step1:" + (step1 - step0));
            //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes step2:" + (step2 - step1));
            //Debug.WriteLine("DHB:GlobalSingletonHelpers:buildFixedRotationImageFromBytes step3:" + (step3 - step2));
            //return result;
            return res;
        }

        /// <summary>
        /// Additionally sets the rotation(orientation) in the candidate object.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public static IList<Image> buildTwoFixedRotationImageFromCandidate(BallotCandidateJSON candidate) {
            IList<Image> res = new List<Image>(2);
            res.Add(new Image());
            res.Add(new Image());
            SKBitmap rotatedBmp = buildFixedRotationSKBitmapFromBytes(candidate.imgStr, (ExifOrientation)candidate.orientation);
            if (rotatedBmp != null) {
                //result = SKImageToXamarinImage(SKImage.FromBitmap((SKBitmap)rotatedBmp));
                SKImage img = SKImage.FromBitmap((SKBitmap)rotatedBmp);
                res[0] = SKImageToXamarinImage(img);
                res[1] = SKImageToXamarinImage(img);
            }
            if (rotatedBmp != null) {
                // > means square images are treated as landscape.
                if (rotatedBmp.Height > rotatedBmp.Width) {
                    candidate.isPortrait = BallotCandidateJSON.PORTRAIT;
                } else {
                    candidate.isPortrait = BallotCandidateJSON.LANDSCAPE;
                }
            }
            return res;
        }


        /// <summary>
        /// Additionally sets the rotation(orientation) in the candidate object.
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public static Image buildFixedRotationImage(BallotCandidateJSON candidate) {
            Image result = new Image();
            SKBitmap rotatedBmp = buildFixedRotationSKBitmapFromBytes(candidate.imgStr, (ExifOrientation)candidate.orientation);
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

        public static SKBitmap CropImage(SKBitmap inImg, int yOffset = 0) {
            int w = inImg.Width;
            int h = inImg.Height;
            int shortestLen = ((w < h) ? w : h);
            /*
            SKImage image = SKImage.FromBitmap(inImg);
            SKImage subset = image.Subset(SKRectI.Create(0, yOffset, shortestLen, shortestLen));
            return subset;*/
            SKRectI square = SKRectI.Create(0, yOffset, shortestLen, shortestLen);
            SKBitmap finalBmp = new SKBitmap(shortestLen, shortestLen);
            Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop pre extract");
            inImg.ExtractSubset(finalBmp, square);
            return finalBmp;
        }

        public static SKBitmap SquareFromTop(SKBitmap inImg) {
            int w = inImg.Width;
            int h = inImg.Height;
            int shortestLen = ((w < h) ? w : h);
            /*
            SKImage image = SKImage.FromBitmap(inImg);
            SKImage subset = image.Subset(SKRectI.Create(0, yOffset, shortestLen, shortestLen));
            return subset;*/
            SKRectI square = SKRectI.Create(0, h-shortestLen, shortestLen, shortestLen);
            SKBitmap finalBmp = new SKBitmap(shortestLen, shortestLen);
            Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop pre extract");
            inImg.ExtractSubset(finalBmp, square);
            return finalBmp;
        }

        public static SKBitmap CropImageAtMidPoint(SKBitmap inImg) {
            int w = inImg.Width;
            int h = inImg.Height;
            int shortestLen = 0;
            int longestLen = 0;
            int x = 0;
            int y = 0;
            if (w < h) {
                shortestLen = w;
                longestLen = h;
                x = 0;
                y = (h - w) / 2;
            } else if (h < w) {
                shortestLen = h;
                longestLen = w;
                y = 0;
                x = (w - h) / 2;
            } else {
                // already square. return.
                return inImg;
            }
            SKRectI square = SKRectI.Create(x, y, shortestLen, shortestLen);
            SKBitmap finalBmp = new SKBitmap(shortestLen, shortestLen);
            Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop pre extract");
            inImg.ExtractSubset(finalBmp, square);
            return finalBmp;
        }

        public static byte[] SquareImage(byte[] imgBytes) {
            // my passed in imgBytes are a jpg, not a bmp.
            //SKBitmap bmp = GlobalSingletonHelpers.SKBitmapFromBytes(imgBytes);
            SKBitmap bmp = buildFixedRotationSKBitmapFromBytes(imgBytes);
            SKImage res = SKImage.FromBitmap(CropImage(bmp));
            return res.Encode(SKEncodedImageFormat.Jpeg, 100).ToArray();

            // testing
            //byte[] test = res.Encode(SKEncodedImageFormat.Jpeg, 100).ToArray();
            //SKBitmap res3 = SKBitmapFromBytes(test2);
            //return test;

        }

        /// <summary>
        /// iOS sends me an image I need to rotate and crop
        /// </summary>
        /// <param name="baseBmp"></param>
        /// <returns></returns>
        public static SKBitmap rotateAndCrop(SKBitmap baseBmp, int rotateDegrees = 90, int offsetAmount = -1) {
            SKBitmap rotatedBmp = null;

            if (rotateDegrees == 90) {
                rotatedBmp = new SKBitmap(baseBmp.Height, baseBmp.Width);
                Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop if have a rotated bmp...");
                using (var canvas = new SKCanvas(rotatedBmp)) {
                    canvas.Translate(rotatedBmp.Width, 0);
                    canvas.RotateDegrees(rotateDegrees);
                    canvas.DrawBitmap(baseBmp, 0, 0);
                }
            } else if (rotateDegrees == 180) {
                rotatedBmp = new SKBitmap(baseBmp.Width, baseBmp.Height);
                using (var canvas = new SKCanvas(rotatedBmp)) {
                    canvas.Translate(rotatedBmp.Width, rotatedBmp.Height);
                    canvas.RotateDegrees(180);
                    canvas.DrawBitmap(baseBmp, 0, 0);
                }
            } else if (rotateDegrees == 270) {
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
            } else {
                // do anything?
                rotatedBmp = baseBmp;
            }

            Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop through rotation");
            // with new image rotation stuff, y may no be longest side. work on that next.
            /*
            int y = (rotatedBmp.Height - rotatedBmp.Width) / 2;
            SKRectI square = SKRectI.Create(0, y, rotatedBmp.Width, rotatedBmp.Width);
            SKBitmap finalBmp = new SKBitmap(rotatedBmp.Width, rotatedBmp.Width);
            Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop pre extract");
            rotatedBmp.ExtractSubset(finalBmp, square);
            Debug.WriteLine("DHB:GlobalSingletonHelpers:rotateAndCrop post extract");
            */
            SKBitmap finalBmp;
            //finalBmp = CropImageAtMidPoint(rotatedBmp);
            
            if (offsetAmount == -1) {
                finalBmp = CropImageAtMidPoint(rotatedBmp);
            } else {
                finalBmp = CropImage(rotatedBmp, offsetAmount);
                //finalBmp = SquareFromTop(rotatedBmp);
            }
            return finalBmp;
        }

        /// <summary>
        /// Returning an int so I dont need to link exif into the native apps.
        /// </summary>
        /// <param name="imgBits"></param>
        /// <returns></returns>
        public static int readExifOrientation(byte[] imgBits) {
            ExifOrientation result = ExifOrientation.Undefined;
            if (imgBits != null) {
                using (var resource = new MemoryStream(imgBits)) {
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
                        result = jpegInfo.Orientation;
                        //int imgExifWidth = jpegInfo.Width;
                        //int imgExifHeight = jpegInfo.Height;
                        Debug.WriteLine("DHB:GlobalSingletonHelpers:readExifOrientation orientation:" + result);
                    } catch (Exception e) {
                        Debug.WriteLine("DHB:GlobalSingletonHelpers:readExifOrientation  bad exif read");
                        Debug.WriteLine(e.ToString());
                    }
                }
            }
            return (int)result;
        }

        //
        //
        //   END IMAGE PROCESSING HELPERS
        //   END IMAGE PROCESSING HELPERS
        //   END IMAGE PROCESSING HELPERS
        //
        //

                    /*
                public static void fixLabelHeight(Label label, View view, double containerWidth) {
                    // Calculate the height of the rendered text. 
                    FontCalc lowerFontCalc = new FontCalc(label, 10, view.Width);
                    FontCalc upperFontCalc = new FontCalc(label, 100, view.Width);
                    while (upperFontCalc.FontSize - lowerFontCalc.FontSize > 1) {
                        // Get the average font size of the upper and lower bounds. 
                        double fontSize = (lowerFontCalc.FontSize + upperFontCalc.FontSize) / 2;
                        // Check the new text height against the container height. 
                        // @NOTE: And again, I'm cheating on the width used.  This won't work if I have multiple columns!!!!
                        FontCalc newFontCalc = new FontCalc(label, fontSize, view.Width);
                        // @NOTE: This is the worst cheat as it's the one most likely to be changed!
                        if ((newFontCalc.TextHeight > (view.Height)) || (newFontCalc.TextHeight == -1)) {
                            upperFontCalc = newFontCalc;
                        } else {
                            lowerFontCalc = newFontCalc;
                        }
                    }
                    // Set the final font size and the text with the embedded value. 
                    label.FontSize = lowerFontCalc.FontSize;
                    label.Text = label.Text.Replace("??", label.FontSize.ToString("F0"));
                }
                */
        public static void fixLabelHeight(Label label, double containerWidth, double containerHeight, 
            int minFontSize = GlobalStatusSingleton.MIN_FONT_SIZE, int maxFontSize=GlobalStatusSingleton.MAX_FONT_SIZE) 
        {
            // Calculate the height of the rendered text. 
            if ((containerWidth <= 0) || (containerHeight<=0)) { return; }

            FontCalc lowerFontCalc = new FontCalc(label, minFontSize, containerWidth);
            FontCalc upperFontCalc = new FontCalc(label, maxFontSize, containerWidth);
            while (upperFontCalc.FontSize - lowerFontCalc.FontSize > 1) {
                // Get the average font size of the upper and lower bounds. 
                double fontSize = (lowerFontCalc.FontSize + upperFontCalc.FontSize) / 2;
                // Check the new text height against the container height. 
                // @NOTE: And again, I'm cheating on the width used.  This won't work if I have multiple columns!!!!
                FontCalc newFontCalc = new FontCalc(label, fontSize, containerWidth);
                // @NOTE: This is the worst cheat as it's the one most likely to be changed!
                if ((newFontCalc.TextHeight > (containerHeight)) || (newFontCalc.TextHeight == -1)) {
                    upperFontCalc = newFontCalc;
                } else {
                    lowerFontCalc = newFontCalc;
                }

                // Testing....
                /*
                if (newFontCalc.TextHeight == -1) {
                    // wtf. why is this happening?
                    // this didn't help...
                    //containerWidth = containerWidth * 2.0;
                    label.MinimumHeightRequest = 20.0;
                    FontCalc test = new FontCalc(label, 10.0, containerWidth);
                    FontCalc test2 = new FontCalc(label, 5.0, containerWidth);
                    FontCalc test3 = new FontCalc(label, 1.0, containerWidth);
                }
                */
            }
            // Set the final font size and the text with the embedded value. 
            label.FontSize = lowerFontCalc.FontSize;
            label.Text = label.Text.Replace("??", label.FontSize.ToString("F0"));
        }

        /*
        public static string getUploadingCategoryDesc() {
            string categoryDesc = "";
            if (GlobalStatusSingleton.uploadingCategories.Count > 0) {
                categoryDesc = GlobalStatusSingleton.uploadingCategories[0].description;
            }
            return categoryDesc;
        }*/

        public static bool listContainsCategory(IList<CategoryJSON> theList, CategoryJSON theCategory) {
            bool found = false;
            foreach(CategoryJSON category in theList) {
                if (category.categoryId == theCategory.categoryId) {
                    found = true;
                    break;
                }
            }
            return found;
        }

        public static bool removeCategoryFromList(IList<CategoryJSON> theList, CategoryJSON theCategory) {
            bool removed = false;
            CategoryJSON foundCat = null;
            foreach (CategoryJSON category in theList) {
                if (category.categoryId == theCategory.categoryId) {
                    removed = true;
                    foundCat = category;
                    break;
                }
            }
            theList.Remove(foundCat);
            return removed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="theList"></param>
        /// <param name="description"></param>
        /// <returns>Null if category description is not present.</returns>
        public static CategoryJSON getCategoryByDescription(IList<CategoryJSON> theList, string description) {
            CategoryJSON result = null;
            foreach(CategoryJSON category in theList) {
                if (category.description.Equals(description)) {
                    result = category;
                    break;
                }
            }
            return result;
        }

        public static async Task<string> requestFromServerAsync(HttpMethod method, string apiCall, string jsonQuery) {
            Debug.WriteLine("DHB:GlobalSingletonHelpers:requestFromServerAsync start");
            string result = "fail";
            try {
                HttpClient client = new HttpClient();

                string requestCall = null;
                HttpRequestMessage request = null;

                if (method == HttpMethod.Get) {
                    requestCall = GlobalStatusSingleton.activeURL + apiCall + jsonQuery;
                    Uri req = new Uri(requestCall);
                    request = new HttpRequestMessage(method, req);
                } else {
                    client.BaseAddress = new Uri(GlobalStatusSingleton.activeURL);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    requestCall = apiCall;
                    request = new HttpRequestMessage(method, requestCall);
                    request.Content = new StringContent(jsonQuery, Encoding.UTF8, "application/json");
                }

                request.Headers.Add("Authorization", GlobalSingletonHelpers.getAuthToken());

                HttpResponseMessage response = await client.SendAsync(request);
                if ((response.StatusCode == System.Net.HttpStatusCode.OK) || (response.StatusCode == System.Net.HttpStatusCode.Created)) {
                    // do I need these?
                    result = await response.Content.ReadAsStringAsync();
                } else if ((response.StatusCode == System.Net.HttpStatusCode.NoContent) || (response.StatusCode == System.Net.HttpStatusCode.NotFound)) {
                    result = EMPTY;
                } else {
                    // pooh. what do i do here?
                    //result = "internal fail; why?";
                    // server failure. keep the msg as a fail for correct onVote processing
                    // do we get back json?
                    result = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("DHB:GlobalSingletonHelpers:requestFromServerAsync " + apiCall + " result recvd:" +result);
                    result = "fail";
                }
                response.Dispose();
                request.Dispose();
                client.Dispose();
            } catch (System.Net.WebException err) {
                //result = "exception";
                // web failure. keep the msg as a simple fail for correct onVote processing
                Debug.WriteLine(err.ToString());
            } catch (Exception e) {
                Debug.WriteLine("DHB:GlobalSingletonHelpers:requestFromServerAsync:Exception apiCall:" + apiCall);
                Debug.WriteLine(e.ToString());
            }
            Debug.WriteLine("DHB:GlobalSingletonHelpers:requestFromServerAsync " + apiCall + " end");
            return result;
        }

        public static async Task<SKBitmap> loadBitmapAsync(Assembly assembly, long pid, int attempt = 0) {
            Debug.WriteLine("DHB:GlobalSingletonHelpers:loadBitmapAsync depth:" + attempt);
            SKBitmap output = null;
            byte[] result = await ImageScrollingPage.requestImageAsync(pid);
            if (result != null) {
                try {
                    /*
                    PreviewResponseJSON resp = JsonConvert.DeserializeObject<PreviewResponseJSON>(result);
                    if (resp != null) {
                        output = GlobalSingletonHelpers.SKBitmapFromBytes(resp.imgStr);
                    }
                    */
                    //output = SKBitmap.Decode(result);
                    output = GlobalSingletonHelpers.SKBitmapFromBytes(result);
                } catch (Exception e) {
                    Debug.WriteLine("DHB:GlobalSingletonHelpers:loadBitmapAsync err:" + e.ToString());
                }
            }
            if (output == null) {
                //output = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly);
                if (attempt < 10) {  // fail after 10 attempts.
                    await Task.Delay(3000);
                    await loadBitmapAsync(assembly, pid, attempt + 1);  // will recurse down till we get it.  
                } else {
                    Debug.WriteLine("DHB:GlobalSingletonHelpers:loadBitmapAsync MaxDepth hit");
                    output = GlobalSingletonHelpers.loadSKBitmapFromResourceName("ImageImprov.IconImages.alert.png", assembly);
                }
            }
            return output;
        }
        public static void SortAndReverse<T>(this ObservableCollection<T> observable) where T : IComparable<T>, IEquatable<T> {
            List<T> sorted = observable.OrderBy(x => x).ToList();
            sorted.Reverse();

            int ptr = 0;
            while (ptr < sorted.Count) {
                if (!observable[ptr].Equals(sorted[ptr])) {
                    T t = observable[ptr];
                    observable.RemoveAt(ptr);
                    observable.Insert(sorted.IndexOf(t), t);
                } else {
                    ptr++;
                }
            }
        }

    }
}
