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
    class AudioFileFingerprinter {
        const int AUDIO_BUFFER_SIZE = 44100;  //Size of buffer (words) for audio-only processing. 0 if audio+video
        const int FFT_BUFFER_SIZE = 1024;  //Size of FFT buffer (power of 2)
        List<Sample> samples = new List<Sample>(10000);
        AVReader avReader = new AVReader();
        short[] fftBuffer = new short[FFT_BUFFER_SIZE];
        const UInt64 SAMPLE_DURATION_TICKS = AVReader.TICKS_PER_SECOND * FFT_BUFFER_SIZE / AUDIO_BUFFER_SIZE;
        private SampleFFT sampleFFT;

        /// <summary>
        /// Generate a sample for each audio segment, then use those to samples to generate fingerprints based on significant samples.
        /// </summary>
        /// <param name="filePath">Full path to the file to process</param>
        /// <param name="episodeId">Episode ID for the associated file</param>
        /// <param name="fingerprintHashes">List of all known fingerprints. Updated to include new fingerprints</param>
        public void GenerateFingerprintsForFile(string filePath, UInt32 episodeId, Dictionary<UInt32, FingerprintGroup> fingerprintHashes, int secondsToCapture = 20000) {
            var upperTimeLimitTicks = AVReader.TICKS_PER_SECOND * (UInt64)secondsToCapture;

            //Console.Clear();
            Console.WriteLine("Samples for file: {0} - {1}", episodeId, filePath);

            try {
                avReader.Open(filePath, AUDIO_BUFFER_SIZE);
            } catch (Exception ex) {
                Console.WriteLine("**ERROR: Failed to open file '{0}' with error: {1}",
                    filePath, ex.Message);
                return;
            }

            var frameBuffer = avReader.NextFrame();
            sampleFFT = new SampleFFT(frameBuffer.AudioSampleRateHz, FFT_BUFFER_SIZE);
            var sampleTimeTicks = frameBuffer.SampleTime;
            var fftBufferOffset = 0;
            var fftBufferBytesToFill = FFT_BUFFER_SIZE;

            while (frameBuffer != null) {
                //Process the audio buffer, if provided
                if (frameBuffer.AudioBuffer != null) {
                    var frameBufferLength = frameBuffer.AudioBuffer.Length;
                    var frameBufferOffset = 0;
                    for ( ; frameBufferOffset + FFT_BUFFER_SIZE <= frameBufferLength; frameBufferOffset += FFT_BUFFER_SIZE) {
                        //Generate sample for current frame
                        CopyAudioBufferToFftBuffer(frameBuffer.AudioBuffer, frameBufferOffset, fftBuffer, fftBufferOffset, fftBufferBytesToFill);
                        samples.Add(new Sample(sampleTimeTicks, fftBuffer, sampleFFT));
                        fftBufferOffset = 0;
                        fftBufferBytesToFill = FFT_BUFFER_SIZE;
                        sampleTimeTicks += SAMPLE_DURATION_TICKS;
                    }

                    //After processing all full FFT buffers from input buffer, preload next FFT buffer with remainder of input buffer
                    var leftOverWordCnt = frameBufferLength - frameBufferOffset;
                    CopyAudioBufferToFftBuffer(frameBuffer.AudioBuffer, frameBufferOffset, fftBuffer, fftBufferOffset, leftOverWordCnt);
                    fftBufferOffset = leftOverWordCnt;
                    fftBufferBytesToFill = FFT_BUFFER_SIZE - leftOverWordCnt;
                }

                frameBuffer = avReader.NextFrame();
                if (frameBuffer != null && frameBuffer.SampleTime > upperTimeLimitTicks) {
                    frameBuffer = null;
                }
            }

            avReader.Close();

            CreateFingerprintsFromSamples(episodeId, samples, fingerprintHashes);
        }

        void CopyAudioBufferToFftBuffer(short[] audioBuffer, int audioBufferOffset, short[] fftBuffer, int fftBufferOffset = 0, int transferCnt = 1024) {
            for (var i = 0; i < transferCnt; i++) {
                fftBuffer[fftBufferOffset++] = audioBuffer[audioBufferOffset++];
            }
        }

        /// <summary>
        /// Given a list of audio samples, generate fingerprints for the samples that are "significant".
        /// </summary>
        /// <param name="episodeId"></param>
        /// <param name="samples"></param>
        /// <param name="combinedFingerprintHashes"></param>
        void CreateFingerprintsFromSamples(UInt32 episodeId, List<Sample> samples, Dictionary<UInt32, FingerprintGroup> combinedFingerprintHashes) {
            DiscardUnimportantBandsForAllSamples(samples);
            LogImportantSampleBands(samples);

            //Create list of significant samples that are above weighted average amplitude across all samples
            var significantSamples = OnlySignificantSamples(samples);

            var fileFingerprintHashes = CreateFingerprintsForAllSignificantSamples(episodeId, significantSamples);
            CombineFingerprintHashes(combinedFingerprintHashes, fileFingerprintHashes);
        }

        /// <summary>
        /// Combine new file's fingerprints into collection that contains all the files in the group. This is either all files in the database or one file being identified
        /// </summary>
        /// <param name="combinedFingerprintHashes"> Existing list of fingerprints for prior files in group </param>
        /// <param name="fileFingerprintHashes"> List of fingerprints for newly processed file </param>
        private void CombineFingerprintHashes(Dictionary<UInt32, FingerprintGroup> combinedFingerprintHashes, Dictionary<UInt32, FingerprintGroup> fileFingerprintHashes) {
            lock (combinedFingerprintHashes) {
                foreach (var relatedFingerprintGroup in fileFingerprintHashes) {
                    var fingerprintHash = relatedFingerprintGroup.Key;
                    if (combinedFingerprintHashes.ContainsKey(fingerprintHash)) {
                        combinedFingerprintHashes[fingerprintHash].AppendFingerprints(relatedFingerprintGroup.Value.Fingerprints);
                    } else {
                        combinedFingerprintHashes.Add(fingerprintHash, relatedFingerprintGroup.Value);
                    }
                }
            }
        }

        private void DiscardUnimportantBandsForAllSamples(List<Sample> samples) {
            var WEIGHT = 3d;

            //Discard bands within each sample that are below weighted average amplitude
            var amplitudeFloor = AverageAmplitudeRMS(samples) * WEIGHT;
            //var amplitudeFloor = AverageAmplitudeMean(samples) * WEIGHT;

            foreach (var sample in samples) {
                //If band's amplitude is below weighted average, ignore it
                for (var i = 0; i < sample.AmplitudeAtFrequencyBands.Length; i++) {
                    if (sample.AmplitudeAtFrequencyBands[i] < amplitudeFloor) {
                        sample.AmplitudeAtFrequencyBands[i] = 0d;  //Flag: Ignore this amplitude as non-significant
                    }
                }
            }

            //TODO Test:
            DiscardRepeatingSamples(samples);
        }

        private void DiscardRepeatingSamples(List<Sample> samples) {
            const UInt64 MIN_TICKS_BETWEEN_SAMPLES_IN_CONTIGOUS_GROUP = SAMPLE_DURATION_TICKS * 4;
            var numberOfBands = samples[0].AmplitudeAtFrequencyBands.Length;
            for (var freqencyBandIdx = 0; freqencyBandIdx < numberOfBands; freqencyBandIdx++) {
                UInt64 priorSampleTime = 0;
                var priorSampleWasSignificant = false;
                foreach (var sample in samples) {
                    if (priorSampleWasSignificant
                        && sample.SampleTimeTicks <= priorSampleTime + MIN_TICKS_BETWEEN_SAMPLES_IN_CONTIGOUS_GROUP
                        && sample.AmplitudeAtFrequencyBands[freqencyBandIdx] != 0d) {
                        //This sample is in a run of significant samples for the current frequency band
                        sample.AmplitudeAtFrequencyBands[freqencyBandIdx] = 0d;
                    } else {
                        priorSampleWasSignificant = (sample.AmplitudeAtFrequencyBands[freqencyBandIdx] != 0d);
                    }
                    priorSampleTime = sample.SampleTimeTicks;
                }
            }
        }

        private double AverageAmplitudeRMS(List<Sample> samples) {
            //Compute average amplitude (RMS: Root Mean Squared) across all samples and bands
            var totalAmplitudeSquared = 0d;
            var count = 0;

            //Compute total amplitude squared across all bands
            foreach (var sample in samples) {
                foreach (var amplitudeAtFrequencyBand in sample.AmplitudeAtFrequencyBands) {
                    totalAmplitudeSquared += amplitudeAtFrequencyBand * amplitudeAtFrequencyBand;
                    count++;
                }
            }

            //Compute SquareRoot of Mean average amplitude to get RMS
            var meanApplitude = totalAmplitudeSquared / count;
            return Math.Sqrt(meanApplitude);
        }

        private double AverageAmplitudeMean(List<Sample> samples) {
            //Compute average amplitude (RMS: Root Mean Squared) across all samples and bands
            var totalAmplitude = 0d;
            var count = 0;

            //Compute total amplitude across all bands
            foreach (var sample in samples) {
                foreach (var amplitudeAtFrequencyBand in sample.AmplitudeAtFrequencyBands) {
                    totalAmplitude += amplitudeAtFrequencyBand;
                    count++;
                }
            }

            //Compute Mean average amplitude to get RMS
            var meanApplitude = totalAmplitude / count;
            return meanApplitude;
        }

        private void LogImportantSampleBands(List<Sample> samples) {
            var maxLines = 0;  //TODO set to 0 to exclude logging or >0 to limit number of lines logged
            if (maxLines > 0) {
                Console.Clear();
                Console.WriteLine("Samples: (SampleTimeMs, Freq, Amplitude)");
                foreach (var sample in samples) {
                    for (var i = 0; i < sample.AmplitudeAtFrequencyBands.Length; i++) {
                        if (sample.AmplitudeAtFrequencyBands[i] > 0d) {
                            Console.WriteLine("{0}\t{1}\t{2}", sample.SampleTimeTicks / 10000, SampleFFT.BandFrequencies[i], (int) sample.AmplitudeAtFrequencyBands[i]);
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

        private Dictionary<UInt32, FingerprintGroup> CreateFingerprintsForAllSignificantSamples(UInt32 episodeId, List<SignificantSample> significantSamples) {
            const int RELATED_SAMPLE_FANOUT = 5;  //Number of following samples to combine with root sample to create a fingerprint for a sample point
            const UInt64 STARTING_TIME_OFFSET_TICKS = 1_000_000_0;  // Ticks from root sample time to start matching
            const UInt64 FANOUT_DURATION_SECS = 1000;  // Seconds from root sample time + offset for limit to how far to search
            const UInt64 MAX_FANOUT_TIMESPAN = AVReader.TICKS_PER_SECOND * FANOUT_DURATION_SECS;  //End of time range to search for fanout

            var fingerprintHashes = new Dictionary<UInt32, FingerprintGroup>();  //Hashes for all related samples in significant samples: Hash + Fingerprints with same hash
            var significantSampleCount = significantSamples.Count;
            FingerprintGroup fingerprintListForHash;
            
            //Process each significant sample to find up to N related future samples
            for (var significantSampleIdx = 0; significantSampleIdx < significantSampleCount; significantSampleIdx++) {
                var rootSample = significantSamples[significantSampleIdx];
                var relatedSampleIdx = significantSampleIdx + 1;
                var startingTime = rootSample.SampleTimeTicks + STARTING_TIME_OFFSET_TICKS;
                var endingTime = startingTime + MAX_FANOUT_TIMESPAN;

                //Find up to N related samples
                for (var relatedSampleCnt = 0; relatedSampleCnt < RELATED_SAMPLE_FANOUT;) {
                    if (relatedSampleIdx >= significantSampleCount) {
                        break;
                    }

                    var relatedFutureSample = significantSamples[relatedSampleIdx++];
                    if (relatedFutureSample.SampleTimeTicks > startingTime 
                            && relatedFutureSample.SampleTimeTicks < endingTime) {
                        var fingerprint = new Fingerprint(episodeId, rootSample, relatedFutureSample);
                        var fingerprintHash = fingerprint.Hash;
                        if (fingerprintHashes.ContainsKey(fingerprintHash)) {
                            fingerprintListForHash = fingerprintHashes[fingerprintHash];
                        } else {
                            fingerprintListForHash = new FingerprintGroup(fingerprintHash);
                            fingerprintHashes.Add(fingerprintHash, fingerprintListForHash);
                        }
                        fingerprintListForHash.AddFingerprint(fingerprint);
                        relatedSampleCnt++;
                    }
                }
            }

            return fingerprintHashes;
        }

        //private Dictionary<UInt32, FingerprintGroup> DiscardLowValueHashes(Dictionary<UInt32, FingerprintGroup> fingerprintHashes) {
            //var DISCARD_THRESHOLD_PERCENTILE = .90d;

            ////Determine duplicate hash count to start discarding at
            //var countList = new List<int>();
            //foreach (var kvp in fingerprintHashes) {
            //    countList.Add(kvp.Value.Fingerprints.Count());
            //}
            //countList.Sort();
            //var midIdx = (int)(countList.Count() * DISCARD_THRESHOLD_PERCENTILE);
            //var discardThreshold = countList[midIdx];

            ////Discard any fingerprint collections with too many fingerprints
            //var hashesToDiscard = new List<UInt32>();
            //foreach (var kvp in fingerprintHashes) {
            //    if (kvp.Value.Fingerprints.Count > discardThreshold) {
            //        hashesToDiscard.Add(kvp.Key);
            //    }
            //}
            //foreach (var hashToDiscard in hashesToDiscard) {
            //    fingerprintHashes.Remove(hashToDiscard);
            //}

        //    return fingerprintHashes;
        //}

        List<SignificantSample> OnlySignificantSamples(List<Sample> allSamples) {
            List<SignificantSample> newList = new List<SignificantSample>(allSamples.Count);
            foreach (var sample in allSamples) {
                for (var i = 0; i < sample.AmplitudeAtFrequencyBands.Length; i++) {
                    if (sample.AmplitudeAtFrequencyBands[i] > 0) {
                        newList.Add(new SignificantSample(sample.SampleTimeTicks, SampleFFT.BandFrequencies[i], sample.AmplitudeAtFrequencyBands[i]));
                    }
                }
            }

            return newList;
        }
    }
}
