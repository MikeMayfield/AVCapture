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

            //fingerprinter = new AudioFileFingerprinter();  //TODO
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\Sample1KHz192K.mp4", 2, databaseFingerprintHashes);

            //fingerprinter = new AudioFileFingerprinter();  //TODO
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\Capture1KHz.mp4", 2, databaseFingerprintHashes);

            //string path = Directory.GetCurrentDirectory() + "\\SampleVideo.mp4";
            //fingerprinter.GenerateFingerprintsForFile(path, 1, databaseFingerprintHashes);

            fingerprinter = new AudioFileFingerprinter();
            fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo2.mp4", 2, databaseFingerprintHashes);

            return databaseFingerprintHashes;
        }
    }
}
