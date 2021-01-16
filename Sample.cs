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
        private static readonly int MIN_FREQUENCY = 550;  //Min frequency to process = 500Hz
        private static readonly int MAX_FREQUENCY = 15000;  //Max frequency to process = 15,000Hz
        private static readonly double MIN_AMPLITUDE = 100d;  //Minimum amplitude to significant and not be considered silence

        private static FFT fft = null;
        private static int sampleRateHz;
        private static double[] fftBuffer = null;
        private static double[] bucketFrequencies;
        private static int minFrequencyBucketIdx = 24;  //Number of buckets to skip to get past 500Hz if 44100 Hz sample rate
        private static int maxFrequencyBucketIdx;

        public int Frequency { get; private set; }  //Frequency in hz
        public long SampleTime { get; private set; }  //Sample time in 10ns ticks

        public double Amplitude;  //Amplitude in ?


        /// <summary>
        /// Create a fingerprint for 
        /// </summary>
        public Sample (int sampleRateHz, long sampleTime, Int16[] pcmSampleAmplitudes) {
            this.SampleTime = sampleTime;

            if (fft == null) {
                fft = new FFT();
                fft.Initialize((uint) pcmSampleAmplitudes.Length);
                Sample.sampleRateHz = sampleRateHz;
                bucketFrequencies = FrequencySpan();
                minFrequencyBucketIdx = IdxForFrequency(bucketFrequencies, MIN_FREQUENCY);
                maxFrequencyBucketIdx = IdxForFrequency(bucketFrequencies, MAX_FREQUENCY);
            }

            var amplitudeHisto = CreateFrequencyHistogramFromSamples(pcmSampleAmplitudes);

            Frequency = FrequencyForMaxAmplitude(amplitudeHisto);

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
            }

            //Console.WriteLine("maxAmplitude: {0}", maxAmplitude);
            Amplitude = maxAmplitude;
            return (maxAmplitude >= MIN_AMPLITUDE) ? (int) bucketFrequencies[maxIdx] : 0;
        }

        private double[] FrequencySpan() {
            return fft.FrequencySpan(sampleRateHz);
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
    }
}
