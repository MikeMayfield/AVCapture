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
        string DATABASE_FINGERPRINTS_JSON_FILE_PATH = Directory.GetCurrentDirectory() + "\\DatabaseFingerprints.json";
        Dictionary<UInt64, FingerprintGroup> databaseFingerprintHashes;

        public Dictionary<UInt64, FingerprintGroup> GenerateFingerprintsForAllShows() {
            if (LOAD_FROM_JSON) {
                databaseFingerprintHashes = LoadFromJson(DATABASE_FINGERPRINTS_JSON_FILE_PATH);
            } else { 
                databaseFingerprintHashes = new Dictionary<UInt64, FingerprintGroup>(20000);
                AudioFileFingerprinter fingerprinter;


                GenerateFingerprintsForFile("SampleVideo.mp4", 1);
                GenerateFingerprintsForFile("C:\\Documents\\GitHub\\AVCapture_FFMpeg\\bin\\Debug\\TestFiles\\29165996-73b2-40c4-a6d4-bd6f57eafc92.mp4", 1);
                //GenerateFingerprintsForFile("20210612_111110_vol40.mp4", 2);
                //GenerateFingerprintsForFile("20210612_111301_vol50.mp4", 3);
                ////GenerateFingerprintsForFile("20210612_111440_vol60.mp4", 4);
                GenerateFingerprintsForFile("SampleVideo2.mp4", 10);
                //GenerateFingerprintsForFile("SampleVideo2Capture1.mp4", 1001);
                ////GenerateFingerprintsForFile("SampleVideo2Capture2.mp4", 1002);

                //TODO
                var maxFileToProcess = 10;  //TODO REMOVE
                var fileList = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\TestFiles", "*.mp4");
                UInt64 episodeId = 101;
                foreach (var filePath in fileList) {
                    if (maxFileToProcess-- <= 0)  //TODO REMOVE
                        break;
                    fingerprinter = new AudioFileFingerprinter();
                    GenerateFingerprintsForFile(filePath, episodeId++);
                }

                //TODO Remove low value fingerprints

                SaveToJson(DATABASE_FINGERPRINTS_JSON_FILE_PATH);
            }

            return databaseFingerprintHashes;
        }

        private void GenerateFingerprintsForFile(string filename, UInt64 fileId) {
            var fingerprinter = new AudioFileFingerprinter();
            string filePath = filename.IndexOf('\\') > 0 ? filename : $"{Directory.GetCurrentDirectory()}\\{filename}";
            fingerprinter.GenerateFingerprintsForFile(filePath, fileId, databaseFingerprintHashes, true);
        }

        void SaveToJson(string filePath) {
            using (StreamWriter file = File.CreateText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, databaseFingerprintHashes, typeof(FingerprintGroup));
            }
        }

        Dictionary<UInt64, FingerprintGroup> LoadFromJson(string filePath) {
            Dictionary<UInt64, FingerprintGroup> result;
            using (StreamReader file = File.OpenText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                result = (Dictionary<UInt64, FingerprintGroup>) serializer.Deserialize(file, typeof(Dictionary<UInt64, FingerprintGroup>));
            }

            return result;
        }
    }
}
