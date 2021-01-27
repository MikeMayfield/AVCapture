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
            AudioFileFingerprinter fingerprinter;

            //fingerprinter = new AudioFileFingerprinter();  //TODO
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\Sample1KHz192K.mp4", 2, databaseFingerprintHashes);

            //fingerprinter = new AudioFileFingerprinter();  //TODO
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\Capture1KHz.mp4", 2, databaseFingerprintHashes);

            fingerprinter = new AudioFileFingerprinter();
            fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo.mp4", 1, databaseFingerprintHashes);

            fingerprinter = new AudioFileFingerprinter();
            fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo2.mp4", 2, databaseFingerprintHashes);

            //TODO
            var fileList = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\TestFiles", "*.mp4");
            var episodeId = 3;
            foreach (var filePath in fileList) {
                fingerprinter = new AudioFileFingerprinter();
                fingerprinter.GenerateFingerprintsForFile(filePath, episodeId++, databaseFingerprintHashes);
            }
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\00aac5fe-57d6-4d34-a801-eb685a3e257d.mp4", 3, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\00aad83f-4431-4451-8c9a-556fce11f3da.mp4", 4, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\00ab6f64-a242-4762-9adc-8bf8266a3dec.mp4", 5, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\00af3e23-1b46-4a6e-87ea-697625b49e3d.mp4", 6, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0a4c38b8-6b86-48ca-94de-cc8b1ba93e85.mp4", 7, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0ae68dca-4b11-42a5-afae-58927f7da97f.mp4", 8, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0c702564-ad3f-4b21-8bf5-b8eb54f5d8d2.mp4", 9, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0cc51e5c-0b4e-48c4-85a5-4c121492d33c.mp4", 10, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0d6d60b7-71dd-430b-81e7-168591a1caa0.mp4", 11, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0FCB6B13-B872-400E-8A5E-73D4B191AC70.mp4", 12, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\01c86b55-5b70-4e15-a59f-e6192fb6517d.mp4", 13, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\02b0c0dc-03a3-4b35-9447-2a3f7affc0d1.mp4", 14, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\019e40c6-58d4-4154-b1e1-fa3f33de9a9f.mp4", 15, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\0063101c-d589-48fa-8a77-80583739eee9.mp4", 16, databaseFingerprintHashes);
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\06637013-8542-479c-b200-ecd9211dc12e.mp4", 17, databaseFingerprintHashes);

            return databaseFingerprintHashes;
        }
    }
}
