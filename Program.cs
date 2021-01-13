using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
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
            var samples = new List<Sample>();

            var avReader = new AVReader();
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo.mp4";

            avReader.Open(path, AUDIO_BUFFER_SIZE);
            var frameBuffer = avReader.NextFrame();
            int frequency = 0;

            while (frameBuffer != null) {
                //Process the audio buffer, if provided
                if (frameBuffer.AudioBuffer != null) {
                    //Console.WriteLine("AUDIO: Time: {0}, Hz: {1}, Length: {2}", 
                    //    frameBuffer.SampleTime, frameBuffer.AudioSampleRateHz, frameBuffer.AudioBuffer.Length);
                    var sampleSummary = new Sample(frameBuffer.AudioSampleRateHz, frameBuffer.SampleTime, frameBuffer.AudioBuffer);
                    //var sampleSummary = new Sample(frameBuffer.AudioSampleRateHz, frameBuffer.SampleTime, SimulatedTone(frequency++ % 19000 + 498));
                    samples.Add(sampleSummary);
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

        private static Int16[] SimulatedTone(int frequencyHz) {
            var simulatedToneDouble = DSPLib.DSP.Generate.ToneSampling(10, frequencyHz, 44100, 2048);
            var simulatedTone = new Int16[2048];
            for (int i = 0; i < simulatedToneDouble.Length; i++) {
                simulatedTone[i] = (Int16) (simulatedToneDouble[i] * 1000.0);
            }

            return simulatedTone;
        }
    }
}
