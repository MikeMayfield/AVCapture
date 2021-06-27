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
    /// 
    /// The significant sampling logic has been highly refactored to be similar to the approach taken in https://github.com/methi1999/Findit
    /// </summary>
    class AudioFileFingerprinter
    {
        //int AUDIO_BUFFER_SIZE = 2048;  //Size of buffer for audio-only processing. 0 if audio+video
        const int AUDIO_BUFFER_SIZE = 44100;  //Size of buffer for audio-only processing. 0 if audio+video
        const int FFT_BUFFER_SIZE = 1024;  //Size of FFT buffer (power of 2)
        List<Sample> samples = new List<Sample>(10000);
        AVReader avReader = new AVReader();
        short[] fftBuffer = new short[FFT_BUFFER_SIZE];
        const int fftBufferLength = FFT_BUFFER_SIZE;
        const long sampleDurationTicks = AVReader.TICKS_PER_SECOND * FFT_BUFFER_SIZE / AUDIO_BUFFER_SIZE;


        /// <summary>
        /// Generate a sample for each audio segment, then use those to samples to generate fingerprints based on significant samples.
        /// </summary>
        /// <param name="filePath">Full path to the file to process</param>
        /// <param name="episodeId">Episode ID for the associated file</param>
        /// <param name="fingerprintHashes">List of all known fingerprints. Updated to include new fingerprints</param>
        public void GenerateFingerprintsForFile(string filePath, int episodeId, Dictionary<long, List<Fingerprint>> fingerprintHashes) {
            //Console.Clear();
            Console.WriteLine("Samples for File: {0} - {1}", episodeId, filePath);

            avReader.Open(filePath, AUDIO_BUFFER_SIZE);
            var frameBuffer = avReader.NextFrame();

            while (frameBuffer != null) {
                //Process the audio buffer, if provided
                if (frameBuffer.AudioBuffer != null) {
                    var frameBufferLength = frameBuffer.AudioBuffer.Length;
                    var sampleTimeTicks = frameBuffer.SampleTime;
                    for (var audioBufferOffset = 0; audioBufferOffset + fftBufferLength <= frameBufferLength; audioBufferOffset += fftBufferLength) {
                        //Generate sample for current frame
                        CopyAudioBufferToFftBuffer(frameBuffer.AudioBuffer, audioBufferOffset, fftBuffer);
                        samples.Add(new Sample(frameBuffer.AudioSampleRateHz, sampleTimeTicks, fftBuffer));
                        sampleTimeTicks += sampleDurationTicks;
                    }
                }

                frameBuffer = avReader.NextFrame();
            }

            avReader.Close();

            CreateFingerprintsFromSamples(episodeId, samples, fingerprintHashes);
        }

        void CopyAudioBufferToFftBuffer(short[] audioBuffer, int audioBufferOffset, short[] fftBuffer) {
            for (int i = 0; i < fftBuffer.Length; i++) {
                fftBuffer[i] = audioBuffer[audioBufferOffset + i];
            }
        }

        /// <summary>
        /// Given a list of audio samples, generate fingerprints for the samples that are "significant".
        /// </summary>
        /// <param name="episodeId"></param>
        /// <param name="samples"></param>
        /// <param name="fingerprintHashes"></param>
        void CreateFingerprintsFromSamples(int episodeId, List<Sample> samples, Dictionary<long, List<Fingerprint>> fingerprintHashes) {
            DiscardUnimportantBandsForAllSamples(samples);
            LogImportantSampleBands(samples);

            //Create list of significant samples that are above weighted average amplitude across all samples
            var significantSamples = OnlySignificantSamples(samples);

            //AddFingerprintsForAllSignificantSamples(episodeId, significantSamples, fingerprintHashes);
            AddFingerprintsForAllSignificantSamples(episodeId, significantSamples, fingerprintHashes);
        }

        private void DiscardUnimportantBandsForAllSamples(List<Sample> samples) {
            var WEIGHT = 2d;

            ////Compute average amplitude (RMS) across all samples and bands
            //var totalAmplitudeSquared = 0d;
            //var count = 0;
            //foreach (var sample in samples) {
            //    //Compute average amplitude across all bands
            //    foreach (var amplitudeAtFrequencyBand in sample.AmplitudeAtFrequencyBands) {
            //        totalAmplitudeSquared += amplitudeAtFrequencyBand * amplitudeAtFrequencyBand;
            //        count++;
            //    }
            //}
            //var avgAmplitude = Math.Sqrt(totalAmplitudeSquared / count);  //RMS average amplitude across all bands

            //Discard bands within each sample that are below weighted average amplitude
            AverageAmplitude(samples);
            //var amplitudeFloor = AmplitudeFloor(samples, 10);
            var amplitudeFloor = AverageAmplitude(samples) * WEIGHT;

            foreach (var sample in samples) {

                //If band's amplitude is below weighted average, ignore it
                for (var i = 0; i < sample.AmplitudeAtFrequencyBands.Length; i++) {
                    if (sample.AmplitudeAtFrequencyBands[i] < amplitudeFloor) {
                        sample.AmplitudeAtFrequencyBands[i] = 0d;  //Flag: Ignore this amplitude as non-significant
                    }
                }
            }
        }

        private double AmplitudeFloor(List<Sample> samples, int desiredSignificantSamplesPerSecond) {
            var durationTicks = samples[samples.Count - 1].SampleTimeTicks;
            var desiredBinCnt = (int)((long)desiredSignificantSamplesPerSecond * durationTicks / 10000000L);
            var goalAmplitude = AverageAmplitude(samples);
            var binCntAtGoalAmplitude = Int32.MaxValue;

            while (binCntAtGoalAmplitude > desiredBinCnt) {
                binCntAtGoalAmplitude = 0;
                foreach (var sample in samples) {
                    for (var idx = 0; idx < sample.AmplitudeAtFrequencyBands.Length; idx++) {
                        if (sample.AmplitudeAtFrequencyBands[idx] >= goalAmplitude) {
                            binCntAtGoalAmplitude++;
                        }
                    }
                    if (binCntAtGoalAmplitude > desiredBinCnt) {
                        break;
                    }
                }

                if (binCntAtGoalAmplitude > desiredBinCnt) {
                    goalAmplitude = goalAmplitude * 1.1d;
                }
            }

            return goalAmplitude;
        }

        private double AverageAmplitude(List<Sample> samples) {
            //Compute average amplitude (RMS) across all samples and bands
            var totalAmplitudeSquared = 0d;
            var count = 0;
            foreach (var sample in samples) {
                //Compute average amplitude across all bands
                foreach (var amplitudeAtFrequencyBand in sample.AmplitudeAtFrequencyBands) {
                    totalAmplitudeSquared += amplitudeAtFrequencyBand * amplitudeAtFrequencyBand;
                    count++;
                }
            }
            var avgAmplitude = Math.Sqrt(totalAmplitudeSquared / count);  //RMS average amplitude across all bands
            return avgAmplitude;
        }

        private void LogImportantSampleBands(List<Sample> samples) {
            if (false) {  //TODO
                Console.Clear();
                Console.WriteLine("Samples: (SampleTimeMs, Freq, Amplitude)");
                var maxLines = 10000;
                foreach (var sample in samples) {
                    for (var i = 0; i < sample.AmplitudeAtFrequencyBands.Length; i++) {
                        if (sample.AmplitudeAtFrequencyBands[i] > 0d) {
                            Console.WriteLine("{0}\t{1}\t{2}", sample.SampleTimeTicks / 10000, Sample.BandFrequencies[i], (int) sample.AmplitudeAtFrequencyBands[i]);
                            if (--maxLines <= 0) {
                                break;
                            }
                        }
                    }
                    if (maxLines <= 0) {
                        break;
                    }
                }
            }
        }

        private void AddFingerprintsForAllSignificantSamples(int episodeId, List<SignificantSample> significantSamples, Dictionary<long, List<Fingerprint>> fingerprintHashes) {
            //const int RELATED_SAMPLE_CNT = 10;  //Number of following samples to combine with root sample to create a fingerprint for a sample point
            const int RELATED_SAMPLE_CNT = 10;  //Number of following samples to combine with root sample to create a fingerprint for a sample point  //TODO
            var significantSampleCount = significantSamples.Count;
            List<Fingerprint> fingerprintListForHash;

            for (var significantSampleIdx = 0; significantSampleIdx < significantSampleCount; significantSampleIdx++) {
                var rootSample = significantSamples[significantSampleIdx];
                var relatedSampleIdx = significantSampleIdx + 1;
                var rootSampleFreq = rootSample.Frequency;
                var startingTime = rootSample.SampleTimeTicks + AVReader.TICKS_PER_SECOND;
                var endingTime = rootSample.SampleTimeTicks + AVReader.TICKS_PER_SECOND * 10;

                for (var relatedSampleCnt = 0; relatedSampleCnt < RELATED_SAMPLE_CNT;) {
                    if (relatedSampleIdx >= significantSampleCount) {
                        break;
                    }

                    var relatedSample = significantSamples[relatedSampleIdx++];
                    if (relatedSample.SampleTimeTicks >= startingTime && relatedSample.SampleTimeTicks < endingTime) {
                        var fingerprint = new Fingerprint(episodeId, rootSample, relatedSample);
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

        //double[] ComputeSignificantAmplitudeByBand(List<Sample> samples) {
        //    var WEIGHT = 1.0d;
        //    var bucketCnt = samples[0].AmplitudeAtFrequencyBands.Length;
        //    double[] result = new double[bucketCnt];

        //    if (bucketCnt > 0) {
        //        double[] averageAmplitudeByBucket = AverageAmplitudeByBucket(samples);
        //        double[] standardDeviationByBucket = StandardDeviationByBucket(samples, averageAmplitudeByBucket);

        //        for (int i = 0; i < bucketCnt; i++) {
        //            result[i] = averageAmplitudeByBucket[i] + standardDeviationByBucket[i] * WEIGHT;
        //        }
        //    }

        //    return result;
        //}

        //List<SignificantSample> OnlySignificantSamples(List<Sample> allSamples, double[] significantAmplitude) {
        //    List<SignificantSample> newList = new List<SignificantSample>(allSamples.Count / 100);
        //    foreach (var sample in allSamples) {
        //        for (var i = 0; i < significantAmplitude.Length; i++) {
        //            if (sample.MaxAmplitudeAtFrequencyBands[i] != 0 && sample.MaxAmplitudeAtFrequencyBands[i] >= significantAmplitude[i]) {
        //                newList.Add(new SignificantSample(sample.SampleTime, Sample.BandFrequencies[i], sample.MaxAmplitudeAtFrequencyBands[i]));
        //            }
        //        }
        //    }

        //    return newList;
        //}

        List<SignificantSample> OnlySignificantSamples(List<Sample> allSamples) {
            List<SignificantSample> newList = new List<SignificantSample>(allSamples.Count);
            foreach (var sample in allSamples) {
                for (var i = 0; i < sample.AmplitudeAtFrequencyBands.Length; i++) {
                    if (sample.AmplitudeAtFrequencyBands[i] > 0) {
                        newList.Add(new SignificantSample(sample.SampleTimeTicks, Sample.BandFrequencies[i], sample.AmplitudeAtFrequencyBands[i]));
                    }
                }
            }

            return newList;
        }

        //double[] AverageAmplitudeByBucket(List<Sample> sampleList) {
        //    var bucketCnt = sampleList[0].AmplitudeAtFrequencyBands.Length;
        //    var result = new double[bucketCnt];
        //    var sumAtBand = new double[bucketCnt];

        //    if (bucketCnt > 0) {
        //        //Init all counters
        //        for (var i = 0; i < bucketCnt; i++) {
        //            sumAtBand[i] = 0d;
        //        }

        //        //Compute sum of all amplitudes for each bucket
        //        foreach (var sample in sampleList) {
        //            for (var i = 0; i < bucketCnt; i++) {
        //                sumAtBand[i] += sample.AmplitudeAtFrequencyBands[i];
        //            }
        //        }

        //        for (var i = 0; i < bucketCnt; i++) {
        //            result[i] = sumAtBand[i] / sampleList.Count;
        //        }
        //    }

        //    return result;
        //}

        //double[] StandardDeviationByBucket(List<Sample> sampleList, double[] averageAmplitudeByBucket) {
        //    var bucketCnt = sampleList[0].AmplitudeAtFrequencyBands.Length;
        //    var result = new double[bucketCnt];
        //    var sumAtBand = new double[bucketCnt];
        //    var sumSquaredDeltaAmplitudeAtBand = new double[bucketCnt];

        //    //Init all counters
        //    for (var i = 0; i < bucketCnt; i++) {
        //        sumAtBand[i] = 0d;
        //        sumSquaredDeltaAmplitudeAtBand[i] = 0d;
        //    }

        //    //Compute standard deviation for each bucket
        //    foreach (var sample in sampleList) {
        //        for (var i = 0; i < bucketCnt; i++) {
        //            var delta = sample.AmplitudeAtFrequencyBands[i] - averageAmplitudeByBucket[i];
        //            sumSquaredDeltaAmplitudeAtBand[i] += delta * delta;
        //        }
        //    }
        //    for (var i = 0; i < bucketCnt; i++) {
        //        result[i] = Math.Sqrt(sumSquaredDeltaAmplitudeAtBand[i] / sampleList.Count);
        //    }

        //    return result;
        //}
    }
}
