using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    class SignificantSample
    {
        public UInt64 SampleTimeTicks { get; private set; }  //Sample time in 10ns ticks
        public int Frequency { get; private set; }  //Frequency in hz  //TODO Remove this after debugging
        public int FrequencyIdx {  get { return Sample.FrequencyIdx(Frequency); } }
        public double Amplitude;

        public SignificantSample(UInt64 sampleTimeTicks, double frequency, double amplitude) {
            this.SampleTimeTicks = sampleTimeTicks;
            this.Frequency = (int) frequency;
            this.Amplitude = amplitude;
        }
    }
}
