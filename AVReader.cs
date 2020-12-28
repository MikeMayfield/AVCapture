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
        /// <summary>
        /// Open an video (MP4, etc.) file for reading
        /// </summary>
        /// <param name="filePath">Full path to file to open</param>
        /// <returns>TRUE if open was successful and media is ready to be read</returns>
        public bool Open(string filePath) {
            //TODO Open the file and prepare for reading decompressed/decoded frames
            return true;  //TODO Return the proper result
        }

        /// <summary>
        /// Get next audio or video frame from input
        /// </summary>
        /// <returns>Frame buffer for next audio or video frame. NULL if end of file</returns>
        public FrameBuffer NextFrame() {
            //TODO Get next audio or video frame and return in FrameBuffer
            return null;  //TODO Return the proper result
        }

        /// <summary>
        /// Close the input file. Once closed, NextFrame cannot be used
        /// </summary>
        public void Close() {
            //TODO Do any close post-processing
        }
    }
}
