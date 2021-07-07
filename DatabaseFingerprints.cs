using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AVCapture
{
    class DatabaseFingerprints
    {
        bool LOAD_FROM_JSON = false;
        string DATABASE_FINGERPRINTS_JSON_FILE_PATH = Directory.GetCurrentDirectory() + "\\DatabaseFingerprints.json";
        Dictionary<UInt32, FingerprintGroup> databaseFingerprintHashes;

        public Dictionary<UInt32, FingerprintGroup> GenerateFingerprintsForAllShows() {
            if (LOAD_FROM_JSON) {
                databaseFingerprintHashes = LoadFromJson(DATABASE_FINGERPRINTS_JSON_FILE_PATH);
            } else { 
                databaseFingerprintHashes = new Dictionary<UInt32, FingerprintGroup>(20000);

                GenerateFingerprintsForFile("SampleVideo.mp4", 1);
                GenerateFingerprintsForFile("SampleVideo2.mp4", 10);

                ProcessAllTestFiles();

                SaveToJson(DATABASE_FINGERPRINTS_JSON_FILE_PATH);
            }

            return databaseFingerprintHashes;
        }

        private void ProcessAllTestFiles() {
            var maxFileToProcess = 50;
            var fileList = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\TestFiles", "*.mp4");
            UInt32 episodeId = 101;
            using (var countdownEvent = new CountdownEvent(Math.Min(fileList.Length, maxFileToProcess))) {
                foreach (var filePath in fileList) {
                    if (maxFileToProcess-- <= 0)
                        break;
                    ThreadPool.QueueUserWorkItem(kvp => {
                            var args = (KeyValuePair<string, UInt32>) kvp;
                            GenerateFingerprintsForFile(args.Key, args.Value);
                            countdownEvent.Signal();
                        }, new KeyValuePair<string, UInt32>(filePath, episodeId));
                    episodeId++;
                }
                countdownEvent.Wait();
            }
        }

        private void GenerateFingerprintsForFile(string filename, UInt32 fileId) {
            var fingerprinter = new AudioFileFingerprinter();
            string filePath = filename.IndexOf('\\') > 0 ? filename : $"{Directory.GetCurrentDirectory()}\\{filename}";
            fingerprinter.GenerateFingerprintsForFile(filePath, fileId, databaseFingerprintHashes);
        }

        void SaveToJson(string filePath) {
            using (StreamWriter file = File.CreateText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, databaseFingerprintHashes, typeof(FingerprintGroup));
            }
        }

        Dictionary<UInt32, FingerprintGroup> LoadFromJson(string filePath) {
            Dictionary<UInt32, FingerprintGroup> result;
            using (StreamReader file = File.OpenText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                result = (Dictionary<UInt32, FingerprintGroup>) serializer.Deserialize(file, typeof(Dictionary<UInt32, FingerprintGroup>));
            }

            return result;
        }
    }
}
