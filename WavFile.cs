using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVCapture
{
    public class WavFile
    {
        private BinaryFileReader binaryFileReader;
        private byte[] audioBuffer = new byte[0];

        public WavFile(string filePath) {
            binaryFileReader = new BinaryFileReader(filePath);
            binaryFileReader.SkipBytes(12);  //Skip over "RIFF"(4), Chunk size (4), "WAVE" (4)

            //Fmt chunk
            Debug.Assert(binaryFileReader.ReadString(4) == "fmt ");  //"fmt "
            var chunkSize = binaryFileReader.ReadInt32();  //Skip over chunk size
            Debug.Assert(binaryFileReader.ReadInt16() == 1);  //wFormatTag == WAVE_FORMAT_PCM
            binaryFileReader.SkipBytes(2);  //Skip nChannels
            SamplesPerSec = binaryFileReader.ReadInt32();  //nSamplesPerSec
            binaryFileReader.SkipBytes(chunkSize - 8);  //Skip nAvgBytesPerSec(4), nBlockAlign(2), wBitsPerSample(2), any other data

            //Skip over non-Data chunk(s)
            var chkId = binaryFileReader.ReadString(4);
            var chkSize = binaryFileReader.ReadInt32();
            while (chkId != "data") {
                binaryFileReader.SkipBytes(chkSize);
                chkId = binaryFileReader.ReadString(4);
                chkSize = binaryFileReader.ReadInt32();
            }

            //File pointer is now at start of raw PCM data buffer
        }

        public int SamplesPerSec { get; private set; }

        public bool NextSample(Int16[] buffer) {
            var bufferLen = buffer.Length;
            if (audioBuffer.Length != bufferLen * 2) {
                audioBuffer = new byte[bufferLen * 2];
            }
            if (binaryFileReader.ReadBytes(audioBuffer)) {
                for (int i = 0; i < buffer.Length; i++) {
                    buffer[i] = BitConverter.ToInt16(audioBuffer, i * 2);
                }
                return true;
            } else {
                return false;
            }
        }

        public void Close() {
            binaryFileReader.Close();
        }
    }
}
