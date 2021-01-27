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
        //private static readonly int MIN_FREQUENCY = 550;  //Min frequency to process = 500Hz
        //private static readonly int MAX_FREQUENCY = 15000;  //Max frequency to process = 15,000Hz
        //private static readonly double MIN_AMPLITUDE = 100d;  //Minimum amplitude to significant and not be considered silence
        private static readonly int MIN_FREQUENCY = 2000;  //Min frequency to process (Hz)
        private static readonly int MAX_FREQUENCY = 22000;  //Max frequency to process = 15,000Hz
        private static readonly double MIN_AMPLITUDE = 0;  //Minimum amplitude to significant and not be considered silence
        private static readonly int COMBINED_BUCKET_CNT = 32;  //Number of FFT buckets desired (likely smaller than number created by FFT result)

        private static FFT fft = null;
        private static int sampleRateHz;
        private static double[] fftBuffer = null;
        private static double[] bucketFrequencies;
        private static int minFrequencyBucketIdx;  //Number of buckets to skip to get past 500Hz if 44100 Hz sample rate
        private static int maxFrequencyBucketIdx;
        private static int fftBucketsPerCombinedBucketCnt;

        public int Frequency { get; private set; }  //Frequency in hz
        public long SampleTime { get; private set; }  //Sample time in 10ns ticks

        public double Amplitude;  //Amplitude in ?

        public static void Init() {
            fft = null;
        }

        /// <summary>
        /// Create a fingerprint for 
        /// </summary>
        public Sample (int sampleRateHz, long sampleTime, Int16[] pcmSampleAmplitudes) {
            this.SampleTime = sampleTime;

            if (fft == null) {
                fft = new FFT();
                fft.Initialize((uint) pcmSampleAmplitudes.Length);
                fftBucketsPerCombinedBucketCnt = pcmSampleAmplitudes.Length / 2 / COMBINED_BUCKET_CNT;
                Sample.sampleRateHz = sampleRateHz;
                bucketFrequencies = FrequencySpan(COMBINED_BUCKET_CNT);
                minFrequencyBucketIdx = IdxForFrequency(bucketFrequencies, MIN_FREQUENCY);
                maxFrequencyBucketIdx = IdxForFrequency(bucketFrequencies, MAX_FREQUENCY);
            }

            var amplitudeHisto = CreateFrequencyHistogramFromSamples(pcmSampleAmplitudes);
            var combinedAmplitudeHist = CreateCombinedFrequencyHistogram(amplitudeHisto, fftBucketsPerCombinedBucketCnt);

            Frequency = FrequencyForMaxAmplitude(combinedAmplitudeHist);

            //Console.WriteLine("MaxFreq: {0}, Amplitudes: {1} {2} {3}", 
                //Frequency, amplitudeHisto[minFrequencyBucketIdx], amplitudeHisto[minFrequencyBucketIdx + 1], amplitudeHisto[minFrequencyBucketIdx + 2]);
        }

        private int IdxForFrequency(double[] bucketFrequencies, int minFrequency) {
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
                //TODO The following is a test
                //maxAmplitude += amplitudeHistogram[i];
            }

            //Console.WriteLine("maxAmplitude: {0}", maxAmplitude);
            Amplitude = maxAmplitude;
            return (maxAmplitude >= MIN_AMPLITUDE) ? (int) bucketFrequencies[maxIdx] : 0;  //TODO
            //return 100;
        }

        private double[] FrequencySpan(int combinedBucketCnt) {
            var fullFrequencySpan = fft.FrequencySpan(sampleRateHz);
            double[] result = new double[fullFrequencySpan.Length / combinedBucketCnt];
            double sum = 0d;
            int resultIdx = 0;
            int combinedCountRemaining = combinedBucketCnt;
            for (int i = 0; i < fullFrequencySpan.Length; i++) {
                sum += fullFrequencySpan[i];
                combinedCountRemaining--;
                if (combinedCountRemaining == 0) {
                    result[resultIdx++] = sum / (double) combinedBucketCnt;
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

        private double[] CreateCombinedFrequencyHistogram(double[] frequencyHistoFromSamples, int fftBucketsPerCombinedBucketCnt) {
            double[] result = new double[frequencyHistoFromSamples.Length / fftBucketsPerCombinedBucketCnt];
            double sumSquared = 0d;
            int resultIdx = 0;
            int combinedCountRemaining = fftBucketsPerCombinedBucketCnt;
            for (int i = 0; i < frequencyHistoFromSamples.Length; i++) {
                sumSquared += (frequencyHistoFromSamples[i] * frequencyHistoFromSamples[i]);
                combinedCountRemaining--;
                if (combinedCountRemaining == 0) {
                    result[resultIdx++] = Math.Sqrt(sumSquared / (double) fftBucketsPerCombinedBucketCnt);
                    combinedCountRemaining = fftBucketsPerCombinedBucketCnt;
                    sumSquared = 0d;
                }
            }

            return result;
        }
    }
}
