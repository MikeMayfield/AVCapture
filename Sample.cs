using DSPLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    /// <summary>
    /// Create a sample summary for a fixed-size audio sample
    /// </summary>
    class Sample
    {
        private static readonly int MIN_FREQUENCY = 40;  //Min frequency to process (Hz)
        private static readonly int MAX_FREQUENCY = 1100;  //Max frequency to process (Hz)
        private static readonly int MIN_AMPLITUDE = 0;  //Minimum amplitude to significant and not be considered silence
        private static readonly int BUCKETS_PER_COMBINED_BUCKET = 1;  //Number of FFT buckets to combine into one smaller bucket

        private static FFT fft = null;
        private static int sampleRateHz;
        private static double[] fftBuffer = null;
        private static int[] bucketFrequencies;
        private static int minFrequencyBucketIdx;  //Number of buckets to skip to get past MIN_FREQUENCY if 44100 Hz sample rate
        private static int maxFrequencyBucketIdx;
        private static int fftBucketCountAfterCombining;  

        public int Frequency { get; private set; }  //Frequency in hz
        public long SampleTime { get; private set; }  //Sample time in 10ns ticks

        public int Amplitude;  //Amplitude in ?

        public static void Init() {
            fft = null;
        }

        /// <summary>
        /// Create a fingerprint for an audio sample
        /// </summary>
        public Sample (int sampleRateHz, long sampleTime, Int16[] pcmSampleAmplitudes) {
            this.SampleTime = sampleTime;

            if (fft == null) {
                fft = new FFT();
                fft.Initialize((uint) pcmSampleAmplitudes.Length);
                fftBucketCountAfterCombining = pcmSampleAmplitudes.Length / 2 / BUCKETS_PER_COMBINED_BUCKET;
                Sample.sampleRateHz = sampleRateHz;
                bucketFrequencies = FrequencySpan(BUCKETS_PER_COMBINED_BUCKET);
                minFrequencyBucketIdx = IdxForFrequency(bucketFrequencies, MIN_FREQUENCY);
                maxFrequencyBucketIdx = IdxForFrequency(bucketFrequencies, MAX_FREQUENCY) - 1;
            }

            var amplitudeHisto = CreateFrequencyHistogramFromSamples(pcmSampleAmplitudes);
            var combinedAmplitudeHist = CreateCombinedFrequencyHistogram(amplitudeHisto, BUCKETS_PER_COMBINED_BUCKET);

            Frequency = FrequencyForMaxAmplitude(combinedAmplitudeHist);

            //Console.WriteLine("MaxFreq: {0}, Amplitudes: {1} {2} {3}", 
                //Frequency, amplitudeHisto[minFrequencyBucketIdx], amplitudeHisto[minFrequencyBucketIdx], amplitudeHisto[minFrequencyBucketIdx + 2]);
        }

        private int IdxForFrequency(int[] bucketFrequencies, int minFrequency) {
            for (int i = 0; i < bucketFrequencies.Length; i++) {
                if (bucketFrequencies[i] >= minFrequency) {
                    return i;
                }
            }

            return bucketFrequencies.Length - 1;
        }

        private int FrequencyForMaxAmplitude(double[] amplitudeHistogram) {
            var maxAmplitude = 0d;
            var maxIdx = 0;

            for (int i = minFrequencyBucketIdx; i <= maxFrequencyBucketIdx; i++) {
                if (amplitudeHistogram[i] > maxAmplitude) {
                    maxAmplitude = amplitudeHistogram[i];
                    maxIdx = i;
                }
            }

            //Console.WriteLine("maxAmplitude: {0}", maxAmplitude);
            Amplitude = (int) maxAmplitude;
            return (int) bucketFrequencies[maxIdx];
            //return (maxAmplitude >= MIN_AMPLITUDE) ? (int) bucketFrequencies[maxIdx] : 0;  //TODO
        }

        /// <summary>
        /// Calculate frequency for each bucket in an FFT list
        /// </summary>
        /// <param name="combinedBucketCnt"> Number of buckets in list </param>
        /// <returns> List of frequencies for buckets </returns>
        /// TODO: Return short int array instead of double. Change all uses of frequency to int
        private int[] FrequencySpan(int combinedBucketCnt) {
            var fullFrequencySpan = fft.FrequencySpan(sampleRateHz);
            int[] result = new int[fullFrequencySpan.Length / combinedBucketCnt];
            double sum = 0d;
            int resultIdx = 0;
            int combinedCountRemaining = combinedBucketCnt;
            for (int i = 0; i < fullFrequencySpan.Length; i++) {
                sum += fullFrequencySpan[i];
                combinedCountRemaining--;
                if (combinedCountRemaining == 0) {
                    result[resultIdx++] = (int)sum / combinedBucketCnt;  //Average frequency across all combined buckets
                    combinedCountRemaining = combinedBucketCnt;
                    sum = 0d;
                }
            }

            return result;
        }

        private void LoadFftBuffer(Int16[] pcmSampleAmplitudes) {
            if (fftBuffer == null || fftBuffer.Length != pcmSampleAmplitudes.Length) {
                fftBuffer = new double[pcmSampleAmplitudes.Length];
            }

            for (int i = 0; i < pcmSampleAmplitudes.Length; i++) {
                fftBuffer[i] = (double) pcmSampleAmplitudes[i];
            }
        }

        private double[] CreateFrequencyHistogramFromSamples(Int16[] pcmSampleAmplitudes) {
            LoadFftBuffer(pcmSampleAmplitudes);
            return DSP.ConvertComplex.ToMagnitude(fft.Execute(fftBuffer));
        }

        /// <summary>
        /// Combine several histogram buckets into one, computing RMS average amplitude in resulting bucket.
        /// </summary>
        /// <param name="frequencyHistoFromSamples"> Histogram of 1025 frequency buckets </param>
        /// <param name="fftBucketsPerCombinedBucketCnt"> Number of buckets to combine into one </param>
        /// <returns> Smaller, combined histogram </returns>
        private double[] CreateCombinedFrequencyHistogram(double[] frequencyHistoFromSamples, int fftBucketsPerCombinedBucketCnt) {
            double[] result = new double[frequencyHistoFromSamples.Length / fftBucketsPerCombinedBucketCnt];

            double sumSquared = 0d;
            int resultIdx = 0;
            int combinedCountRemaining = fftBucketsPerCombinedBucketCnt;
            double dblFftBucketsPerCombinedBucketCnt = (double) fftBucketsPerCombinedBucketCnt;

            for (int i = 0; i < frequencyHistoFromSamples.Length; i++) {
                sumSquared += (frequencyHistoFromSamples[i] * frequencyHistoFromSamples[i]);
                combinedCountRemaining--;
                if (combinedCountRemaining == 0) {
                    result[resultIdx++] = Math.Sqrt(sumSquared / dblFftBucketsPerCombinedBucketCnt);
                    combinedCountRemaining = fftBucketsPerCombinedBucketCnt;
                    sumSquared = 0d;
                }
            }

            return result;
        }
    }
}
