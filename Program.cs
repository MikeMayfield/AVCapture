using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;

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
        /// Open the test input file and process each A/V buffer in file
        /// </summary>
        static void Main(string[] args) {
            var fingerprintHashes = new Dictionary<int, List<Fingerprint>>(1000000);
            var fingerprinter = new AudioFileFingerprinter();
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo.mp4";
            fingerprinter.GenerateFingerprintsForFile(path, 1, fingerprintHashes);

            path = Directory.GetCurrentDirectory() + "\\SampleVideo2.mp4";
            fingerprinter.GenerateFingerprintsForFile(path, 2, fingerprintHashes);

            Debug.WriteLine("Finished");
        }


        private static Int16[] SimulatedTone(int frequencyHz) {
            var simulatedToneDouble = DSPLib.DSP.Generate.ToneSampling(10, frequencyHz, 44100, 2048);
            var simulatedTone = new Int16[2048];
            for (int i = 0; i < simulatedToneDouble.Length; i++) {
                simulatedTone[i] = (Int16) (simulatedToneDouble[i] * 1000.0);
            }

            return simulatedTone;
        }
    }
}
