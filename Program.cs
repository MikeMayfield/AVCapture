using System;
using System.Drawing.Imaging;
using System.IO;

namespace AVCapture
{
    /// <summary>
    /// Sample program using AVReader. This class is provided to support testing of
    /// implementation of AVReader and is not part of the deliverable for the 
    /// development project
    /// </summary>
    class Program
    {
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyy_MM_dd_HH_mm_ss_ffff");
        }

        /// <summary>
        /// Open the file specified in args[0] and process each A/V buffer in file
        /// </summary>
        static void Main(string[] args) {
            var avReader = new AVReader();
            string path = Directory.GetCurrentDirectory() + "\\" + "demo.mp4";
            if (avReader.Open(path)) {
                var frameBuffer = avReader.NextFrame();
                while (frameBuffer != null) {
                    if (frameBuffer.audioBuffer != null) {
                        //TODO Do something with the audio buffer
                    } else {
                        //TODO Do something with the video buffer
                        //string imageName = Directory.GetCurrentDirectory() + "\\result\\" + GetTimestamp(DateTime.Now) + ".jpg";
                        //frameBuffer.videoBuffer.Save(imageName, ImageFormat.Jpeg);
                    }
                    frameBuffer = avReader.NextFrame();
                }
                avReader.Close();
            }
        }
    }
}
