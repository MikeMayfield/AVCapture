using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
            var databaseFingerprintHashes = new DatabaseFingerprints().GenerateFingerprintsForAllShows();

            var matchFingerprints = new MatchFingerprints();
            var matchedEpisodeId = matchFingerprints.IdentifyEpisodeForAudioMatch(databaseFingerprintHashes);

            Console.WriteLine("Finished, matching episode ID: {0}", matchedEpisodeId);
            Thread.Sleep(5000);
        }
    }
}
