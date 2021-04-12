using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    class SignificantSample
    {
        public long SampleTime { get; private set; }  //Sample time in 10ns ticks
        public int Frequency { get; private set; }  //Frequency in hz
        public int FrequencyIdx {  get { return Sample.FrequencyIdx(Frequency); } }
        public double Amplitude;  //TODO REMOVE

        public SignificantSample(long sampleTime, double frequency, double amplitude) {
            this.SampleTime = sampleTime;
            this.Frequency = (int) frequency;
            this.Amplitude = amplitude;  //TODO REMOVE
        }
    }
}
