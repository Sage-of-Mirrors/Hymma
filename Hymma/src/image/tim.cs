using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
            set { m_Image = value; }
        }

        private Bitmap m_Image;

        public TIM() { }

        public TIM(string file_name)
        {
            // Load a png. Simple!
            if (Path.GetExtension(file_name) == ".png")
            {
                m_Image = new Bitmap(file_name);
                return;
            }

            // Need to load a TIM.
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

        public TIM(Bitmap bmp)
        {
            m_Image = bmp;
        }

        #region Loading
        private void Load_TIM(EndianBinaryReader reader)
        {
            Debug.Assert(reader.ReadInt32() == 16, "TIM file did not have the correct magic!");

            int flags = reader.ReadInt32();

            BitsPerPixel bits_per_pixel = (BitsPerPixel)(flags & 3);
            bool has_clut = (flags & 8) == 0 ? false : true;

            if (has_clut)
            {
                Load_TIM_With_CLUT(reader, bits_per_pixel);
            }
            else
            {
                Load_TIM_No_CLUT(reader, bits_per_pixel);
            }
        }

        private void Load_TIM_With_CLUT(EndianBinaryReader reader, BitsPerPixel bpp)
        {
            PixelFormat pix_fmt = TIM_bpp_To_PixelFormat(bpp);

            int clut_size = reader.ReadInt32();
            short clut_origin_x = reader.ReadInt16();
            short clut_origin_y = reader.ReadInt16();

            int color_count = reader.ReadInt16();
            int pal_count = reader.ReadInt16();

            List<Color>[] palettes = new List<Color>[pal_count];

            for (int i = 0; i < pal_count; i++)
            {
                palettes[i] = Load_CLUT(reader, color_count);
            }

            int image_index = 0;
            float width_modifier = Get_Width_Modifier(bpp);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int image_size = reader.ReadInt32();
                short image_origin_x = reader.ReadInt16();
                short image_origin_y = reader.ReadInt16();

                int image_width = reader.ReadInt16();
                int image_height = reader.ReadInt16();

                m_Image = new Bitmap((int)(image_width * width_modifier), image_height, pix_fmt);
                ColorPalette pal = m_Image.Palette;

                for (int i = 0; i < color_count; i++)
                {
                    pal.Entries[i] = palettes[image_index][i];
                }

                m_Image.Palette = pal;
                BitmapData img_data = m_Image.LockBits(new Rectangle(0, 0, (int)(image_width * width_modifier), image_height),
                    ImageLockMode.ReadWrite, pix_fmt);

                for (int y = 0; y < image_height; y++)
                {
                    for (int x = 0; x < image_width * 2; x++)
                    {
                        byte src = reader.ReadByte();

                        if (bpp == BitsPerPixel.bpp_4)
                        {
                            byte temp = (byte)((src & 0x0F) << 4);
                            src >>= 4;
                            src |= temp;
                        }

                        System.Runtime.InteropServices.Marshal.WriteByte(img_data.Scan0, (y * img_data.Stride) + x, src);
                    }
                }

                m_Image.UnlockBits(img_data);
                image_index++;
            }
        }

        private void Load_TIM_No_CLUT(EndianBinaryReader reader, BitsPerPixel bpp)
        {
            Debug.Assert(false, "Found a 16 or 24bpp image!");
        }

        private List<Color> Load_CLUT(EndianBinaryReader reader, int count)
        {
            List<Color> pal = new List<Color>();

            for (int j = 0; j < count; j++)
            {
                short color = reader.ReadInt16();

                int red = (color & 0x1F) * 8;
                int green = ((color & 0x3E0) >> 5) * 8;
                int blue = ((color & 0x7C00) >> 10) * 8;

                pal.Add(Color.FromArgb(red, green, blue));
            }

            return pal;
        }

        private PixelFormat TIM_bpp_To_PixelFormat(BitsPerPixel bpp)
        {
            switch (bpp)
            {
                case BitsPerPixel.bpp_4:
                    return PixelFormat.Format4bppIndexed;
                case BitsPerPixel.bpp_8:
                    return PixelFormat.Format8bppIndexed;
                case BitsPerPixel.bpp_16:
                    return PixelFormat.Format16bppRgb555;
                case BitsPerPixel.bpp_24:
                    return PixelFormat.Format24bppRgb;
                default:
                    return PixelFormat.DontCare;
            }
        }

        private float Get_Width_Modifier(BitsPerPixel bpp)
        {
            switch (bpp)
            {
                case BitsPerPixel.bpp_4:
                    return 4.0f;
                case BitsPerPixel.bpp_8:
                    return 2.0f;
                case BitsPerPixel.bpp_16:
                    return 1.0f;
                case BitsPerPixel.bpp_24:
                    return 0.5f;
                default:
                    return 1.0f;
            }
        }
        #endregion

        #region Saving
        public void Save_PNG(string file_name)
        {
            m_Image.Save(file_name);
        }

        public void Save_TIM(string file_name)
        {
            BitsPerPixel bpp = PixelFormat_To_TIM_BPP(m_Image.PixelFormat);

            using (FileStream strm = new FileStream(file_name, FileMode.Create))
            {
                EndianBinaryWriter writer = new EndianBinaryWriter(strm, Endian.Little);
                int color_count = 0;
                int width_modifier = 0;
                List<Color> palette_colors = new List<Color>(m_Image.Palette.Entries);

                writer.Write(16);

                switch (m_Image.PixelFormat)
                {
                    case PixelFormat.Format4bppIndexed:
                        writer.Write(8 | (int)BitsPerPixel.bpp_4);
                        color_count = 16;
                        width_modifier = 4;
                        break;
                    case PixelFormat.Format8bppIndexed:
                        writer.Write(8 | (int)BitsPerPixel.bpp_8);
                        color_count = 256;
                        width_modifier = 2;
                        break;
                    default:
                        return;
                }

                writer.Write(12 + (color_count * 2));
                writer.Write(0);
                writer.Write((short)color_count);
                writer.Write((short)1);

                for (int i = 0; i < m_Image.Palette.Entries.Length; i++)
                {
                    Color col = m_Image.Palette.Entries[i];

                    int final_color = col.R / 8;
                    final_color |= (col.G / 8) << 5;
                    final_color |= (col.B / 8) << 10;

                    writer.Write((short)final_color);
                }

                for (int i = 0; i < color_count - m_Image.Palette.Entries.Length; i++)
                {
                    writer.Write((short)0);
                }

                writer.Write(12 + ((m_Image.Width / 2) * m_Image.Height));
                writer.Write(0);

                writer.Write((short)(m_Image.Width / width_modifier));
                writer.Write((short)m_Image.Height);

                for (int y = 0; y < m_Image.Height; y++)
                {
                    for (int x = 0; x < m_Image.Width; x += 2)
                    {
                        switch (m_Image.PixelFormat)
                        {
                            case PixelFormat.Format4bppIndexed:
                                Color pix4_1 = m_Image.GetPixel(x, y);
                                Color pix4_2 = m_Image.GetPixel(x + 1, y);

                                byte final_pix4 = (byte)palette_colors.IndexOf(pix4_1);
                                final_pix4 |= (byte)((palette_colors.IndexOf(pix4_2)) << 4);

                                writer.Write(final_pix4);
                                break;
                            case PixelFormat.Format8bppIndexed:
                                break;
                        }
                    }
                }
            }
        }

        private BitsPerPixel PixelFormat_To_TIM_BPP(PixelFormat fmt)
        {
            switch (fmt)
            {
                case PixelFormat.Format4bppIndexed:
                    return BitsPerPixel.bpp_4;
                case PixelFormat.Format8bppIndexed:
                    return BitsPerPixel.bpp_8;
                case PixelFormat.Format16bppRgb565:
                    return BitsPerPixel.bpp_16;
                case PixelFormat.Format24bppRgb:
                    return BitsPerPixel.bpp_24;
                default:
                    return BitsPerPixel.bpp_24;
            }
        }
        #endregion
    }
}
