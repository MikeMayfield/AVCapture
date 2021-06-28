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
        public UInt64 SampleTimeTicks { get; private set; }  //Sample time in 100ns ticks
        public double[] AmplitudeAtFrequencyBands;  //Amplitude (RMS) found across all FFT bins in a FFT band  //TODO convert to int or short

        /// <summary>
        /// Create a fingerprint for an audio sample
        /// </summary>
        public Sample(UInt64 sampleTimeTicks, Int16[] pcmSampleAmplitudes, SampleFFT sampleFFT) {
            this.SampleTimeTicks = sampleTimeTicks;

            var amplitudeHisto = CreateFrequencyHistogramFromPcmSlices(pcmSampleAmplitudes, sampleFFT);
            ComputeAmplitudeAtFrequencyBands(amplitudeHisto, sampleFFT);  //Fill AmplitudeAtFrequencyBands property with RMS amplitude for each frequency band we care about
        }

        public static int FrequencyIdx(int frequency) {
            for (var i = 0; i < SampleFFT.BandFrequencies.Length; i++) {
                if (frequency <= SampleFFT.BandFrequencies[i]) {
                    return i;
                }
            }

            return 0;  //Need some default return value, but shouldn't ever be used
        }

        /// <summary>
        /// Given the list of frequency band boundaries, calculate the exact starting frequency 
        /// of each band and the offset within the FFT bins to the start of each band
        /// </summary>
        private int[] SelectFrequencyBands(int[] frequencyBandBoundaries, SampleFFT sampleFFT) {
            var frequencyBoundariesCnt = frequencyBandBoundaries.Length;
            var bandFrequencies = new int[frequencyBoundariesCnt];

            var nextLowerBound = frequencyBandBoundaries[0];
            var bandIdx = 0;

            for (var i = 0; i < sampleFFT.fftBinFrequencies.Length; i++) {
                if (sampleFFT.fftBinFrequencies[i] >= nextLowerBound) {
                    bandFrequencies[bandIdx] = (int) sampleFFT.fftBinFrequencies[i];
                    if (bandIdx == 0) {
                        sampleFFT.indexToFirstFftBinForBands = i;
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
        private double[] CreateFrequencyHistogramFromPcmSlices(Int16[] pcmSampleAmplitudes, SampleFFT sampleFFT) {
            ScalePcmToFitWindow(pcmSampleAmplitudes, sampleFFT);

            //Perform FFT on input buffer, returning absolute magnitude in each FFT bin
            var result = DSP.ConvertComplex.ToMagnitude(sampleFFT.Execute());

            return result;
        }

        /// <summary>
        /// A Hann window is applied to the sample values to properly filter the beginning and ending of the PCM data in a 
        /// sample to reduce noise from clipped frequencies at sample boundaries 
        /// (see https://download.ni.com/evaluation/pxi/Understanding%20FFTs%20and%20Windowing.pdf)
        /// </summary>
        /// <param name="pcmSampleAmplitudes"></param>
        private void ScalePcmToFitWindow(Int16[] pcmSampleAmplitudes, SampleFFT sampleFFT) {
            for (int i = 0; i < pcmSampleAmplitudes.Length; i++) {
                sampleFFT.fftBuffer[i] = (double) pcmSampleAmplitudes[i] * sampleFFT.windowCoefficients[i];
            }
        }

        private void ComputeAmplitudeAtFrequencyBands(double[] amplitudeAtFftFrequecyBin, SampleFFT sampleFFT) {
            AmplitudeAtFrequencyBands = new double[SampleFFT.FREQUENCY_BANDS.Length - 1];
            var nextBandIdx = 1;
            var sumOfAmplitudeSquared = 0d;
            var countOfAmplitudes = 0d;

            //Compute RMS amplitude within each frequency band that we are using
            for (var i = sampleFFT.indexToFirstFftBinForBands; i < amplitudeAtFftFrequecyBin.Length; i++) {
                //If FFT bin belongs to next band, remember best match for band and prepare for next band
                if (sampleFFT.fftBinFrequencies[i] >= SampleFFT.BandFrequencies[nextBandIdx]) {
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
