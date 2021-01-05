using GleamTech.VideoUltimate;
using System;
using System.Diagnostics;
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
        private VideoFrameReader videoFrameReader;
        private FrameBuffer frame = new FrameBuffer();

        private string inputFilePath;
        private string tmpAudioPath = Directory.GetCurrentDirectory() + "\\" + "temp.wav";
        private string tmpPcmPath = Directory.GetCurrentDirectory() + "\\" + "temp.pcm";

        /// <summary>
        /// Open an video (MP4, etc.) file for reading
        /// </summary>
        /// <param name="filePath">Full path to file to open</param>
        /// <returns>TRUE if open was successful and media is ready to be read</returns>
        public bool Open(string filePath) {

            //TODO Open the file and prepare for reading decompressed/decoded frames
            frame.audioSampleRateHz = 48000;
            inputFilePath = filePath;
            videoFrameReader = new VideoFrameReader(inputFilePath);
            if (videoFrameReader != null)
                return true;  //TODO Return the proper result

            return false;
        }

        /// <summary>
        /// Get next audio or video frame from input
        /// </summary>
        /// <returns>Frame buffer for next audio or video frame. NULL if end of file</returns>
        public FrameBuffer NextFrame() {
            //TODO Get next audio or video frame and return in FrameBuffer
            frame.sampleTime += 100;

            if (videoFrameReader.Read())
            {
                frame.videoBuffer = videoFrameReader.GetFrame();

                // Audio
                if (ExtractAudio(inputFilePath, tmpAudioPath, (float)videoFrameReader.CurrentFrameNumber / (float)videoFrameReader.FrameRate, ((float)videoFrameReader.CurrentFrameNumber / (float)videoFrameReader.FrameRate) + 1f / (float)videoFrameReader.FrameRate))
                    GetAudioPcmBuffer(tmpAudioPath, tmpPcmPath);

                return frame;
            }

            return null;
        }

        public bool ExtractAudio(string input, string output, float start, float end)
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
            ffmpegProcess.StartInfo.Arguments = " -i " + inputFile + " -ss " + start + " -to " + end + " -vn -acodec pcm_s16le -ac 1 " + "-ar " + frame.audioSampleRateHz.ToString() + " " + outputFile;
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();
            if (!ffmpegProcess.HasExited)
            {
                ffmpegProcess.Kill();
            }
            ffmpegProcess.Close();

            return true;
        }

        public void GetAudioPcmBuffer(string input, string output)
        {
            var inputFile = input;
            var outputFile = output;
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            if (File.Exists(inputFile))
            {
                var ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardInput = true;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\FFMpeg\\ffmpeg.exe";
                ffmpegProcess.StartInfo.Arguments = " -y -i " + inputFile + " -acodec pcm_s16le -f s16le " + outputFile;
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();
                if (!ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                }
                ffmpegProcess.Close();

                byte[] buffer = File.ReadAllBytes(output);
                short[] samples = new short[buffer.Length / 2];
                Buffer.BlockCopy(buffer, 0, samples, 0, buffer.Length);

                frame.audioBuffer = samples;
            }
        }

        /// <summary>
        /// Close the input file. Once closed, NextFrame cannot be used
        /// </summary>
        public void Close() {
            //TODO Do any close post-processing
        }
    }
}
