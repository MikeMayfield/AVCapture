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
            var avReader = new AVReader();
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo.mp4";

            if (avReader.Open(path)) {
                var frameBuffer = avReader.NextFrame();
                while (frameBuffer != null) {
                    //Process the audio buffer, if provided
                    if (frameBuffer.audioBuffer != null) {
                        Console.WriteLine("AUDIO: Time: {0}, Hz: {1}, Length: {2}", 
                            frameBuffer.sampleTime, frameBuffer.audioSampleRateHz, frameBuffer.audioBuffer.Length);
                    } 
                    
                    //Process the video buffer if provided
                    if (frameBuffer.videoBuffer != null) {
                        Console.WriteLine("VIDEO: Time: {0}, File: {0}.jpg",
                            frameBuffer.sampleTime);
                        string imageName = Directory.GetCurrentDirectory() + "\\result\\" + frameBuffer.sampleTime + ".jpg";
                        frameBuffer.videoBuffer.Save(imageName, ImageFormat.MemoryBmp);
                    }

                    frameBuffer = avReader.NextFrame();
                }
                avReader.Close();
            } else {
                Console.WriteLine("Unable to open input file");
            }

            Debug.WriteLine("Finished");
        }
    }
}
