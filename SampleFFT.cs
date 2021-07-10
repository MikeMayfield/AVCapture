using DSPLib;
using System;
using System.Numerics;

namespace AVCapture
{
    class SampleFFT
    {
        public static int[] FREQUENCY_BANDS = new int[] { 301, 345, 388, 431, 474, 517, 560, 603, 646, 689, 732, 775, 818, 861, 904, 947, 990 };

        public FFT fft;
        public int sampleRateHz;
        public double[] fftBuffer = null;  //Buffer to use for FFT result
        public int[] fftBinFrequencies;  //The freqency where each FFT bin starts
        public double[] windowCoefficients;  //Coefficient to multiple each associated FFT bin by to provide windowing (currently Hann window)
        public int indexToFirstFftBinForBands;  //Index into the FFT bin that corresponds to the start of the first band
        public static int[] BandFrequencies;  //The frequency where each FFT band starts

        public SampleFFT(int sampleRateHz, uint bufferSize) {
            fft = new FFT();
            fftBuffer = new double[bufferSize];
            fft.Initialize(bufferSize);
            windowCoefficients = DSP.Window.Coefficients(DSP.Window.Type.Hann, bufferSize);
            var dFftBinFrequences = fft.FrequencySpan(sampleRateHz);
            fftBinFrequencies = new int[dFftBinFrequences.Length];
            for (var i = 0; i < dFftBinFrequences.Length; i++) {
                fftBinFrequencies[i] = Convert.ToInt32(dFftBinFrequences[i]);
            }
            this.sampleRateHz = sampleRateHz;
            BandFrequencies = SelectFrequencyBands(FREQUENCY_BANDS);
        }

        public Complex[] Execute() {
            return fft.Execute(fftBuffer);
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

    }
}
