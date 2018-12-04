using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using Hymma.Util;

namespace Hymma.Image
{
    public class TIM
    {
        public enum BitsPerPixel
        {
            bpp_4,
            bpp_8,
            bpp_16,
            bpp_24
        }

        public Bitmap Image
        {
            get { return m_Image; }
            private set { m_Image = value; }
        }

        private Bitmap m_Image;

        public TIM(string file_name)
        {
            using (FileStream strm = new FileStream(file_name, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(strm, Endian.Little);

                int test = reader.PeekReadInt32();

                if (reader.PeekReadInt32() == 5397068)
                {
                    reader = LZR.Decompress(reader);
                }

                Load_TIM(reader);
            }
        }

        public TIM(byte[] stream)
        {
            EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Little);
            Load_TIM(reader);
        }

        private void Load_TIM(EndianBinaryReader reader)
        {
            Debug.Assert(reader.ReadInt32() == 16, "TIM file did not have the correct magic!");

            int flags = reader.ReadInt32();

            BitsPerPixel bits_per_pixel = (BitsPerPixel)(flags & 3);
            bool has_clut = (flags & 8) == 0 ? false : true;

            switch (bits_per_pixel)
            {
                case BitsPerPixel.bpp_4:
                    if (has_clut)
                    {
                        Load_4_BPP_Image_CLUT(reader);
                    }
                    else
                    {
                        Load_4_BPP_Image_No_CLUT(reader);
                    }
                    break;
                case BitsPerPixel.bpp_8:
                    if (has_clut)
                    {
                        Load_8_BPP_Image_CLUT(reader);
                    }
                    else
                    {
                        Load_4_BPP_Image_No_CLUT(reader);
                    }
                    break;
                case BitsPerPixel.bpp_16:
                    break;
                case BitsPerPixel.bpp_24:
                    break;
                default:
                    Debug.Assert(false, "Invalid bpp type " + bits_per_pixel + "!");
                    break;
            }
        }

        private void Load_4_BPP_Image_CLUT(EndianBinaryReader reader)
        {
            int clut_size = reader.ReadInt32() - 12; // The header is 12 bytes long

            short clut_offset_x = reader.ReadInt16();
            short clut_offset_y = reader.ReadInt16();

            Debug.Assert(reader.ReadInt16() == 16, "CLUT color count wasn't 16! (4bpp image)");

            short clut_count = reader.ReadInt16();

            List<Color>[] clut_colors = new List<Color>[clut_count];
            for (int i = 0; i < clut_count; i++)
            {
                clut_colors[i] = new List<Color>();

                for (int j = 0; j < 16; j++)
                {
                    short color = reader.ReadInt16();

                    int red = (color & 0x1F) * 8;
                    int green = ((color & 0x3E0) >> 5) * 8;
                    int blue = ((color & 0x7C00) >> 10) * 8;

                    clut_colors[i].Add(Color.FromArgb(red, green, blue));
                }
            }

            int image_size = reader.ReadInt32() - 12; // The header is 12 bytes long

            short image_offset_x = reader.ReadInt16();
            short image_offset_y = reader.ReadInt16();

            int image_width = reader.ReadInt16() * 4; // Multiply the stored value by 4 to get actual width
            int image_height = reader.ReadInt16();

            m_Image = new Bitmap(image_width, image_height);

            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x += 2)
                {
                    byte src = reader.ReadByte();

                    int pix_2 = (src & 0xF0) >> 4;
                    int pix_1 = src & 0xF;

                    m_Image.SetPixel(x, y, clut_colors[0][pix_1]);
                    m_Image.SetPixel(x + 1, y, clut_colors[0][pix_2]);
                }
            }
        }

        private void Load_4_BPP_Image_No_CLUT(EndianBinaryReader reader)
        {

        }

        private void Load_8_BPP_Image_CLUT(EndianBinaryReader reader)
        {
            int clut_size = reader.ReadInt32() - 12; // The header is 12 bytes long

            short clut_offset_x = reader.ReadInt16();
            short clut_offset_y = reader.ReadInt16();

            Debug.Assert(reader.ReadInt16() == 256, "CLUT color count wasn't 256! (8bpp image)");

            short clut_count = reader.ReadInt16();

            List<Color>[] clut_colors = new List<Color>[clut_count];
            for (int i = 0; i < clut_count; i++)
            {
                clut_colors[i] = new List<Color>();

                for (int j = 0; j < 256; j++)
                {
                    short color = reader.ReadInt16();

                    int red = (color & 0x1F) * 8;
                    int green = ((color & 0x3E0) >> 5) * 8;
                    int blue = ((color & 0x7C00) >> 10) * 8;

                    clut_colors[i].Add(Color.FromArgb(red, green, blue));
                }
            }

            int image_size = reader.ReadInt32() - 12; // The header is 12 bytes long

            short image_offset_x = reader.ReadInt16();
            short image_offset_y = reader.ReadInt16();

            int image_width = reader.ReadInt16() * 2; // Multiply the stored value by 2 to get actual width
            int image_height = reader.ReadInt16();

            m_Image = new Bitmap(image_width, image_height);

            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    byte src = reader.ReadByte();

                    m_Image.SetPixel(x, y, clut_colors[0][src]);
                }
            }

            m_Image.Save(@"C:\Users\Dylan\Downloads\exar2\fpack_extract\anm\M054A\test.png");
        }
    }
}
