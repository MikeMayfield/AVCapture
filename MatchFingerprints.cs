﻿using Newtonsoft.Json;
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
        public MatchResult IdentifyEpisodeForAudioMatch(Dictionary<UInt32, FingerprintGroup> databaseHashes) {
            var episodeFingerprintMatches = new Dictionary<UInt32, Dictionary<Int64, UInt32>>();  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta
            System.GC.Collect();  //TODO REMOVE

            string path = Directory.GetCurrentDirectory() + "\\SampleVideo2_030-130.mp4";
            var fingerprinter = new AudioFileFingerprinter();
            var fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 20);
            var matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            path = Directory.GetCurrentDirectory() + "\\SampleVideo2_030-130.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 30);
            matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            path = Directory.GetCurrentDirectory() + "\\SampleVideo2_030-130.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 40);
            matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            path = Directory.GetCurrentDirectory() + "\\SampleVideo2_030-130.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 50);
            matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            path = Directory.GetCurrentDirectory() + "\\NCIS_0614_min1.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 60);
            matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            path = Directory.GetCurrentDirectory() + "\\NCIS_0614_min2.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 45);
            matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            path = Directory.GetCurrentDirectory() + "\\NCIS_0614_min3.mp4";
            fingerprinter = new AudioFileFingerprinter();
            fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 60);
            matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
                matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            //path = Directory.GetCurrentDirectory() + "\\TestFiles\\39657384-c174-46e8-abcd-7357a8b12ea3.mp4";
            //fingerprinter = new AudioFileFingerprinter();
            //fingerprintsForCaptureFile = new Dictionary<UInt32, FingerprintGroup>();
            //fingerprinter.GenerateFingerprintsForFile(path, 0, fingerprintsForCaptureFile, 60);
            //matchedEpisode = GetEpisodeMatchForCapture(databaseHashes, fingerprintsForCaptureFile);
            //Console.WriteLine("Finished, matching episode ID: {0} with {1}:1 confidence at offset {2}",  //TODO REMOVE
            //    matchedEpisode.EpisodeId, matchedEpisode.ConfidenceRatio, (double) matchedEpisode.SampleTimeDeltaTicks / 10000000d);

            return matchedEpisode;
        }

        /// <summary>
        /// Identify matching episode within database for fingerprints from a captured episode
        /// </summary>
        /// <param name="databaseHashes"> Hashed fingerprints from all episode in database </param>
        /// <param name="fingerprintsForCaptureFile"> Hashed fingerprints from episode to identify </param>
        /// <returns> Information about potential matching episode </returns>
        private MatchResult GetEpisodeMatchForCapture(Dictionary<UInt32, FingerprintGroup> databaseHashes,
                Dictionary<UInt32, FingerprintGroup> fingerprintsForCaptureFile) {
            var episode_FingerprintHistogram = CreateFingerprintSampleTimeDeltaHistogram(databaseHashes, fingerprintsForCaptureFile);  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta

            return GetEpisodeIdForMatchingEpisode(episode_FingerprintHistogram);
        }

        /// <summary>
        /// Create a histogram of fingerprint matches, counting the number of fingerprint matches at the same relative offset from the starting time of the recording
        /// </summary>
        /// <param name="databaseHashes"> Hashed fingerprints from all episode in database </param>
        /// <param name="fingerprintsForCaptureFile"> Hashed fingerprints from episode to identify </param>
        /// <returns> Dictionary, were key is episode ID. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta </returns>
        private Dictionary<UInt32, Dictionary<Int64, UInt32>> CreateFingerprintSampleTimeDeltaHistogram(Dictionary<UInt32, FingerprintGroup> databaseHashes,
                Dictionary<UInt32, FingerprintGroup> fingerprintsForCaptureFile) {
            var episode_FingerprintHistogram = new Dictionary<UInt32, Dictionary<Int64, UInt32>>();  //Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta

            //Create histogram of match count at the offset between fingerprint in database and fingerprint in capture file for each episode 
            foreach (var captureHash_fingerprints in fingerprintsForCaptureFile) {
                var captureFingerprintHash = captureHash_fingerprints.Key;
                var captureFingerprintsWithSameHash = captureHash_fingerprints.Value;

                //For each fingerprint for a given hash in the capture file, increment usage count for each matching hash in database, 
                //  generating a histogram of usage counts at different delta times between fingerprint sample times
                foreach (var fingerprintForCaptureHash in captureFingerprintsWithSameHash.Fingerprints) {
                    if (databaseHashes.ContainsKey(fingerprintForCaptureHash.Hash)) {
                        UpdateDeltaTimeHistogramForEachMatchingFingerprintInDatabase(fingerprintForCaptureHash, databaseHashes[fingerprintForCaptureHash.Hash], episode_FingerprintHistogram);
                    }
                }
            }

            return episode_FingerprintHistogram;
        }

        /// <summary>
        /// Given a fingerprint from the capture file, find all fingerprints in database for the same hash (ie. frequency difference and time difference between samples). 
        /// For each candidate fingerprint with the same hash, calculate the time difference between the capture sample and the database sample.
        /// Count number of occurances of the time difference within the episode associated with the database sample hash.
        /// </summary>
        /// <param name="fingerprintForCaptureHash"> Fingerprint from capture file to search for </param>
        /// <param name="fingerprintsInDatabaseThatMatchCaptureFingerprint"> Fingerprints in the database with the same hash as the capture fingerprint </param>
        /// <param name="episode_FingerprintHistogram"> Result collection to update: Dictionary: Key is episode ID. Value is dictionary where each entry's 
        ///     key is the timestamp delta between two samples and value is count of samples with the same timestamp delta</param>
        private void UpdateDeltaTimeHistogramForEachMatchingFingerprintInDatabase(Fingerprint fingerprintForCaptureHash, FingerprintGroup fingerprintsInDatabaseThatMatchCaptureFingerprint, 
                Dictionary<UInt32, Dictionary<Int64, UInt32>> episode_FingerprintHistogram) {
            Dictionary<Int64, UInt32> sampleTimeDelta_Counts;  //Key=Delta between capture fingerprint time and sample from database with the same hash. Value=Number of database samples with the corresponding delta time

            //Compare the capture fingerprint all fingerprints in the database
            foreach (var matchingDatabaseFingerprint in fingerprintsInDatabaseThatMatchCaptureFingerprint.Fingerprints) {
                //If result already contains a collection of delta times and counts for the database fingerprint's episode ID, use it, else create a new one and add it to the result
                if (episode_FingerprintHistogram.ContainsKey(matchingDatabaseFingerprint.EpisodeId)) {
                    sampleTimeDelta_Counts = episode_FingerprintHistogram[matchingDatabaseFingerprint.EpisodeId];
                } else {
                    sampleTimeDelta_Counts = new Dictionary<Int64, UInt32>();
                    episode_FingerprintHistogram.Add(matchingDatabaseFingerprint.EpisodeId, sampleTimeDelta_Counts);
                }

                //Increment one more entry at the same delta time between sample times, initializing a new one if the delta time hasn't been seen before
                var deltaSampleTime = (Int64)matchingDatabaseFingerprint.SampleTimeTicks - (Int64)fingerprintForCaptureHash.SampleTimeTicks;
                if (sampleTimeDelta_Counts.ContainsKey(deltaSampleTime)) {
                    sampleTimeDelta_Counts[deltaSampleTime]++;
                } else {
                    sampleTimeDelta_Counts.Add(deltaSampleTime, 1);
                }
            }
        }

        /// <summary>
        /// Get matching Episode ID, Match count, and Match time delta, and ConfidenceRatio, based on histogram with high spike of matches at the same sample delta times
        /// </summary>
        /// <param name="episode_FingerprintHistogram"> //Dictionary: Key is fingerprint hash. Value is dictionary where each entry's key is the timestamp delta between two samples and value is count of samples with the same timestamp delta </param>
        /// <returns> Match result info </returns>
        private MatchResult GetEpisodeIdForMatchingEpisode(Dictionary<UInt32, Dictionary<Int64, UInt32>> episode_FingerprintHistogram) {
            UInt32 matchingEpisodeId = 0;
            UInt32 maxHashMatchCnt = 0;
            Int64 matchingSampleTimeDelta = 0;
            UInt32 almostMaxHashMatchCnt = 0;
            UInt32 almostEpisodeId = 0;
            const Int64 DELTA_TICKS = 30_000_0;

            foreach (var episodeId_sampleTimeDelta_MatchCount in episode_FingerprintHistogram) {
                var episodeId = episodeId_sampleTimeDelta_MatchCount.Key;
                var episodeHistogram = episodeId_sampleTimeDelta_MatchCount.Value;
                UInt32 highestMatchCountInEpisode = 0;
                Int64 sampleDeltaTimeAtHighestMatchCount = 0;

                //Find max match count within episode
                foreach (var sampleTimeDelta_MatchCount in episodeHistogram) {
                    var currentSampleTimeDelta = sampleTimeDelta_MatchCount.Key;
                    var currentMatchCount = sampleTimeDelta_MatchCount.Value;
                    
                    if (currentMatchCount > highestMatchCountInEpisode) {
                        highestMatchCountInEpisode = currentMatchCount;
                        sampleDeltaTimeAtHighestMatchCount = currentSampleTimeDelta;
                    }
                }

                //Add matches within +/- DELTA_TICKS seconds of max as close matches
                var minSampleTimeDelta = sampleDeltaTimeAtHighestMatchCount - DELTA_TICKS;
                var maxSampleTimeDelta = sampleDeltaTimeAtHighestMatchCount + DELTA_TICKS;
                UInt32 countNearPeak = 0;
                int cnt = 0;
                foreach (var sampleTimeDelta_MatchCount in episodeHistogram) {
                    var currentSampleTimeDelta = sampleTimeDelta_MatchCount.Key;
                    var currentMatchCount = sampleTimeDelta_MatchCount.Value;

                    if (currentSampleTimeDelta >= minSampleTimeDelta && currentSampleTimeDelta <= maxSampleTimeDelta) {
                        countNearPeak += currentMatchCount;
                        cnt++;
                    }
                }
                highestMatchCountInEpisode = countNearPeak;

                if (highestMatchCountInEpisode > maxHashMatchCnt) {
                    maxHashMatchCnt = highestMatchCountInEpisode;
                    matchingEpisodeId = episodeId;
                    matchingSampleTimeDelta = sampleDeltaTimeAtHighestMatchCount;
                } else if (highestMatchCountInEpisode > almostMaxHashMatchCnt) {
                    almostMaxHashMatchCnt = highestMatchCountInEpisode;
                    almostEpisodeId = episodeId;
                }
            }

            var confidenceRatio = (almostMaxHashMatchCnt != 0) ?
                (double)maxHashMatchCnt / (double)almostMaxHashMatchCnt :
                maxHashMatchCnt;

            return new MatchResult(matchingEpisodeId, matchingSampleTimeDelta, maxHashMatchCnt, confidenceRatio);
        }

        void SaveToJson(string filePath, Dictionary<UInt32, FingerprintGroup> fingerprints) {
            using (StreamWriter file = File.CreateText(filePath)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, fingerprints, typeof(Dictionary<UInt32, FingerprintGroup>));
            }
        }

    }
}
