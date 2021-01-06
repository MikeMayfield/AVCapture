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
    public class AVReader {
        private VideoFrameReader videoFrameReader;
        private FrameBuffer frame = new FrameBuffer();
        private WavFile wavFile;
        private double frameDurationSec;
        private double timestampSec = 0.0;
        private const double TICKS_PER_SECOND = 10000000.0;
        private string wavFilePath = Directory.GetCurrentDirectory() + "\\" + "temp.wav";

        /// <summary>
        /// Open an video (MP4, etc.) file for reading
        /// </summary>
        /// <param name="filePath">Full path to file to open</param>
        /// <returns>TRUE if open was successful and media is ready to be read</returns>
        public bool Open(string filePath) {
            PrepareVideoExtraction(filePath);
            PrepareAudioExtraction(filePath, videoFrameReader.FrameRate);
            return true;
        }

        /// <summary>
        /// Get next audio or video frame from input
        /// </summary>
        /// <returns>Frame buffer for next audio or video frame. NULL if end of file</returns>
        public FrameBuffer NextFrame() {
            frame.SampleTime = (long)(timestampSec * TICKS_PER_SECOND);

            if (videoFrameReader.Read())
            {
                frame.VideoBuffer = videoFrameReader.GetFrame();
                wavFile.NextSample(frame.AudioBuffer);
                timestampSec += frameDurationSec;
                return frame;
            }

            return null;
        }

        /// <summary>
        /// Close the input file. Once closed, NextFrame cannot be used
        /// </summary>
        public void Close() {
            if (wavFile != null) {
                wavFile.Close();
                DeleteExistingFile(wavFilePath);
            }
            if (videoFrameReader != null) {
                videoFrameReader.Dispose();
            }
        }

        private void PrepareVideoExtraction(string filePath) {
            videoFrameReader = new VideoFrameReader(filePath);
            if (videoFrameReader != null) {
                frameDurationSec = 1.0 / videoFrameReader.FrameRate;
            } else {
                throw new InvalidDataException("Unable to open video file");
            }
        }

        private void PrepareAudioExtraction(string filePath, double videoFrameRate) {
            CreateWavFileFromAVFile(filePath, wavFilePath);
            wavFile = new WavFile(wavFilePath);

            frame.AudioSampleRateHz = wavFile.SamplesPerSec;
            frame.AudioBuffer = new Int16[(int) ((double) wavFile.SamplesPerSec / videoFrameRate)];
        }

        private bool CreateWavFileFromAVFile(string avFilePath, string wavFilePath) {
            DeleteExistingFile(wavFilePath);

            var ffmpegProcess = new Process();
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardInput = true;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.StartInfo.RedirectStandardError = true;
            ffmpegProcess.StartInfo.CreateNoWindow = true;
            ffmpegProcess.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\FFMpeg\\ffmpeg.exe";
            ffmpegProcess.StartInfo.Arguments = String.Format(" -i {0} -vn -acodec pcm_s16le -ac 1 -ar {1} {2}",  //PCM-16, mono
                avFilePath, frame.AudioSampleRateHz, wavFilePath);
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();
            if (!ffmpegProcess.HasExited) {
                ffmpegProcess.Kill();
            }
            ffmpegProcess.Close();

            return true;
        }

        private void DeleteExistingFile(string filePath) {
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        }
    }
}
