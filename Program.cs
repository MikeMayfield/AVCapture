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
        /// Open the file specified in args[0] and process each A/V buffer in file
        /// </summary>
        static void Main(string[] args) {
            var avReader = new AVReader();
            if (avReader.Open(args[0])) {
                var frameBuffer = avReader.NextFrame();
                while (frameBuffer != null) {
                    if (frameBuffer.audioBuffer != null) {
                        //TODO Do something with the audio buffer
                    } else {
                        //TODO Do something with the video buffer
                    }
                    frameBuffer = avReader.NextFrame();
                }
                avReader.Close();
            }
        }
    }
}
