using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    class DatabaseFingerprints
    {
        public Dictionary<int, List<Fingerprint>> GenerateFingerprintsForAllShows() {
            var databaseFingerprintHashes = new Dictionary<int, List<Fingerprint>>(20000);
            var fingerprinter = new AudioFileFingerprinter();

            string path = Directory.GetCurrentDirectory() + "\\SampleVideo.mp4";
            fingerprinter.GenerateFingerprintsForFile(path, 1, databaseFingerprintHashes);

            path = Directory.GetCurrentDirectory() + "\\SampleVideo2.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprinter.GenerateFingerprintsForFile(path, 2, databaseFingerprintHashes);

            return databaseFingerprintHashes;
        }
    }
}
