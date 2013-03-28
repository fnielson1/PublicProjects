using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Sockets;
using MonitorCommunication;

namespace Monitor_Client
{
    public static class Screenshot
    {
        private static Graphics gfxScreenshot;

        /// <summary>
        /// Used for writing the image to a stream
        /// </summary>
        private static Communication _communication;


        /// <summary>
        /// Get a screenshot of the primary screen and return a Bitmap object that is jpeg encoded
        /// </summary>
        /// <param name="quality">An integer from 0 to 100, with 100 being the highest quality</param>
        /// <returns>A Bitmap of the screenshot</returns>
        public static Bitmap GetScreenshotPrimary(int quality)
        {
            return GetScreenshot(Screen.PrimaryScreen, quality);            
        }


        /// <summary>
        /// Get a screenshot of all the screens and return a Bitmap list of jpeg encoded objects
        /// </summary>
        /// <param name="quality">An integer from 0 to 100, with 100 being the highest quality</param>
        /// <returns>A Bitmap object list containing the screen shots</returns>
        public static List<Bitmap> GetScreenshotAll(int quality)
        {
            List<Bitmap> screenList = new List<Bitmap>(Screen.AllScreens.Length);
            foreach (Screen currentScreen in Screen.AllScreens)
            {
                screenList.Add(GetScreenshot(currentScreen, quality));
            }

            return screenList;
        }


        /// <summary>
        /// Get a screenshot of the primary screen and return a Bitmap object that is jpeg encoded
        /// </summary>
        /// <param name="currentScreen">The screen to get a screenshot of</param>
        /// <param name="quality">An integer from 0 to 100, with 100 being the highest quality</param>
        /// <returns>A Bitmap of the screenshot</returns>
        public static Bitmap GetScreenshot(Screen currentScreen, int quality)
        {
            // Set the bitmap object to the size of the screen
            Bitmap bmpScreenshot = new Bitmap(currentScreen.Bounds.Width, currentScreen.Bounds.Height, PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap
            gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner
            gfxScreenshot.CopyFromScreen(currentScreen.Bounds.X, currentScreen.Bounds.Y, 0, 0, currentScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

            // Convert the image to a jpeg to make the image smaller
            bmpScreenshot = ConvertToJpeg(bmpScreenshot, quality);

            return bmpScreenshot;
        }


        /// <summary>
        /// Writes a jpeg image to a stream
        /// </summary>
        /// <param name="img">The image to write</param>
        /// <param name="sStream">The stream to write the image to</param>
        /// <param name="quality">An integer from 0 to 100, with 100 being the highest quality</param>
        public static void WriteJpegToStream(Image img, Stream sStream, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("The quality argument when writing the image must be between 0 and 100");

            // Encoder parameter for image quality
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            // Jpeg image codec
            ImageCodecInfo jpegCodec = _GetEncoderInfo("image/jpeg");

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            // Convert the image to a byte array that contains the size of the image (in bytes) and send it
            byte[] imageBytes = ImageToByteArray(img, ImageFormat.Jpeg);    // Get a byte array representing the image
            _communication = new Communication(sStream);
            _communication.Write(imageBytes, Communication.DataType.IMAGE); // Write the image to the stream
        }


        // Code taken from http://bytes.com/topic/c-sharp/answers/836433-asynchronous-image-transfer-over-tcp-ip-socket
        /// <summary>
        /// Converts an image into a byte array
        /// </summary>
        /// <param name="imageIn">The image to convert</param>
        /// <param name="format">The image format to use</param>
        /// <returns>The byte array representing the image</returns>
        public static byte[] ImageToByteArray(Image imageIn, ImageFormat format)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, format);
            return ms.GetBuffer();
        }


        // Code taken from http://www.switchonthecode.com/tutorials/csharp-tutorial-image-editing-saving-cropping-and-resizing
        /// <summary>
        /// Converts an image to a jpeg image, with the given quality
        /// </summary>
        /// <param name="img">An image object</param>
        /// <param name="quality">An integer from 0 to 100, with 100 being the highest quality</param>
        /// <returns>The image in the jpeg format</returns>
        public static Bitmap ConvertToJpeg(Image img, int quality)
        {
            if (quality <= 0 || quality > 100)
                throw new ArgumentOutOfRangeException("The quality argument when converting the image must be between 1 and 100");


            // Encoder parameter for image quality
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            // Jpeg image codec
            ImageCodecInfo jpegCodec = _GetEncoderInfo("image/jpeg");

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            // Write the image to a stream to convert it to a jpeg
            MemoryStream imageStream = new MemoryStream();
            img.Save(imageStream, jpegCodec, encoderParams);
            Bitmap jpegImage = new Bitmap(imageStream);

            return jpegImage;
        }

        /// <summary>
        /// Returns the image codec with the given mime type
        /// </summary>
        /// <returns>The codec pertaining to that specified image type</returns>
        private static ImageCodecInfo _GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }


        /// <summary>
        /// Resizes an image.
        /// </summary>
        /// <param name="imgToResize">The image to resize</param>
        /// <param name="size">A size object containing the size of image you want</param>
        /// <returns>The resized image</returns>
        public static Bitmap ResizeImage(Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage(b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return b;
        }
    }
}
