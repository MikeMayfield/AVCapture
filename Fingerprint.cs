using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    class Fingerprint
    {
        public int Hash { get; private set; }
        public long SampleTime { get; private set; }
        public int EpisodeId { get; private set; }
        public int Freq1;  //TODO
        public int Freq2;  //TODO
        public int Offset;  //TODO

        public Fingerprint(int episodeId, Sample sample1, Sample sample2) {
            EpisodeId = episodeId;
            SampleTime = sample1.SampleTime;
            int sampleTimeOffset = (int) (sample2.SampleTime - sample1.SampleTime);
            Hash = ComputeHash(sample1, sample2, sampleTimeOffset);
            Freq1 = sample1.Frequency;
            Freq2 = sample2.Frequency;
            Offset = sampleTimeOffset;
        }

        private int ComputeHash(Sample sample1, Sample sample2, int sampleTimeOffset) {
            return (sample1.Frequency << 16) | (sample2.Frequency << 2) | sampleTimeOffset;
        }
    }
}
