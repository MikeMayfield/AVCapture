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
        /// <returns>Episode ID matching the list of of fingerprints; 0 if no reliable match</returns>
        public UInt64 IdentifyEpisodeForAudioMatch(Dictionary<UInt64, FingerprintGroup> databaseHashes) {
            var episodeFingerprintMatches = new Dictionary<UInt64, Dictionary<UInt64, UInt32>>();  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta

            //Generate fingerprints for audio capture  //TODO: Use window into episode
            //string path = Directory.GetCurrentDirectory() + "\\SampleVideo2Capture2.mp4";
            string path = Directory.GetCurrentDirectory() + "\\SampleVideo2_030-130.mp4";
            //string path = Directory.GetCurrentDirectory() + "\\SampleVideo2.mp4";
            var fingerprinter = new AudioFileFingerprinter();
            var fingerprintsForCaptureFile = new Dictionary<UInt64, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, false);
            //SaveToJson(Directory.GetCurrentDirectory() + "\\MatchFingerprints.json", fingerprintsForCaptureFile);

            var matchId_Count = new Dictionary<UInt64, UInt32>();  //Episode IDs and their related match counts
            var subFingerprintsForCaptureFile = new Dictionary<UInt64, FingerprintGroup>(1000);  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta
            var listFingerprintsForCaptureFile = fingerprintsForCaptureFile.ToList();
            for (var sectionOffset = 0; sectionOffset < fingerprintsForCaptureFile.Count; sectionOffset += 1000 /*25*/) {
                var sectionEnd = sectionOffset + 1000;
                subFingerprintsForCaptureFile.Clear();
                for (var idx = sectionOffset; idx < fingerprintsForCaptureFile.Count && idx < sectionEnd; idx++) {
                    subFingerprintsForCaptureFile.Add(listFingerprintsForCaptureFile[idx].Key, listFingerprintsForCaptureFile[idx].Value);
                }
                var matchingEpisodeId_MaxMatchCount = Temp(subFingerprintsForCaptureFile, databaseHashes);
                if (matchId_Count.ContainsKey(matchingEpisodeId_MaxMatchCount.Key)) {
                    matchId_Count[matchingEpisodeId_MaxMatchCount.Key] += matchingEpisodeId_MaxMatchCount.Value;
                } else {
                    matchId_Count.Add(matchingEpisodeId_MaxMatchCount.Key, matchingEpisodeId_MaxMatchCount.Value);
                }
            }

            return GetEpisodeIdForMostSignificantMatch(matchId_Count);  //TODO Return confidence ratio as well as ID
        }

        private UInt64 GetEpisodeIdForMostSignificantMatch(Dictionary<UInt64, UInt32> matchId_Count) {
            const UInt32 MINIMUM_ACCEPTABLE_CONFIDENCE_RATIO = 5;
            UInt64 maxEpisodeId= 0;
            UInt32 maxMatchCount = 0;
            UInt32 almostMatchCount = 0;

            foreach (var entry in matchId_Count) {
                var entryMatchCount = entry.Value;
                if (entryMatchCount > maxMatchCount) {
                    almostMatchCount = maxMatchCount;
                    maxMatchCount = entryMatchCount;
                    maxEpisodeId = entry.Key;
                } else if (entry.Value > almostMatchCount) {
                    almostMatchCount = entryMatchCount;
                }
            }

            if (almostMatchCount == 0 || maxMatchCount / almostMatchCount >= MINIMUM_ACCEPTABLE_CONFIDENCE_RATIO) {
                return maxEpisodeId;
            } else {
                return 0;
            }
        }

        private KeyValuePair<UInt64, UInt32> Temp(Dictionary<UInt64, FingerprintGroup> subFingerprintsForCaptureFile, 
                Dictionary<UInt64, FingerprintGroup> databaseHashes) {
            var episodeFingerprintMatches = new Dictionary<UInt64, Dictionary<UInt64, UInt32>>();  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta

            foreach (var hashGroup in subFingerprintsForCaptureFile) {
                var fingerprintHash = hashGroup.Key;
                var fingerprintsWithSameHash = hashGroup.Value;
                foreach (var captureFingerprint in fingerprintsWithSameHash.Fingerprints) {
                    if (databaseHashes.ContainsKey(captureFingerprint.Hash)) {
                        ProcessMatchingHash(captureFingerprint, databaseHashes[captureFingerprint.Hash], episodeFingerprintMatches);
                    }
                }
            }

            return GetEpisodeIdForMatchingEpisode(episodeFingerprintMatches);
        }

        /// <summary>
        /// Given a fingerprint from the capture file, find all fingerprints in database for the same hash (ie. frequency difference and time difference between samples). 
        /// For each candidate fingerprint with the same hash, calculate the time difference between the capture sample and the database sample.
        /// Count number of occurances of the time difference within the episode associated with the database sample hash.
        /// </summary>
        /// <param name="captureFingerprint"> Fingerprint from capture file to search for </param>
        /// <param name="fingerprintsInDatabaseThatMatchCaptureFingerprint"> Fingerprints in the database with the same hash as the capture fingerprint </param>
        /// <param name="episodeFingerprintMatchesToUpdate"> Result collection to update: Dictionary: Key is episode ID. Value is dictionary where each entry's 
        ///     key is the timestamp delta between two samples and value is count of samples with the same timestamp delta</param>
        private void ProcessMatchingHash(Fingerprint captureFingerprint, FingerprintGroup fingerprintsInDatabaseThatMatchCaptureFingerprint, 
                Dictionary<UInt64, Dictionary<UInt64, UInt32>> episodeFingerprintMatchesToUpdate) {
            Dictionary<UInt64, UInt32> sampleTimeDeltaCounts;  //Key=Delta between capture fingerprint time and sample from database with the same hash. Value=Number of database samples with the corresponding delta time

            //Compare the capture fingerprint all fingerprints in the database
            foreach (var matchingDatabaseFingerprint in fingerprintsInDatabaseThatMatchCaptureFingerprint.Fingerprints) {
                //If result already contains a collection of delta times and counts for the database fingerprint's episode ID, use it, else create a new one and add it to the result
                if (episodeFingerprintMatchesToUpdate.ContainsKey(matchingDatabaseFingerprint.EpisodeId)) {
                    sampleTimeDeltaCounts = episodeFingerprintMatchesToUpdate[matchingDatabaseFingerprint.EpisodeId];
                } else {
                    sampleTimeDeltaCounts = new Dictionary<UInt64, UInt32>();
                    episodeFingerprintMatchesToUpdate.Add(matchingDatabaseFingerprint.EpisodeId, sampleTimeDeltaCounts);
                }

                //Increment one more entry at the same delta time between sample times, initializing a new one if the delta time hasn't been seen before
                var deltaSampleTime = (UInt32)(matchingDatabaseFingerprint.SampleTimeTicks - captureFingerprint.SampleTimeTicks);
                if (deltaSampleTime >= 0) {
                    if (sampleTimeDeltaCounts.ContainsKey(deltaSampleTime)) {
                        sampleTimeDeltaCounts[deltaSampleTime]++;
                    } else {
                        sampleTimeDeltaCounts.Add(deltaSampleTime, 1);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="episodeFingerprintMatches"> //Dictionary: Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta </param>
        /// <returns></returns>
        private KeyValuePair<UInt64, UInt32> GetEpisodeIdForMatchingEpisode(Dictionary<UInt64, Dictionary<UInt64, UInt32>> episodeFingerprintMatches) {
            UInt64 matchingEpisodeId = 0L;
            UInt32 maxHashMatchCnt = 0;
            UInt64 matchHashOffset = 0;
            UInt32 almostMaxHashMatchCnt = 0;
            UInt64 almostMatchingEpisodeId = 0L;
            UInt64 almostHashOffset = 0L;

            //  . For each episode that had a matching hash with the capture file
            //  . . Find maximum delta time count for possibly matching hashes within the episode
            //  . . If count is > than prior best match
            //  . . . Remember new best episode match (and extra info for debugging)
            foreach (var episodeId_sampleTimeOffsetToMatchCount in episodeFingerprintMatches) {
                UInt32 maxSubmatchCnt = 0;
                UInt64 priorBinOffset = UInt64.MaxValue;
                UInt32 sumOfAdjacentBins = 0;  //TODO Don't process adjacent bins
                var episodeDictionary = episodeId_sampleTimeOffsetToMatchCount.Value.ToList();  //TODO Use episodeId_sampleTimeOffsetToMatchCount directly
                episodeDictionary.Sort((pair1, pair2) => {
                    return pair1.Key.CompareTo(pair2.Key);
                });
                //foreach (var countAtSampleTimeOffset in episodeId_sampleTimeOffsetToMatchCount.Value) {
                foreach (var kvp in episodeDictionary) {
                    var currentBinOffset = kvp.Key;
                    var currentBinValue = kvp.Value;
                    //if (priorBinOffset + COMBINE_WITHIN_TICKS > currentBinOffset) {
                        sumOfAdjacentBins += currentBinValue;
                    //} else {
                        if (sumOfAdjacentBins > maxHashMatchCnt) {
                            almostMaxHashMatchCnt = maxHashMatchCnt;
                            almostMatchingEpisodeId = matchingEpisodeId;
                            almostHashOffset = matchHashOffset;
                            maxHashMatchCnt = sumOfAdjacentBins;
                            matchingEpisodeId = episodeId_sampleTimeOffsetToMatchCount.Key;
                            matchHashOffset = currentBinOffset;
                        } else if (sumOfAdjacentBins > almostMaxHashMatchCnt) {
                            almostMaxHashMatchCnt = sumOfAdjacentBins;
                            almostMatchingEpisodeId = episodeId_sampleTimeOffsetToMatchCount.Key;
                            almostHashOffset = currentBinOffset;
                        }
                        if (sumOfAdjacentBins > maxSubmatchCnt) {
                            maxSubmatchCnt = sumOfAdjacentBins;
                        }

                        sumOfAdjacentBins = currentBinValue;
                    //}
                    priorBinOffset = currentBinOffset;
                }
            }

            return new KeyValuePair<UInt64, UInt32>(matchingEpisodeId, maxHashMatchCnt);
        }

        void SaveToJson(string filePath, Dictionary<UInt64, FingerprintGroup> fingerprints) {
            using (StreamWriter file = File.CreateText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, fingerprints, typeof(Dictionary<UInt64, FingerprintGroup>));
            }
        }

    }
}
