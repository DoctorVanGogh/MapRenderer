using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MemorySize
{
    public static class MemorySizeUtil
    {
        // NOTE: coud use constants but value never changes and better preformance with a cast
        public static double GetMBs(this long bytes) => bytes / 1048576L; // 1024 * 1024
        public static double GetMBs(this int bytes) => bytes / 1048576; // 1024 * 1024
        public static double GetMBs(this uint bytes) => bytes / 1048576u; // 1024 * 1024

        // NOTE: uint should be good enough but going long just in case
        public static long GetTextureSize(int side, int colorDepth)
        {
            int colorBytes = colorDepth / 8; // bits -> bytes
            return side * side * colorBytes;
        }
    }
}
