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
        /// <param name="databaseHashes">Dictionary: Key=Hash of anchorFreq, targetFreq, offsetTime, Value=List of hash fingerprints</param>
        /// <returns></returns>
        public int IdentifyEpisodeForAudioMatch(Dictionary<int, List<Fingerprint>> databaseHashes) {
            var episodeFingerprintMatches = new Dictionary<int, Dictionary<long, int>>();  //Key by hash: Value is dictionary of counts by delta-time key

            //Generate fingerprints for audio capture  //TODO: Use window into episode
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo2Capture2.mp4";
            //string path = Directory.GetCurrentDirectory() + "\\440HzSine.mp4";
            var fingerprinter = new AudioFileFingerprinter();
            var realtimeMatchHashes = new Dictionary<int, List<Fingerprint>>(1000);
            fingerprinter.GenerateFingerprintsForFile(path, -1, realtimeMatchHashes);
            //At this point, matchHashes contains hashes for all significant matches in realtime constellation

            //Count number of matching hashes at the same sample time delta for each possibly matching episode
            foreach (var hashGroup in realtimeMatchHashes) {
                var hash = hashGroup.Key;
                var realtimeFingerprintList = hashGroup.Value;
                foreach (var realtimeFingerprint in realtimeFingerprintList) {
                    if (databaseHashes.ContainsKey(realtimeFingerprint.Hash)) {
                        ProcessMatchingHash(realtimeFingerprint, databaseHashes[realtimeFingerprint.Hash], episodeFingerprintMatches);
                    }
                }
            }

            //Find episode with most hash matches
            var matchingEpisodeId = 0;
            var maxHashMatchCnt = 0;
            var almostMaxHashMatchCnt = 0;
            var almostMatchingEpisodeId = 0;
            foreach (var episodeFingerprintMatch in episodeFingerprintMatches) {
                var maxSubmatchCnt = 0;
                foreach (var deltaTimeCount in episodeFingerprintMatch.Value) {
                    if (deltaTimeCount.Value > maxHashMatchCnt) {
                        almostMaxHashMatchCnt = maxHashMatchCnt;
                        almostMatchingEpisodeId = matchingEpisodeId;
                        maxHashMatchCnt = deltaTimeCount.Value;
                        matchingEpisodeId = episodeFingerprintMatch.Key;
                    } else if (deltaTimeCount.Value > almostMaxHashMatchCnt) {
                        almostMaxHashMatchCnt = deltaTimeCount.Value;
                        almostMatchingEpisodeId = episodeFingerprintMatch.Key;
                    }
                    if (deltaTimeCount.Value > maxSubmatchCnt) {
                        maxSubmatchCnt = deltaTimeCount.Value;
                    }
                }
            }

            SaveToJson(Directory.GetCurrentDirectory() + "\\MatchFingerprints.json", realtimeMatchHashes);

            return (maxHashMatchCnt > 10 && (maxHashMatchCnt - almostMaxHashMatchCnt > 10 || matchingEpisodeId == almostMatchingEpisodeId)) ? matchingEpisodeId : 0;
        }

        private void ProcessMatchingHash(Fingerprint realtimeFingerprint, List<Fingerprint> matchingDatabaseFingerprints, Dictionary<int, Dictionary<long, int>> episodeFingerprintMatches) {
            Dictionary<long, int> sampleTimeDeltaCounts;

            foreach (var matchingDatabaseFingerprint in matchingDatabaseFingerprints) {
                if (episodeFingerprintMatches.ContainsKey(matchingDatabaseFingerprint.EpisodeId)) {
                    sampleTimeDeltaCounts = episodeFingerprintMatches[matchingDatabaseFingerprint.EpisodeId];
                } else { 
                    sampleTimeDeltaCounts = new Dictionary<long, int>();
                    episodeFingerprintMatches.Add(matchingDatabaseFingerprint.EpisodeId, sampleTimeDeltaCounts);
                }

                var deltaSampleTime = (int) (matchingDatabaseFingerprint.SampleTime - realtimeFingerprint.SampleTime);
                if (sampleTimeDeltaCounts.ContainsKey(deltaSampleTime)) {
                    sampleTimeDeltaCounts[deltaSampleTime]++;
                } else {
                    sampleTimeDeltaCounts.Add(deltaSampleTime, 1);
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
