using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    /// <summary>
    /// Fast Fourier Transform
    /// </summary>
    public class FFT
    {

        int sampleCount, m;

        // Lookup tables. Only need to recompute when size of FFT changes.
        readonly double[] cosTable;
        readonly double[] sinTable;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="sampleCount">Number of samples in input collection. Must be power of two</param>
        public FFT(int sampleCount) {
            this.sampleCount = sampleCount;
            this.m = (int) (Math.Log(sampleCount) / Math.Log(2));

            //Make sure sampleCount is a power of 2
            if (sampleCount != (1 << m))
                throw new ArgumentException("FFT length must be power of 2");

            //Precompute tables
            cosTable = new double[sampleCount / 2];
            sinTable = new double[sampleCount / 2];
            var piOverSampleCountTimesMinus2 = -2 * Math.PI / sampleCount;
            for (int i = 0; i < sampleCount / 2; i++) {
                var temp2 = piOverSampleCountTimesMinus2 * i;
                cosTable[i] = Math.Cos(temp2);
                sinTable[i] = Math.Sin(temp2);
            }
        }

        /// <summary>
        /// Compute FFT of PCM-16 sample buffer
        /// </summary>
        /// <param name="pcmSampleApplitudes">Collection of input samples</param>
        /// <returns></returns>
        public double[] fft(Int16[] pcmSampleApplitudes) {
            var realComponents = new double[sampleCount];
            var complexComponents = new double[sampleCount];
            for (int i = 0; i < sampleCount; i++) {
                realComponents[i] = (double) pcmSampleApplitudes[i];
                complexComponents[i] = 0.0d;
            }
            fft(realComponents, complexComponents);
            return realComponents;
        }

        public void fft(double[] realComponents, double[] complexComponents) {
            int i, j, k, n1, n2, a;
            double c, s, t1, t2;

            // Bit-reverse
            j = 0;
            n2 = sampleCount / 2;
            for (i = 1; i < sampleCount - 1; i++) {
                n1 = n2;
                while (j >= n1) {
                    j = j - n1;
                    n1 = n1 / 2;
                }
                j += n1;

                if (i < j) {
                    t1 = realComponents[i];
                    realComponents[i] = realComponents[j];
                    realComponents[j] = t1;
                    t1 = complexComponents[i];
                    complexComponents[i] = complexComponents[j];
                    complexComponents[j] = t1;
                }
            }

            // FFT
            n2 = 1;

            for (i = 0; i < m; i++) {
                n1 = n2;
                n2 += n2;
                a = 0;

                for (j = 0; j < n1; j++) {
                    c = cosTable[a];
                    s = sinTable[a];
                    a += 1 << (m - i - 1);

                    for (k = j; k < sampleCount; k = k + n2) {
                        t1 = c * realComponents[k + n1] - s * complexComponents[k + n1];
                        t2 = s * realComponents[k + n1] + c * complexComponents[k + n1];
                        realComponents[k + n1] = realComponents[k] - t1;
                        complexComponents[k + n1] = complexComponents[k] - t2;
                        realComponents[k] = realComponents[k] + t1;
                        complexComponents[k] = complexComponents[k] + t2;
                    }
                }
            }
        }
    }
}
