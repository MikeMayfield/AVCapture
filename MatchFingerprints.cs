using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    class MatchFingerprints
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="databaseHashes">Dictionary: Key=Hash of anchorFreq, targetFreq, offsetTime; Value=List of hash fingerprints</param>
        /// <returns></returns>
        public int IdentifyEpisodeForAudioMatch(Dictionary<int, List<Fingerprint>> databaseHashes) {
            var episodeFingerprintMatches = new Dictionary<int, Dictionary<long, int>>();  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta

            //Generate fingerprints for audio capture  //TODO: Use window into episode
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo2Capture2.mp4";
            //string path = Directory.GetCurrentDirectory() + "\\Sample1KHz.mp4";
            var fingerprinter = new AudioFileFingerprinter();
            var fingerprintsForCaptureFile = new Dictionary<int, List<Fingerprint>>(1000);
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile);
            SaveToJson(Directory.GetCurrentDirectory() + "\\MatchFingerprints.json", fingerprintsForCaptureFile);

            //Count number of matching hashes at the same sample time delta for each possibly matching episode.
            //  . ForEach group of fingerprints for capture file with the same hash
            //  . . For each fingerprint for this file in the group
            //  . . . If fingerprint's hash is in another file in the database
            //  . . . . Create histogram of deltas between capture sample's sample time and the sample time for samples with matching fingerprints in the database
            foreach (var hashGroup in fingerprintsForCaptureFile) {
                var fingerprintHash = hashGroup.Key;
                var fingerprintsWithSameHash = hashGroup.Value;
                foreach (var captureFingerprint in fingerprintsWithSameHash) {
                    if (databaseHashes.ContainsKey(captureFingerprint.Hash)) {
                        ProcessMatchingHash(captureFingerprint, databaseHashes[captureFingerprint.Hash], episodeFingerprintMatches);
                    }
                }
            }

            return GetEpisodeIdForMatchingEpisode(episodeFingerprintMatches);
        }

        private int GetEpisodeIdForMatchingEpisode(Dictionary<int, Dictionary<long, int>> episodeFingerprintMatches) {
            var matchingEpisodeId = 0;
            var maxHashMatchCnt = 0;
            var almostMaxHashMatchCnt = 0;
            var almostMatchingEpisodeId = 0;

            //  . For each episode that had a matching hash with the capture file
            //  . . Find maximum delta time count for possibly matching hashes within the episode
            //  . . If count is > than prior best match
            //  . . . Remember new best episode match (and extra info for debugging)
            foreach (var episodeId_sampleTimeOffsetToMatchCount in episodeFingerprintMatches) {
                var maxSubmatchCnt = 0;
                foreach (var countAtSampleTimeOffset in episodeId_sampleTimeOffsetToMatchCount.Value) {
                    if (countAtSampleTimeOffset.Value > maxHashMatchCnt) {
                        almostMaxHashMatchCnt = maxHashMatchCnt;
                        almostMatchingEpisodeId = matchingEpisodeId;
                        maxHashMatchCnt = countAtSampleTimeOffset.Value;
                        matchingEpisodeId = episodeId_sampleTimeOffsetToMatchCount.Key;
                    } else if (countAtSampleTimeOffset.Value > almostMaxHashMatchCnt) {
                        almostMaxHashMatchCnt = countAtSampleTimeOffset.Value;
                        almostMatchingEpisodeId = episodeId_sampleTimeOffsetToMatchCount.Key;
                    }
                    if (countAtSampleTimeOffset.Value > maxSubmatchCnt) {
                        maxSubmatchCnt = countAtSampleTimeOffset.Value;
                    }
                }
            }

            return (maxHashMatchCnt > 10 && (maxHashMatchCnt - almostMaxHashMatchCnt > 10 || matchingEpisodeId == almostMatchingEpisodeId)) ? matchingEpisodeId : 0;
        }

        private void ProcessMatchingHash(Fingerprint captureFingerprint, List<Fingerprint> fingerprintsInDatabaseThatMatchCaptureFingerprint, Dictionary<int, Dictionary<long, int>> episodeFingerprintMatchesToUpdate) {
            Dictionary<long, int> sampleTimeDeltaCounts;  //Key=Delta between capture fingerprint time and sample from database with the same hash. Value=Number of capture/database samples with the corresponding delta time

            //Compare the capture fingerprint all fingerprints in the database
            foreach (var matchingDatabaseFingerprint in fingerprintsInDatabaseThatMatchCaptureFingerprint) {
                //If result already contains a collection of delta times and counts for the database fingerprint's episode ID, use it, else create a new one and add it to the result
                if (episodeFingerprintMatchesToUpdate.ContainsKey(matchingDatabaseFingerprint.EpisodeId)) {
                    sampleTimeDeltaCounts = episodeFingerprintMatchesToUpdate[matchingDatabaseFingerprint.EpisodeId];
                } else { 
                    sampleTimeDeltaCounts = new Dictionary<long, int>();
                    episodeFingerprintMatchesToUpdate.Add(matchingDatabaseFingerprint.EpisodeId, sampleTimeDeltaCounts);
                }

                //Increment one more entry at the same delta time between sample times, initializing a new one if the delta time hasn't been seen before
                var deltaSampleTime = (int) (matchingDatabaseFingerprint.SampleTime - captureFingerprint.SampleTime);
                if (deltaSampleTime >= 0) {
                    if (sampleTimeDeltaCounts.ContainsKey(deltaSampleTime)) {
                        sampleTimeDeltaCounts[deltaSampleTime]++;
                    } else {
                        sampleTimeDeltaCounts.Add(deltaSampleTime, 1);
                    }
                }
            }
        }

        void SaveToJson(string filePath, Dictionary<int, List<Fingerprint>> fingerprints) {
            using (StreamWriter file = File.CreateText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, fingerprints, typeof(Dictionary<int, List<Fingerprint>>));
            }
        }

    }
}
