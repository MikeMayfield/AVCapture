using AForge.Video.FFMPEG;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace AVCapture
{
    /// <summary>
    /// Utility for reading an MP4 or similar file.
    /// 
    /// Typical operation:
    /// . Open video file
    /// . While NextFrame returns a non-null buffer
    /// . . Process the frame
    /// . Close video file
    /// </summary>
    public class AVReader
    {
        private VideoFileReader reader;
        private string inputFilePath;
        private string outputFilePath = Directory.GetCurrentDirectory() + "\\" + "demo.wav";
        private FrameBuffer frame = new FrameBuffer();
        private int frameCount = 0;

        /// <summary>
        /// Open an video (MP4, etc.) file for reading
        /// </summary>
        /// <param name="filePath">Full path to file to open</param>
        /// <returns>TRUE if open was successful and media is ready to be read</returns>
        public bool Open(string filePath) {
            //TODO Open the file and prepare for reading decompressed/decoded frames            
            reader = new VideoFileReader();
            inputFilePath = filePath;
            reader.Open(inputFilePath);
            if (reader != null)
                return true;  //TODO Return the proper result

            return false;
        }

        /// <summary>
        /// Get next audio or video frame from input
        /// </summary>
        /// <returns>Frame buffer for next audio or video frame. NULL if end of file</returns>
        public FrameBuffer NextFrame() {
            //TODO Get next audio or video frame and return in FrameBuffer
            
            // Audio
            StartConverting(inputFilePath, outputFilePath, frameCount*1000 / reader.FrameRate, 1000 / reader.FrameRate);
            byte[] buffer = File.ReadAllBytes(outputFilePath);
            short[] samples = new short[(buffer.Length-78)/2];
            Buffer.BlockCopy(buffer, 78, samples, 0, buffer.Length-78);
            File.Delete(outputFilePath);

            frame.audioBuffer = samples;
            frameCount++;

            // Video
            //if ((frame.videoBuffer = reader.ReadVideoFrame()) != null)
            //{
            //    count++;
            //    return frame;
            //}
            //return null;

            using (var videoFrame = reader.ReadVideoFrame())
            {
                if (videoFrame == null)
                {
                    frameCount = 0;
                    return null;
                }

                frame.videoBuffer = videoFrame;

                // Save frames to result directory
                //string imageName = Directory.GetCurrentDirectory() + "\\result\\" + Program.GetTimestamp(DateTime.Now) + ".jpg";
                //frame.videoBuffer.Save(imageName, ImageFormat.Jpeg);
                return frame;
            }
        }

        public void StartConverting(string inputPath, string outputPath, int start, int duration)
        {
            var inputFile = new MediaFile { Filename = inputPath };
            var outputFile = new MediaFile { Filename = outputPath };

            using (var engine = new Engine())
            {
                var options = new ConversionOptions { Seek = TimeSpan.FromMilliseconds(start), MaxVideoDuration = TimeSpan.FromMilliseconds(duration) };
                engine.GetMetadata(inputFile);
                int index = inputFile.Metadata.AudioData.SampleRate.IndexOf("Hz");
                frame.audioSampleRateHz = Int32.Parse(inputFile.Metadata.AudioData.SampleRate.Remove(index));
                engine.Convert(inputFile, outputFile, options);
            }
        }

        /// <summary>
        /// Close the input file. Once closed, NextFrame cannot be used
        /// </summary>
        public void Close() {
            //TODO Do any close post-processing
            reader.Close();
        }
    }
}
