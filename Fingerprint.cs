using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    /// <summary>
    /// Encapsulate hash of (anchor and target frequencies, plus time difference between them)
    /// and sample time of anchor sample.
    /// </summary>
    class Fingerprint
    {
        public int Hash { get; private set; }  //Hash of anchor and target frequencies, combined with time difference between samples
        public long SampleTime { get; private set; }  //Time of anchor sample
        public int EpisodeId { get; private set; }  //Episode ID of show/episode in database

        public int Freq1;  //TODO
        public double Amp1;  //TODO
        public int Freq2;  //TODO
        public double Amp2;  //TODO
        public int Offset;  //TODO

        public Fingerprint(int episodeId, Sample sample1, Sample sample2) {
            EpisodeId = episodeId;
            SampleTime = sample1.SampleTime;
            int sampleTimeOffset = (int) (sample2.SampleTime - sample1.SampleTime);
            Hash = ComputeHash(sample1, sample2, sampleTimeOffset);

            Freq1 = sample1.Frequency;
            Amp1 = sample1.Amplitude;
            Freq2 = sample2.Frequency;
            Amp2 = sample2.Amplitude;
            Offset = sampleTimeOffset;
        }

        private int ComputeHash(Sample sample1, Sample sample2, int sampleTimeOffset) {
            return ((sample1.Frequency << 16) + (sample2.Frequency << 8)) + sampleTimeOffset;
            //return ((sample1.Frequency - sample2.Frequency) << 16) + sampleTimeOffset;
        }
    }
}
