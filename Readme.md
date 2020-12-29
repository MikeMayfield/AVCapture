The AVCapture project will be used as part of a larger project that identifies recorded TV episodes using a phone microphone. As part of that project, I need to process the audio of a recorded TV episode and compare it to live audio captured on a phone. (The capture and processing on a phone is not part of this subproject.) I also need to process each video frame image during capture to analyze portions of each frame image (not part of the subproject).

The AVReader class encapsulates the specifics for how to get audio and video samples from an MP4 file. This allows me to do further processing of the audio and video samples without a dependency on how they are captured, meaning I don't have to know how to code for the .NET media library or third-party libraries, like FFmpeg. This provides a simplified way to access these samples from a related C# program (not part of this subproject).

The audio samples will be processed with a Fourier transform to create a series of frequency patterns. These patterns will be calculated for a recorded TV episode. They will later be recreated in real time from the phone microphone while listening to a recorded episode. This will be matched with patterns from prior recordings to attempt to identify the episode being played.

By combining the analysis from the video samples with the audio frequency patterns and their associated timestamps it should theoretically be possible to identify an episode and the current location in the playback. Once identified, it should be possible to control the playback to skip to specific locations within the streaming video.

All of the audio and video analysis and subsequent programming is beyond the scope of the work for the AVReader. The AVCapture project is used to make the subsequent work easier by providing easy access to audio and video samples.