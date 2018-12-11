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
 * Similarly, the compression routine was written by CUE in C and adapted to C# by Gamma/SageOfMirrors.
 * Thanks! ¡Muchas gracias!
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

        public static byte[] Compress(EndianBinaryReader reader)
        {
            List<byte> data_buffer = new List<byte>();
            List<byte> flag_buffer = new List<byte>();

            // These are all copied + translated from CUE's code.
            uint times = 0, sum = 0;
            uint most_times = 0, best_sum = 0;
            long update = 0;
            long src_offset = 0, data_offset = 0, end = 0, nor_offset = 0, normal_offset = 0;

            uint method;
            uint best_method;

            byte[] normal = new byte[256];

            end = reader.BaseStream.Length;

            while (reader.BaseStream.Position < end)
            {
                most_times = 0;

                #region DestCopy
                if (src_offset >= reader.BaseStream.Position + 0xFFF)
                    sum = 0xFFF;
                else
                    sum = (uint)(src_offset - reader.BaseStream.Position);

                for ( ; sum > 0; sum--)
                {
                    for (times = 0; times < 0xF; times++)
                    {
                        if (src_offset + times >= end || times >= sum)
                            break;
                        if (reader.ReadByteAt(src_offset + times) != reader.ReadByteAt(src_offset + times - sum))
                            break;
                    }

                    if ((times >= most_times) && times > 2)
                    {
                        most_times = times;
                        best_sum = sum;
                        best_method = (uint)CopyMode.DestCopy;
                    }
                }
                #endregion

                #region InPlaceCopy
                for (times = 1; times < 0x7F; times++)
                {
                    if (src_offset + times >= end - 1)
                        break;
                    if (reader.ReadByteAt(src_offset + times) != (reader.ReadByteAt(src_offset)))
                        break;
                }

                if ((times >= most_times) && times > 2)
                {
                    most_times = times;
                    best_method = (uint)CopyMode.InPlaceCopy;
                }
                #endregion

                #region RunCopy and DirectCopy
                if (most_times == 0)
                {
                    normal[nor_offset++] = reader.ReadByteAt(src_offset++);
                    update = (nor_offset == normal_offset + 0xFF) ? 1 : 0;
                }
                else
                {
                    update = (nor_offset > normal_offset) ? 1 : 0;
                }

                if (update > 0)
                {
                    times = (uint)(nor_offset - normal_offset);

                    //method <<= 2;
                }
                #endregion

            }

            return new byte[0];
        }
    }
}
