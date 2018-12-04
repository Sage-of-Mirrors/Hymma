using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.Diagnostics;

/*
 * The decompression routine below was written by asmodean in C++ and adapted to C# by Gamma/SageOfMirrors.
 * Asmodean's original header follows:
 * 
 * // coded by asmodean
 * // contact: 
 * //   web:   http://asmodean.reverse.net
 * //   email: asmodean [at] hush.com
 * //   irc:   asmodean on efnet (irc.efnet.net)
 * 
 */

namespace Hymma.Util
{
    public static class LZR
    {
        public enum CopyMode
        {
            DirectCopy,
            RunCopy,
            InPlaceCopy,
            DestCopy
        }

        public static EndianBinaryReader Decompress(EndianBinaryReader reader)
        {
            Debug.Assert(reader.ReadInt32() == 5397068, "LZR magic was incorrect!");

            int flag_bit_count = reader.ReadInt32();
            int compressed_data_size = reader.ReadInt32();
            int original_data_size = reader.ReadInt32();

            byte[] flags = reader.ReadBytes((flag_bit_count + 7) / 8);

            int flag_offset = 0;
            int dest_offset = 0;
            byte[] dest = new byte[original_data_size];

            while (dest_offset < original_data_size)
            {
                byte flag = flags[flag_offset++];

                for (int i = 0; i < 4 && dest_offset < original_data_size; i++)
                {
                    switch ((CopyMode)(flag >> 6))
                    {
                        // Copy one byte from the source directly to the destination
                        case CopyMode.DirectCopy:
                            dest[dest_offset++] = reader.ReadByte();
                            break;
                        // Read a byte from the source. Copy that many bytes from source to destination.
                        case CopyMode.RunCopy:
                            int copy_length = reader.ReadByte();

                            for (int c = copy_length; c > 0; c--)
                            {
                                dest[dest_offset++] = reader.ReadByte();
                            }

                            break;
                        // Read a byte from the source. Copy that many *copies of the next byte*
                        // from source to the destination.
                        case CopyMode.InPlaceCopy:
                            int copy_in_place_length = reader.ReadByte();

                            for (int c = copy_in_place_length; c > 0; c--)
                            {
                                dest[dest_offset++] = reader.PeekReadByte();
                            }

                            reader.SkipByte();

                            break;
                        // Read two bytes from the source. Calculate an offset and a count. Copy count-many bytes
                        // to the destination from the current destination offset minus the calculated offset.
                        case CopyMode.DestCopy:
                            uint p = (uint)(reader.ReadByte() << 4);
                            uint n = reader.ReadByte();

                            p |= n >> 4;
                            n &= 0xF;

                            for (uint c = n; c > 0; c--)
                            {
                                dest[dest_offset] = dest[dest_offset - p];
                                dest_offset++;
                            }
                            break;
                    }

                    flag <<= 2;
                }
            }

            return new EndianBinaryReader(dest, Endian.Little);
        }

        public static byte[] Compress()
        {
            return new byte[0];
        }
    }
}
