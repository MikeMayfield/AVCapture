using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    /// <summary>
    /// Generate fingerprints for an audio file
    /// </summary>
    class AudioFileFingerprinter
    {
        int AUDIO_BUFFER_SIZE = 4096;  //2048;  //Size of buffer for audio-only processing. 0 if audio+video
        List<Sample> samples = new List<Sample>(10000);
        AVReader avReader = new AVReader();

        /// <summary>
        /// Generate a sample for each audio segment, then use those to samples to generate fingerprints based on significant samples.
        /// </summary>
        /// <param name="filePath">Full path to the file to process</param>
        /// <param name="episodeId">Episode ID for the associated file</param>
        /// <param name="fingerprintHashes">List of all known fingerprints. Updated to include new fingerprints</param>
        public void GenerateFingerprintsForFile(string filePath, int episodeId, Dictionary<int, List<Fingerprint>> fingerprintHashes) {
            Console.WriteLine("Samples for File: {0} - {1}", episodeId, filePath);
            avReader.Open(filePath, AUDIO_BUFFER_SIZE);
            var frameBuffer = avReader.NextFrame();
            var sampleCount = 0;
            var sampleCountLimit = int.MaxValue;  //TODO 3000;

            while (frameBuffer != null) {
                if (sampleCount++ >= sampleCountLimit) { 
                    break;
                }

                //Process the audio buffer, if provided
                if (frameBuffer.AudioBuffer != null) {
                    if (frameBuffer.SampleTime > 1000000000)  //TODO REMOVE Limits to 80 seconds capture
                        break;
                    //Generate sample for current frame
                    samples.Add(new Sample(frameBuffer.AudioSampleRateHz, frameBuffer.SampleTime, frameBuffer.AudioBuffer));
                }

                frameBuffer = avReader.NextFrame();
            }

            avReader.Close();

            CreateFingerprintsFromSamples(episodeId, samples, fingerprintHashes);
        }

        /// <summary>
        /// Given a list of audio samples, generate fingerprints for the samples that are "significant".
        /// </summary>
        /// <param name="episodeId"></param>
        /// <param name="samples"></param>
        /// <param name="fingerprintHashes"></param>
        void CreateFingerprintsFromSamples(int episodeId, List<Sample> samples, Dictionary<int, List<Fingerprint>> fingerprintHashes) {
            double[] significantAmplitudeByBucket = ComputeSignificantAmplitudeByBucket(samples);

            //Create list of significant samples that are at least n standard deviations above the average across all samples
            List<SignificantSample> significantSamples = OnlySignificantSamples(samples, significantAmplitudeByBucket);
            LogAllSignificantSamples(episodeId, significantSamples);

            AddFingerprintsForAllSignificantSamples(episodeId, significantSamples, fingerprintHashes);
        }

        private void LogAllSignificantSamples(int episodeId, List<SignificantSample> significantSamples) {
            if (true) {  //TODO
                Console.WriteLine($"{significantSamples.Count} significant samples for episode ID: {episodeId} (SampleTimeMs, Freq)");
                var maxLines = 1000;
                foreach (var sample in significantSamples) {
                    Console.WriteLine("{0}\t{1}", ((double) sample.SampleTime) / 10000.0d, sample.Frequency);
                    if (--maxLines <= 0) {
                        break;
                    }
                }
            }
        }

        private void AddFingerprintsForAllSignificantSamples(int episodeId, List<SignificantSample> significantSamples, Dictionary<int, List<Fingerprint>> fingerprintHashes) {
            const int RELATED_SAMPLE_CNT = 10;  //Number of following samples to combine with root sample to create a fingerprint for a sample point
            var significantSampleCount = significantSamples.Count;
            List<Fingerprint> fingerprintListForHash;

            for (var sampleIdx = 0; sampleIdx < significantSampleCount; sampleIdx++) {
                var rootSample = significantSamples[sampleIdx];
                var relatedSampleIdx = sampleIdx + 1;

                for (var relatedSampleCnt = 0; relatedSampleCnt < RELATED_SAMPLE_CNT;) {
                    if (relatedSampleIdx >= significantSampleCount) {
                        break;
                    }

                    var relatedSample = significantSamples[relatedSampleIdx++];
                    if (rootSample.SampleTime != relatedSample.SampleTime) {
                        var fingerprint = new Fingerprint(episodeId, rootSample, relatedSample);
                        //if (fingerprint.Offset >= 1_000_000_0) {  //Ignore samples within 1 second of each other
                            var fingerprintHash = fingerprint.Hash;
                            if (fingerprintHashes.ContainsKey(fingerprintHash)) {
                                fingerprintListForHash = fingerprintHashes[fingerprintHash];
                            } else {
                                fingerprintListForHash = new List<Fingerprint>(1);
                                fingerprintHashes.Add(fingerprintHash, fingerprintListForHash);
                            }
                            fingerprintListForHash.Add(fingerprint);
                            relatedSampleCnt++;
                        //}
                    }
                }
            }

        }

        double[] ComputeSignificantAmplitudeByBucket(List<Sample> samples) {
            var WEIGHT = 1.0d;
            var bucketCnt = samples[0].AmplitudeAtFrequencyBand.Length;
            double[] result = new double[bucketCnt];

            if (bucketCnt > 0) {
                double[] averageAmplitudeByBucket = AverageAmplitudeByBucket(samples);
                double[] standardDeviationByBucket = StandardDeviationByBucket(samples, averageAmplitudeByBucket);

                for (int i = 0; i < bucketCnt; i++) {
                    result[i] = averageAmplitudeByBucket[i] + standardDeviationByBucket[i] * WEIGHT;
                }
            }

            return result;
        }

        List<SignificantSample> OnlySignificantSamples(List<Sample> allSamples, double[] significantAmplitude) {
            List<SignificantSample> newList = new List<SignificantSample>(allSamples.Count / 100);
            foreach (var sample in allSamples) {
                for (var i = 0; i < significantAmplitude.Length; i++) {
                    if (sample.AmplitudeAtFrequencyBand[i] != 0 && sample.AmplitudeAtFrequencyBand[i] >= significantAmplitude[i]) {
                        newList.Add(new SignificantSample(sample.SampleTime, Sample.BucketFrequencies[i], sample.AmplitudeAtFrequencyBand[i]));
                    }
                }
            }

            return newList;
        }

        double[] AverageAmplitudeByBucket(List<Sample> sampleList) {
            var bucketCnt = sampleList[0].AmplitudeAtFrequencyBand.Length;
            var result = new double[bucketCnt];
            var sumAtBand = new double[bucketCnt];

            if (bucketCnt > 0) {
                //Init all counters
                for (var i = 0; i < bucketCnt; i++) {
                    sumAtBand[i] = 0d;
                }

                //Compute sum of all amplitudes for each bucket
                foreach (var sample in sampleList) {
                    for (var i = 0; i < bucketCnt; i++) {
                        sumAtBand[i] += sample.AmplitudeAtFrequencyBand[i];
                    }
                }

                for (var i = 0; i < bucketCnt; i++) {
                    result[i] = sumAtBand[i] / sampleList.Count;
                }
            }

            return result;
        }

        double[] StandardDeviationByBucket(List<Sample> sampleList, double[] averageAmplitudeByBucket) {
            var bucketCnt = sampleList[0].AmplitudeAtFrequencyBand.Length;
            var result = new double[bucketCnt];
            var sumAtBand = new double[bucketCnt];
            var sumSquaredDeltaAmplitudeAtBand = new double[bucketCnt];

            //Init all counters
            for (var i = 0; i < bucketCnt; i++) {
                sumAtBand[i] = 0d;
                sumSquaredDeltaAmplitudeAtBand[i] = 0d;
            }

            //Compute standard deviation for each bucket
            foreach (var sample in sampleList) {
                for (var i = 0; i < bucketCnt; i++) {
                    var delta = sample.AmplitudeAtFrequencyBand[i] - averageAmplitudeByBucket[i];
                    sumSquaredDeltaAmplitudeAtBand[i] += delta * delta;
                }
            }
            for (var i = 0; i < bucketCnt; i++) {
                result[i] = Math.Sqrt(sumSquaredDeltaAmplitudeAtBand[i] / sampleList.Count);
            }

            return result;
        }
    }
}
