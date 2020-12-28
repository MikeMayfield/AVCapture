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
        //Relative time when frame starts (in 100 nanosecond increments)
        //The base time (0 ns) is when the first frame is captured
        public UInt64 sampleTime { get; internal set; }

        //Audio sample rate, in samples/second (e.g. 44.1KHz is 44100)
        //Undefined for video frame.
        public Int32 audioSampleRateHz { get; internal set; }

        //Audio buffer (if audio frame, else NULL)
        //Signed 16-bit mono PCM format. Stereo channels are combined by summation to 
        //make a single, mono channel. Channels other than front stereo are discarded.
        public Int16[] audioBuffer { get; internal set; }

        //Video buffer (if video frame, else NULL)
        //The video frame is encapsulated in a Bitmap instance. This 
        //allows efficient transfer of data from an internal video frame.
        public Bitmap videoBuffer { get; internal set; }
    }
}
