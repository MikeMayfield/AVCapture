using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    /// <summary>
    /// Group of related fingerprints with the same hash value
    /// </summary>
    class FingerprintGroup
    {
        public UInt64 Hash;
        public List<Fingerprint> Fingerprints = new List<Fingerprint>();

        public FingerprintGroup(UInt64 fingerPrintHash) {
            this.Hash = fingerPrintHash;
        }

        public void AddFingerprint(Fingerprint fingerprint) {
            Fingerprints.Add(fingerprint);
        }

        public void AppendFingerprints(List<Fingerprint> fingerprintGroup) {
            Fingerprints.AddRange(fingerprintGroup);
        }
    }
}
