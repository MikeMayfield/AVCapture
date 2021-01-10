using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AVCapture
{
    public class BinaryFileReader
    {
        private FileStream fileStream;
        private byte[] twoByteBuffer = new byte[2];
        private byte[] fourByteBuffer = new byte[4];

        public BinaryFileReader(string filePath) {
            fileStream = File.OpenRead(filePath);
        }

        ~BinaryFileReader() {
            Close();
        }

        public void SkipBytes(int byteCount) {
            fileStream.Seek(byteCount, SeekOrigin.Current);
        }

        public bool ReadBytes(byte[] buffer) {
            var bufferLen = buffer.Length;
            var bytesReadCnt = fileStream.Read(buffer, 0, buffer.Length);
            return (bytesReadCnt == bufferLen);
        }

        public String ReadString(int length) {
            var buffer = new byte[length];
            ReadBytes(buffer);
            return Encoding.Default.GetString(buffer);
        }

        public Int32 ReadInt32() {
            ReadBytes(fourByteBuffer);
            return BitConverter.ToInt32(fourByteBuffer, 0);
        }

        public Int32 ReadInt16() {
            ReadBytes(twoByteBuffer);
            return BitConverter.ToInt16(twoByteBuffer, 0);
        }

        public void Close() {
            if (fileStream != null) {
                fileStream.Close();
                fileStream.Dispose();
            }

            fileStream = null;
        }
    }
}
