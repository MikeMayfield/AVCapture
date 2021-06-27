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
        private static int[] FREQUENCY_BANDS = new int[] { 386, 457, 542, 642, 761, 902, 1068 };  //Log(n) frequency bands //TODO Interatively determine optimal frequency ranges for best lookup accuracy

        private static FFT fft = null;  //Fast Fourier Transform helper class
        private static int sampleRateHz;
        private static double[] fftBuffer = null;  //Buffer to use for FFT result
        private static int[] fftBinFrequencies;  //The freqency where each FFT bin starts
        private static double[] windowCoefficients;  //Coefficient to multiple each associated FFT bin by to provide windowing (currently Hann window)
        private static int indexToFirstFftBinForBands;  //Index into the FFT bin that corresponds to the start of the first band

        public static int[] BandFrequencies;  //The frequency where each FFT band starts
        public UInt64 SampleTimeTicks { get; private set; }  //Sample time in 100ns ticks
        public double[] AmplitudeAtFrequencyBands;  //Amplitude (RMS) found across all FFT bins in a FFT band  //TODO convert to int or short

        /// <summary>
        /// Create a fingerprint for an audio sample
        /// </summary>
        public Sample(int sampleRateHz, UInt64 sampleTimeTicks, Int16[] pcmSampleAmplitudes) {
            this.SampleTimeTicks = sampleTimeTicks;

            if (fft == null) {
                fft = new FFT();
                fftBuffer = new double[pcmSampleAmplitudes.Length];
                fft.Initialize((uint) pcmSampleAmplitudes.Length);
                windowCoefficients = DSP.Window.Coefficients(DSP.Window.Type.Hann, (uint) pcmSampleAmplitudes.Length);
                var dFftBinFrequences = fft.FrequencySpan(sampleRateHz);
                fftBinFrequencies = new int[dFftBinFrequences.Length];
                for (var i = 0; i < dFftBinFrequences.Length; i++) {
                    fftBinFrequencies[i] = Convert.ToInt32(dFftBinFrequences[i]);
                }
                Sample.sampleRateHz = sampleRateHz;
                BandFrequencies = SelectFrequencyBands(FREQUENCY_BANDS);
            }

            var amplitudeHisto = CreateFrequencyHistogramFromPcmSlices(pcmSampleAmplitudes);
            ComputeAmplitudeAtFrequencyBands(amplitudeHisto);  //Fill AmplitudeAtFrequencyBands property with RMS amplitude for each frequency band we care about
        }

        public static int FrequencyIdx(int frequency) {
            for (var i = 0; i < BandFrequencies.Length; i++) {
                if (frequency <= BandFrequencies[i]) {
                    return i;
                }
            }

            return 0;  //Need some default return value, but shouldn't ever be used
        }

        /// <summary>
        /// Given the list of frequency band boundaries, calculate the exact starting frequency 
        /// of each band and the offset within the FFT bins to the start of each band
        /// </summary>
        private int[] SelectFrequencyBands(int[] frequencyBandBoundaries) {
            var frequencyBoundariesCnt = frequencyBandBoundaries.Length;
            var bandFrequencies = new int[frequencyBoundariesCnt];

            var nextLowerBound = frequencyBandBoundaries[0];
            var bandIdx = 0;

            for (var i = 0; i < fftBinFrequencies.Length; i++) {
                if (fftBinFrequencies[i] >= nextLowerBound) {
                    bandFrequencies[bandIdx] = (int) fftBinFrequencies[i];
                    if (bandIdx == 0) {
                        indexToFirstFftBinForBands = i;
                    }
                    bandIdx++;
                    if (bandIdx < frequencyBoundariesCnt) {
                        nextLowerBound = frequencyBandBoundaries[bandIdx];
                    } else {
                        break;
                    }
                }
            }

            return bandFrequencies;
        }

        /// <summary>
        /// Convert all the PCM slices of a sample into a FFT histogram of amplitude (in Db) at each FFT bin
        /// </summary>
        /// <param name="pcmSampleAmplitudes"></param>
        /// <returns></returns>
        private double[] CreateFrequencyHistogramFromPcmSlices(Int16[] pcmSampleAmplitudes) {
            ScalePcmToFitWindow(pcmSampleAmplitudes);

            //Perform FFT on input buffer, returning absolute magnitude in each FFT bin
            var result = DSP.ConvertComplex.ToMagnitude(fft.Execute(fftBuffer));

            return result;
        }

        /// <summary>
        /// A Hann window is applied to the sample values to properly filter the beginning and ending of the PCM data in a 
        /// sample to reduce noise from clipped frequencies at sample boundaries 
        /// (see https://download.ni.com/evaluation/pxi/Understanding%20FFTs%20and%20Windowing.pdf)
        /// </summary>
        /// <param name="pcmSampleAmplitudes"></param>
        private void ScalePcmToFitWindow(Int16[] pcmSampleAmplitudes) {
            for (int i = 0; i < pcmSampleAmplitudes.Length; i++) {
                fftBuffer[i] = (double) pcmSampleAmplitudes[i] * windowCoefficients[i];
            }
        }

        ///// <summary>
        ///// Convert an FFT result from absolute magnitudes to magnitudes in Db power
        ///// </summary>
        ///// <param name="fftMagnitudes"></param>
        //private void ConvertFftMagnitudesToDb(double[] fftMagnitudes) {
        //    var ZERO_DB_AMPLITUDE = 1024d;
        //    for (var i = 0; i < fftMagnitudes.Length; i++) {
        //        fftMagnitudes[i] = (fftMagnitudes[i] != 0D) ? 20.0d * Math.Log10(fftMagnitudes[i] / ZERO_DB_AMPLITUDE) : Double.MinValue;  //TODO This could use Math.Log and a combined multiplier
        //    }
        //}

        //private double[] CreateCombinedFrequencyHistogram(double[] frequencyHistoFromSamples, int[] bucketIndices) {
        //    double[] result = new double[bucketIndices.Length - 1];

        //    double sum = 0d;
        //    double sumSquared = 0d;
        //    int bucketIndicesIdx = 1;
        //    int resultIdx = 0;
        //    double slotCount = 0;

        //    for (int i = bucketIndices[0]; i < frequencyHistoFromSamples.Length; i++) {
        //        //If slot belongs to next bucket, compute current bucket and prepare for next bucket
        //        if (i >= bucketIndices[bucketIndicesIdx]) {
        //            //result[resultIdx++] = (slotCount > 0) ? Math.Sqrt(sumSquared / slotCount) : 0;
        //            result[resultIdx++] = sum;
        //            sum = 0d;
        //            sumSquared = 0d;
        //            slotCount = 0d;
        //            bucketIndicesIdx++;
        //            if (bucketIndicesIdx >= bucketIndices.Length) {
        //                break;
        //            }
        //        }

        //        //Compute sum of slot value squared for one more slot
        //        sum += frequencyHistoFromSamples[i];
        //        sumSquared += (frequencyHistoFromSamples[i] * frequencyHistoFromSamples[i]);
        //        slotCount++;
        //    }

        //    return result;
        //}

        private void ComputeAmplitudeAtFrequencyBands(double[] amplitudeAtFftFrequecyBin) {
            AmplitudeAtFrequencyBands = new double[FREQUENCY_BANDS.Length - 1];
            var nextBandIdx = 1;
            var sumOfAmplitudeSquared = 0d;
            var countOfAmplitudes = 0d;

            //Compute RMS amplitude within each frequency band that we are using
            for (var i = indexToFirstFftBinForBands; i < amplitudeAtFftFrequecyBin.Length; i++) {
                //If FFT bin belongs to next band, remember best match for band and prepare for next band
                if (fftBinFrequencies[i] >= BandFrequencies[nextBandIdx]) {
                    //Remember results for bin that is about to change
                    AmplitudeAtFrequencyBands[nextBandIdx - 1] = (countOfAmplitudes > 0) ? 
                            Math.Sqrt(sumOfAmplitudeSquared / countOfAmplitudes) : 0d;  //RMS amplitude of FFT bins within band

                    //Advance to next bin
                    sumOfAmplitudeSquared = 0d;
                    countOfAmplitudes = 0;
                    nextBandIdx++;
                    if (nextBandIdx > AmplitudeAtFrequencyBands.Length) {
                        break;
                    }
                }

                //Keep a running total of the amplitudes_squared and the sample count for use in computing RMS average when band changes
                sumOfAmplitudeSquared += amplitudeAtFftFrequecyBin[i] * amplitudeAtFftFrequecyBin[i];
                countOfAmplitudes++;
            }
        }
    }
}
