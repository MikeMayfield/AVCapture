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
        int AUDIO_BUFFER_SIZE = 512;  //2048;  //Size of buffer for audio-only processing. 0 if audio+video
        List<Sample> samples = new List<Sample>(10000);
        AVReader avReader = new AVReader();

        /// <summary>
        /// Generate a sample for each audio segment. Record the frequency with the maximum amplitude and the sample time
        /// </summary>
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
            const int RELATED_SAMPLE_CNT = 5;  //Number of following samples to combine with root sample to create a fingerprint for a sample point
            List<Fingerprint> fingerprintListForHash;

            //Determine the amplitude that is 2 standard deviations above the average across all samples
            double averageAmplitude = AverageAmplitude(samples);
            double standardDeviation = Deviation(samples, averageAmplitude);
            int significantAmplitude = (int) (averageAmplitude + standardDeviation * 3);
            List<Sample> significantSamples = OnlySignificantSamples(samples, significantAmplitude);
            int significantSampleCount = significantSamples.Count;

            //Log info on all samples considered significant
            if (true) {
                var maxSamples = 5000;
                Console.WriteLine($"Samples for episode ID: {episodeId}");
                foreach (var sample in samples) {
                    Console.WriteLine("{0}\t{1}\t{2}", ((double) sample.SampleTime) / 10000000.0d, sample.Frequency, sample.Amplitude);
                    if (--maxSamples < 0)
                        break;
                }

                //Console.WriteLine($"Significant samples for episode ID: {episodeId}");
                //foreach (var sample in significantSamples) {
                //    Console.WriteLine("{0}\t{1}\t{2}", ((double) sample.SampleTime) / 10000000.0d, sample.Frequency, sample.Amplitude);
                //}
            }

            for (var sampleIdx = 0; sampleIdx < significantSampleCount; sampleIdx++) {
                var rootSample = significantSamples[sampleIdx];
                var relatedSampleIdx = sampleIdx + 1;

                for (var relatedSampleCnt = 0; relatedSampleCnt < RELATED_SAMPLE_CNT; ) {
                    if (relatedSampleIdx >= significantSampleCount) {
                        break;
                    }

                    var relatedSample = significantSamples[relatedSampleIdx++];
                    var fingerprint = new Fingerprint(episodeId, rootSample, relatedSample);
                    if (fingerprint.Offset >= 000_000_0) {  //Ignore samples within 1/2 second of each other
                        var fingerprintHash = fingerprint.Hash;
                        if (fingerprintHashes.ContainsKey(fingerprintHash)) {
                            fingerprintListForHash = fingerprintHashes[fingerprintHash];
                        } else {
                            fingerprintListForHash = new List<Fingerprint>(1);
                            fingerprintHashes.Add(fingerprintHash, fingerprintListForHash);
                        }
                        fingerprintListForHash.Add(fingerprint);
                        relatedSampleCnt++;
                    }
                }
            }
        }

        List<Sample> OnlySignificantSamples(List<Sample> allSamples, int significantAmplitude) {
            List<Sample> newList = new List<Sample>(allSamples.Count / 100);
            foreach (var sample in allSamples) {
                if (sample.Frequency != 0 && sample.Amplitude >= significantAmplitude) {
                    newList.Add(sample);
                }
            }

            return newList;

            //List<Sample> ignoringAdjacentSamples = new List<Sample>(newList.Count);
            //long priorSampleTime = 0;
            //foreach (var sample in newList) {
            //    if (sample.SampleTime - priorSampleTime > 2000000) {
            //        ignoringAdjacentSamples.Add(sample);
            //        priorSampleTime = sample.SampleTime;
            //    }
            //}

            //return ignoringAdjacentSamples;
        }

        double AverageAmplitude(List<Sample> sampleList) {
            double sum = 0d;
            int cnt = 0;
            foreach (var sample in sampleList) {
                if (sample.Amplitude > 0) {
                    sum += sample.Amplitude;
                    cnt++;
                }
            }
            return sum / cnt;
        }

        double Deviation(List<Sample> sampleList, double average) {
            double sumDeltaSquared = 0d;
            int cnt = 0;
            foreach (var sample in sampleList) {
                if (sample.Amplitude > 0) {
                    var delta = sample.Amplitude - average;
                    sumDeltaSquared += (delta * delta);
                    cnt++;
                }
            }

            return Math.Sqrt(sumDeltaSquared / cnt);
        }
    }
}
