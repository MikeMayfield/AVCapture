using AForge.Video.FFMPEG;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.Diagnostics;
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
            GetAudioBuffer(inputFilePath, outputFilePath, (float)frameCount / (float)reader.FrameRate, 1f / (float)reader.FrameRate);
            byte[] buffer = File.ReadAllBytes(outputFilePath);
            short[] samples = new short[(buffer.Length-78)/2];
            Buffer.BlockCopy(buffer, 78, samples, 0, buffer.Length-78);
            File.Delete(outputFilePath);

            frame.audioBuffer = samples;
            frameCount++;

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

        static public void GetAudioBuffer(string input, string output, float start, float end)
        {
            var inputFile = input;
            var outputFile = output;
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            var ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardInput = true;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.StartInfo.RedirectStandardError = true;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\FFMpeg\\ffmpeg.exe";
            ffmpegProcess.StartInfo.Arguments = " -i " + inputFile + " -ss " + start + " -to " + end + " -vn -acodec pcm_s16le -ac 2 -ar 44100 " + outputFile;
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();
            if (!ffmpegProcess.HasExited)
            {
                ffmpegProcess.Kill();
            }
            ffmpegProcess.Close();
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
