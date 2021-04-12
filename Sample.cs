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
        //private static int[] FREQUENCY_BUCKETS = new int[] { 2000, 3000, 4500, 8000 };  //Frequency bands to combine into individual buckets
        private static int[] FREQUENCY_BUCKETS = new int[] { 540, 728, 983, 1326, 1789, 2414, 3257, 4394, 5929, 8000 };  //Log(n) frequency bands 
        //private static int[] FREQUENCY_BUCKETS = new int[] { 400, 500, 600, 700, 800, 950, 1000, 1050, 1200 };  //Frequency bands to combine into individual buckets - 1KHz test
        private static FFT fft = null;
        private static int sampleRateHz;
        private static double[] fftBuffer = null;
        private static int[] bucketIndices = null;
        private static double[] windowCoefficients;
        private static double[] fullFrequencySpan;

        public static int[] BucketFrequencies;
        public long SampleTime { get; private set; }  //Sample time in 10ns ticks
        public double[] AmplitudeAtFrequencyBand;

        public static void Init() {
            fft = null;
        }

        /// <summary>
        /// Create a fingerprint for an audio sample
        /// </summary>
        public Sample(int sampleRateHz, long sampleTime, Int16[] pcmSampleAmplitudes) {
            this.SampleTime = sampleTime;

            if (fft == null) {
                fft = new FFT();
                fft.Initialize((uint) pcmSampleAmplitudes.Length);
                windowCoefficients = DSP.Window.Coefficients(DSP.Window.Type.Hann, (uint) pcmSampleAmplitudes.Length);
                fullFrequencySpan = fft.FrequencySpan(sampleRateHz);
                Sample.sampleRateHz = sampleRateHz;
                CombineFrequencyBands(out bucketIndices, out BucketFrequencies);
            }

            var amplitudeHisto = CreateFrequencyHistogramFromPcmSlices(pcmSampleAmplitudes);
            AmplitudeAtFrequencyBand = CreateCombinedFrequencyHistogram(amplitudeHisto, bucketIndices);

            if (false) {  //TODO
                foreach (var amplitude in AmplitudeAtFrequencyBand) {
                    Console.Write($"{(int) (amplitude * 100)} ");
                }
                Console.WriteLine();
            }
        }

        public static int FrequencyIdx(int frequency) {
            for (var i = 0; i < BucketFrequencies.Length; i++) {
                if (frequency <= BucketFrequencies[i]) {
                    return i;
                }
            }

            return 0;  //Need some default return value, but shouldn't ever be used
        }

        private void CombineFrequencyBands(out int[] bucketIndex, out int[] bucketFrequency) {
            var frequencyBoundariesCnt = FREQUENCY_BUCKETS.Length;
            bucketIndex = new int[frequencyBoundariesCnt];
            bucketFrequency = new int[frequencyBoundariesCnt];

            var nextLowerBound = FREQUENCY_BUCKETS[0];
            var outIdx = 0;

            for (var i = 0; i < fullFrequencySpan.Length; i++) {
                if (fullFrequencySpan[i] >= nextLowerBound) {
                    bucketIndex[outIdx] = i;
                    bucketFrequency[outIdx] = (int) fullFrequencySpan[i];
                    outIdx++;
                    if (outIdx < frequencyBoundariesCnt) {
                        nextLowerBound = FREQUENCY_BUCKETS[outIdx];
                    } else {
                        break;
                    }
                }
            }

        }

        private void LoadFftBuffer(Int16[] pcmSampleAmplitudes) {
            if (fftBuffer == null || fftBuffer.Length != pcmSampleAmplitudes.Length) {
                fftBuffer = new double[pcmSampleAmplitudes.Length];
            }

            for (int i = 0; i < pcmSampleAmplitudes.Length; i++) {
                fftBuffer[i] = (double) pcmSampleAmplitudes[i] * windowCoefficients[i];
            }
        }

        private double[] CreateFrequencyHistogramFromPcmSlices(Int16[] pcmSampleAmplitudes) {
            LoadFftBuffer(pcmSampleAmplitudes);
            return DSP.ConvertComplex.ToMagnitude(fft.Execute(fftBuffer));
        }

        private double[] CreateCombinedFrequencyHistogram(double[] frequencyHistoFromSamples, int[] bucketIndices) {
            double[] result = new double[bucketIndices.Length - 1];

            double sum = 0d;
            double sumSquared = 0d;
            int bucketIndicesIdx = 1;
            int resultIdx = 0;
            double slotCount = 0;

            for (int i = bucketIndices[0]; i < frequencyHistoFromSamples.Length; i++) {
                //If slot belongs to next bucket, compute current bucket and prepare for next bucket
                if (i >= bucketIndices[bucketIndicesIdx]) {
                    //result[resultIdx++] = (slotCount > 0) ? Math.Sqrt(sumSquared / slotCount) : 0;
                    result[resultIdx++] = sum;
                    sum = 0d;
                    sumSquared = 0d;
                    slotCount = 0d;
                    bucketIndicesIdx++;
                    if (bucketIndicesIdx >= bucketIndices.Length) {
                        break;
                    }
                }

                //Compute sum of slot value squared for one more slot
                sum += frequencyHistoFromSamples[i];
                sumSquared += (frequencyHistoFromSamples[i] * frequencyHistoFromSamples[i]);
                slotCount++;
            }

            return result;
        }

    }
}
