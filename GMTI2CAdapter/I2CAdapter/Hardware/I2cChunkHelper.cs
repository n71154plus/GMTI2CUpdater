using System;

namespace GMTI2CUpdater.I2CAdapter.Hardware
{
    internal static class I2cChunkHelper
    {
        public static void WriteChunks(int dataLength, int chunkSize, Action<int, int> writeChunk)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive.");

            int offset = 0;
            while (offset < dataLength)
            {
                int remaining = dataLength - offset;
                int chunkLen = Math.Min(chunkSize, remaining);

                writeChunk(offset, chunkLen);
                offset += chunkLen;
            }
        }

        public static void ReadChunks(int length, int chunkSize, Func<int, int, bool, byte[]> readChunk, byte[] destination)
        {
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive.");

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destination.Length < length)
                throw new ArgumentException("Destination buffer is smaller than requested length.", nameof(destination));

            int offset = 0;
            while (offset < length)
            {
                int remaining = length - offset;
                int chunkLen = Math.Min(chunkSize, remaining);
                bool isLast = offset + chunkLen >= length;

                byte[] chunkData = readChunk(offset, chunkLen, isLast) ??
                    throw new InvalidOperationException("Chunk reader returned null data.");

                if (chunkData.Length < chunkLen)
                    throw new InvalidOperationException("Chunk reader returned insufficient data.");

                Array.Copy(chunkData, 0, destination, offset, chunkLen);
                offset += chunkLen;
            }
        }
    }
}
