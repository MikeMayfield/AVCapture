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
        public UInt32 Hash { get; private set; }  //Hash of anchor and target frequencies, combined with time difference between samples
        public UInt32 EpisodeId { get; private set; }  //Episode ID of show/episode in database  //TODO Use UInt32, since we really only need ~26 bits
        public UInt64 SampleTimeTicks { get; private set; }  //Time of anchor sample

        //public int Freq1;  //TODO REMOVE after debugging
        //public double Amp1;  //TODO
        //public int Freq2;  //TODO
        //public double Amp2;  //TODO
        //public UInt64 Offset;  //TODO

        public Fingerprint(UInt32 episodeId, SignificantSample significantSample1, SignificantSample significantSample2) {
            EpisodeId = episodeId;
            SampleTimeTicks = significantSample1.SampleTimeTicks;
            Hash = ComputeHash(significantSample1, significantSample2);

            //Freq1 = significantSample1.Frequency;
            //Amp1 = significantSample1.Amplitude;
            //Freq2 = significantSample2.Frequency;
            //Amp2 = significantSample2.Amplitude;
            //Offset = significantSample2.SampleTimeTicks - significantSample1.SampleTimeTicks;
        }

        [JsonConstructor]
        public Fingerprint(int Freq1, double Amp1, int Freq2, double Amp2, UInt64 Offset, UInt32 Hash, UInt64 SampleTimeTicks, UInt32 EpisodeId) {
            //this.Freq1 = Freq1;
            //this.Amp1 = Amp1;
            //this.Freq2 = Freq2;
            //this.Amp2 = Amp2;
            //this.Offset = Offset;
            this.Hash = Hash;
            this.SampleTimeTicks = SampleTimeTicks;
            this.EpisodeId = EpisodeId;
        }


        private UInt32 ComputeHash(SignificantSample sample1, SignificantSample sample2) {
            UInt32 deltaTimeBetweenSamples = (UInt32)(sample2.SampleTimeTicks - sample1.SampleTimeTicks);
            return ((UInt32) sample1.FrequencyIdx << 28) | ((UInt32) sample2.FrequencyIdx << 24) | ((deltaTimeBetweenSamples / 232199) & 0xFFFFFF);  //232199 is ticks per sample
        }
    }
}
