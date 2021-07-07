using GleamTech.VideoUltimate;
using System;
using System.Diagnostics;
using System.IO;

namespace AVCapture
{
    /// <summary>
    /// Utility for reading an MP4 or similar file.
    /// 
    /// NOTE: Do not use this class from the UI thread
    /// 
    /// Typical operation:
    /// . Open MP4 AV file
    /// . While NextFrame returns a non-null buffer
    /// . . Process the frame
    /// . Close AV file
    /// </summary>
    public class AVReader {
        private VideoFrameReader videoFrameReader;
        private FrameBuffer frame = new FrameBuffer();
        private WavFile wavFile;
        //private double frameDurationSec;
        //private double timestampSec = 0.0;
        private UInt64 frameDurationTicks;  //Duration of each frame in 100ns ticks (only accurate when audioBps % sampleSize == 0)
        private UInt64 timestampTicks;  //Sample time in 100ns ticks
        public const UInt64 TICKS_PER_SECOND = 10000000;  //100ns per tick
        private string wavFilePath;

        /// <summary>
        /// Open an audio/video (MP4, etc.) file for reading
        /// </summary>
        /// <param name="filePath">Full path to file to open</param>
        /// <param name="bufferSize">Buffer size, in samples, eg. in words </param>
        /// <returns>TRUE if open was successful and media is ready to be read</returns>
        public void Open(string filePath, int bufferSize = 0) {
            if (bufferSize == 0) {
                PrepareVideoExtraction(filePath);
                PrepareAudioExtraction(filePath, bufferSize, videoFrameReader.FrameRate);
            } else {
                PrepareAudioExtraction(filePath, bufferSize, 0);
            }
        }

        /// <summary>
        /// Close the input file. Once closed, NextFrame cannot be used
        /// </summary>
        public void Close() {
            if (wavFile != null) {
                wavFile.Close();
                wavFile = null;
                //DeleteExistingFile(wavFilePath);
            }
            if (videoFrameReader != null) {
                videoFrameReader.Dispose();
                videoFrameReader = null;
            }
        }

        /// <summary>
        /// Get next audio/video frame from input
        /// </summary>
        /// <returns>Frame buffer for next audio and video frame. NULL if end of file</returns>
        public FrameBuffer NextFrame() {
            //frame.SampleTime = (long)(timestampSec * TICKS_PER_SECOND);
            frame.SampleTime = timestampTicks;

            bool haveVideoBuffer = false;
            bool haveAudioBuffer;
            if (videoFrameReader != null && videoFrameReader.Read()) {
                frame.VideoBuffer = videoFrameReader.GetFrame();
                haveVideoBuffer = (frame.VideoBuffer != null);
            }

            haveAudioBuffer = wavFile.NextSample(frame.AudioBuffer);
            //timestampSec += frameDurationSec;
            timestampTicks += frameDurationTicks;

            return (haveVideoBuffer || haveAudioBuffer) ? frame : null;
        }

        private void PrepareVideoExtraction(string filePath) {
            //video is not supported in this implementation
            //videoFrameReader = new VideoFrameReader(filePath);
            //if (videoFrameReader != null) {
            //    frameDurationSec = 1.0 / videoFrameReader.FrameRate;
            //} else {
                throw new InvalidDataException("Unable to open video file");
            //}
        }

        private void PrepareAudioExtraction(string filePath, int bufferSize, double videoFrameRate) {
            CreateWavFileFromAVFile(filePath);
            wavFile = new WavFile(wavFilePath);

            frame.AudioSampleRateHz = wavFile.SamplesPerSec;
            if (frame.AudioSampleRateHz != bufferSize) {
                Console.WriteLine("**ERROR: Sample rate of file ({0}) does not match defined sample rate ({1})", frame.AudioSampleRateHz, bufferSize);
            }

            if (bufferSize == 0) {  //Combined audio/video extraction
                frame.AudioBuffer = new Int16[(int) ((double) wavFile.SamplesPerSec / videoFrameRate)];
            } else {  //Audio extraction only
                frame.AudioBuffer = new Int16[bufferSize];
                frameDurationTicks = TICKS_PER_SECOND / (UInt64)(frame.AudioSampleRateHz / bufferSize);
            }
        }

        private void CreateWavFileFromAVFile(string avFilePath) {
            wavFilePath = avFilePath + ".wav";
            //DeleteExistingFile(wavFilePath);

            if (!File.Exists(wavFilePath)) {
                var ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardInput = true;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\FFMpeg\\ffmpeg.exe";
                ffmpegProcess.StartInfo.Arguments = String.Format(" -i {0} -vn -acodec pcm_s16le -ac 1 -ar 44100 {2}",  //PCM-16, mono
                    avFilePath, frame.AudioSampleRateHz, wavFilePath);
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();
                if (!ffmpegProcess.HasExited) {
                    ffmpegProcess.Kill();
                }
                ffmpegProcess.Close();
            }
        }

        private void DeleteExistingFile(string filePath) {
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        }
    }
}
