using Newtonsoft.Json;
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
    class Fingerprint {
        public int Hash { get; private set; }  //Hash of anchor and target frequencies, combined with time difference between samples
        public long SampleTime { get; private set; }  //Time of anchor sample
        public int EpisodeId { get; private set; }  //Episode ID of show/episode in database

        public int Freq1;  //TODO REMOVE after debugging
        public double Amp1;  //TODO
        public int Freq2;  //TODO
        public double Amp2;  //TODO
        public int Offset;  //TODO

        public Fingerprint(int episodeId, SignificantSample sample1, SignificantSample sample2) {
            EpisodeId = episodeId;
            SampleTime = sample1.SampleTime;
            int sampleTimeDelta = (int) (sample2.SampleTime - sample1.SampleTime);
            Hash = ComputeHash(sample1, sample2, sampleTimeDelta);

            Freq1 = sample1.Frequency;
            Amp1 = sample1.Amplitude;
            Freq2 = sample2.Frequency;
            Amp2 = sample2.Amplitude;
            Offset = sampleTimeDelta;
        }

        [JsonConstructor]
        public Fingerprint(int Freq1, double Amp1, int Freq2, double Amp2, int Offset, int Hash, long SampleTime, int EpisodeId) {
            this.Freq1 = Freq1;
            this.Amp1 = Amp1;
            this.Freq2 = Freq2;
            this.Amp2 = Amp2;
            this.Offset = Offset;
            this.Hash = Hash;
            this.SampleTime = SampleTime;
            this.EpisodeId = EpisodeId;
        }


        private int ComputeHash(SignificantSample sample1, SignificantSample sample2, int sampleTimeOffset) {
            return (sample1.FrequencyIdx << 30) | (sample2.FrequencyIdx << 28) | (sampleTimeOffset & 0xFFFFFFF);
        }
    }
}
