using System;
using System.Drawing;

namespace AVCapture
{
    /// <summary>
    /// Audio/video frame buffer returned by AVReader.NextFrame(). 
    /// The buffer contains either audio or video data, depending on the frame type.
    /// When audio data is provided, the video data is null, and vica versa.
    /// 
    /// Audio data is a 16bit PCM capture, with multiple channels combined to create
    /// a single mono channel.
    /// 
    /// Video data is a 24bit BMP image, store in a System.Drawing.Imaging.BitmapData 
    /// instance using PixelFormat.Format24bppRgb
    /// </summary>
    public class FrameBuffer
    {
        //Relative time when frame starts (in 100ns increments)
        //The base time (0ns) is when the first frame is captured
        public long SampleTime { get; internal set; }

        //Audio sample rate, in samples/second (e.g. 44.1KHz is 44100)
        //Undefined for video-only frame.
        public Int32 AudioSampleRateHz { get; internal set; }

        //Audio buffer (if audio frame, else NULL)
        //Signed 16-bit mono PCM format. Stereo or other multi-channels are combined to 
        //make a single, mono channel.
        public Int16[] AudioBuffer { get; internal set; }

        //Video buffer (if video frame, else NULL)
        //The video frame is encapsulated in a Bitmap instance. This 
        //allows efficient transfer of data from an internal video frame.
        public Bitmap VideoBuffer { get; internal set; }
    }
}
