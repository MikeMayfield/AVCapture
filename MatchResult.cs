using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    /// <summary>
    /// Result of a captured file search
    /// </summary>
    class MatchResult
    {
        public UInt64 EpisodeId;  //Episode ID of identified episode
        public Int64 SampleTimeDeltaTicks;   //Difference between start of capture file time and start time of matching fingerprints in matched episode
        public UInt32 MatchCount;  //Number of matches at time offset
        public double ConfidenceRatio;  //Confidence ratio

        public MatchResult(UInt64 episodeId, Int64 sampleTimeDeltaTicks, UInt32 matchCount, double confidenceRatio = 0d) {
            EpisodeId = episodeId;
            SampleTimeDeltaTicks = sampleTimeDeltaTicks;
            MatchCount = matchCount;
            ConfidenceRatio = confidenceRatio;
        }
    }
}
