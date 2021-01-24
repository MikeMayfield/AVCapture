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
        int AUDIO_BUFFER_SIZE = 256;  //Size of buffer for audio-only processing. 0 if audio+video
        List<Sample> samples = new List<Sample>(10000);
        AVReader avReader = new AVReader();

        public void GenerateFingerprintsForFile(string filePath, int episodeId, Dictionary<int, List<Fingerprint>> fingerprintHashes) {
            Sample.Init();  //TODO Is this needed?
            avReader.Open(filePath, AUDIO_BUFFER_SIZE);
            var frameBuffer = avReader.NextFrame();
            var todo = 0;
            var maxTodo = 5000;

            while (frameBuffer != null && todo++ < maxTodo) {
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

        void CreateFingerprintsFromSamples(int episodeId, List<Sample> samples, Dictionary<int, List<Fingerprint>> fingerprintHashes) {
            List<Fingerprint> fingerprintListForHash;
            double averageAmplitude = AverageAmplitude(samples);
            double standardDeviation = Deviation(samples, averageAmplitude);
            //int significantAmplitude = (int) (averageAmplitude + standardDeviation + standardDeviation);  //TODO
            int significantAmplitude = (int) (averageAmplitude + standardDeviation);
            List<Sample> significantSamples = OnlySignificantSamples(samples, significantAmplitude);
            int sampleCount = significantSamples.Count;

            Console.WriteLine("Samples for File: {0}", episodeId);
            //var s = significantSamples[0];
            foreach (var sample in samples) {
                Console.WriteLine("{0}\t{1}\t{2}", sample.SampleTime, sample.Amplitude, sample.Frequency);
                //s = sample;
            }

            for (var sampleIdx = 0; sampleIdx < sampleCount; sampleIdx++) {
                var sample1 = significantSamples[sampleIdx];
                var relatedSampleIdx = sampleIdx + 1;
                for (var relatedSampleCnt = 0; relatedSampleCnt < 10 ; ) {
                    if (relatedSampleIdx >= sampleCount) {
                        break;
                    }
                    var sample2 = significantSamples[relatedSampleIdx++];
                    var fingerprint = new Fingerprint(episodeId, sample1, sample2);
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
            foreach (var sample in sampleList) {
                sum += sample.Amplitude;
            }
            return sum / sampleList.Count;
        }

        double Deviation(List<Sample> sampleList, double average) {
            double sumDeltaSquared = 0d;
            foreach (var sample in sampleList) {
                var delta = sample.Amplitude - average;
                sumDeltaSquared += (delta * delta);
            }

            return Math.Sqrt(sumDeltaSquared / sampleList.Count);
        }
    }
}
