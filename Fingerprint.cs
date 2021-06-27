﻿using Newtonsoft.Json;
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
        public long Hash { get; private set; }  //Hash of anchor and target frequencies, combined with time difference between samples
        public long SampleTimeTicks { get; private set; }  //Time of anchor sample
        public int EpisodeId { get; private set; }  //Episode ID of show/episode in database

        public int Freq1;  //TODO REMOVE after debugging
        public double Amp1;  //TODO
        public int Freq2;  //TODO
        public double Amp2;  //TODO
        public long Offset;  //TODO

        public Fingerprint(int episodeId, SignificantSample significantSample1, SignificantSample significantSample2) {
            EpisodeId = episodeId;
            SampleTimeTicks = significantSample1.SampleTimeTicks;
            var sampleTimeDelta = significantSample2.SampleTimeTicks - significantSample1.SampleTimeTicks;
            Hash = ComputeHash(significantSample1, significantSample2, sampleTimeDelta);

            Freq1 = significantSample1.Frequency;
            Amp1 = significantSample1.Amplitude;
            Freq2 = significantSample2.Frequency;
            Amp2 = significantSample2.Amplitude;
            Offset = sampleTimeDelta;
        }

        [JsonConstructor]
        public Fingerprint(int Freq1, double Amp1, int Freq2, double Amp2, long Offset, long Hash, long SampleTime, int EpisodeId) {
            this.Freq1 = Freq1;
            this.Amp1 = Amp1;
            this.Freq2 = Freq2;
            this.Amp2 = Amp2;
            this.Offset = Offset;
            this.Hash = Hash;
            this.SampleTimeTicks = SampleTime;
            this.EpisodeId = EpisodeId;
        }


        private long ComputeHash(SignificantSample sample1, SignificantSample sample2, long sampleTimeOffset) {
            //return (sample1.FrequencyIdx << 24) | (sample2.FrequencyIdx << 20) | (sampleTimeOffset & 0xFFFFF);
            return (long) (sampleTimeOffset + (long)sample1.FrequencyIdx * 10_000_000_000L + (long) sample2.FrequencyIdx * 1_000_000_000L);  //TODO
        }
    }
}
