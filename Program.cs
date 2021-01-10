using System;
using System.Diagnostics;
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
        /// <summary>
        /// Open the test input file and process each A/V buffer in file
        /// </summary>
        static void Main(string[] args) {
            int AUDIO_BUFFER_SIZE = 2048;  //Size of buffer for audio-only processing. 0 if audio+video

            var avReader = new AVReader();
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo.mp4";

            avReader.Open(path, AUDIO_BUFFER_SIZE);
            var frameBuffer = avReader.NextFrame();
            while (frameBuffer != null) {
                //Process the audio buffer, if provided
                if (frameBuffer.AudioBuffer != null) {
                    Console.WriteLine("AUDIO: Time: {0}, Hz: {1}, Length: {2}", 
                        frameBuffer.SampleTime, frameBuffer.AudioSampleRateHz, frameBuffer.AudioBuffer.Length);
                } 
                    
                //Process the video buffer if provided
                if (frameBuffer.VideoBuffer != null) {
                    Console.WriteLine("VIDEO: Time: {0}",
                        frameBuffer.SampleTime);
                    //string imageName = Directory.GetCurrentDirectory() + "\\result\\" + frameBuffer.SampleTime + ".jpg";
                    //frameBuffer.VideoBuffer.Save(imageName, ImageFormat.MemoryBmp);
                    frameBuffer.VideoBuffer.Dispose();
                }

                frameBuffer = avReader.NextFrame();
            }
            avReader.Close();

            Debug.WriteLine("Finished");
        }
    }
}
