using Newtonsoft.Json;
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
        bool LOAD_FROM_JSON = false;
        string JSON_FILE_PATH = Directory.GetCurrentDirectory() + "\\Fingerprints.json";
        Dictionary<int, List<Fingerprint>> databaseFingerprintHashes;

        public Dictionary<int, List<Fingerprint>> GenerateFingerprintsForAllShows() {
            if (LOAD_FROM_JSON) {
                databaseFingerprintHashes = LoadFromJson(JSON_FILE_PATH);
            } else { 
                databaseFingerprintHashes = new Dictionary<int, List<Fingerprint>>(20000);
                AudioFileFingerprinter fingerprinter;

                //fingerprinter = new AudioFileFingerprinter();
                //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo.mp4", 1, databaseFingerprintHashes);

                fingerprinter = new AudioFileFingerprinter();
                fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo2.mp4", 2, databaseFingerprintHashes);

                //fingerprinter = new AudioFileFingerprinter();
                //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo2Capture1.mp4", 1, databaseFingerprintHashes);

                //fingerprinter = new AudioFileFingerprinter();
                //fingerprinter.GenerateFingerprintsForFile(Directory.GetCurrentDirectory() + "\\SampleVideo2Capture2.mp4", 1, databaseFingerprintHashes);

                //TODO
                var maxFileToProcess = -1;  //TODO REMOVE
                var fileList = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\TestFiles", "*.mp4");
                var episodeId = 3;
                foreach (var filePath in fileList) {
                    if (maxFileToProcess-- <= 0)  //TODO REMOVE
                        break;
                    fingerprinter = new AudioFileFingerprinter();
                    fingerprinter.GenerateFingerprintsForFile(filePath, episodeId++, databaseFingerprintHashes);
                }

                SaveToJson(JSON_FILE_PATH);
            }

            return databaseFingerprintHashes;
        }

        void SaveToJson(string filePath) {
            using (StreamWriter file = File.CreateText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, databaseFingerprintHashes, typeof(Dictionary<int, List<Fingerprint>>));
            }
        }

        Dictionary<int, List<Fingerprint>> LoadFromJson(string filePath) {
            Dictionary<int, List<Fingerprint>> result;
            using (StreamReader file = File.OpenText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                result = (Dictionary<int, List<Fingerprint>>) serializer.Deserialize(file, typeof(Dictionary<int, List<Fingerprint>>));
            }

            return result;
        }
    }
}
