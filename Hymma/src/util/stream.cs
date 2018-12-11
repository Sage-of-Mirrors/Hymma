using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;

namespace Hymma.Util
{
    public static class Stream_Util
    {
        public static void Skip_Padding(EndianBinaryReader reader, int test, int padValue)
        {
            // Pad up to a 32 byte alignment
            // Formula: (x + (n-1)) & ~(n-1)
            long nextAligned = (test + (padValue - 1)) & ~(padValue - 1);

            long delta = nextAligned - test;
            //reader.BaseStream.Position = reader.BaseStream.Position;
            for (int i = 0; i < delta; i++)
            {
                reader.SkipByte();
            }
        }
    }
}
