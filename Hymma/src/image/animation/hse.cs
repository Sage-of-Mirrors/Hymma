using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.IO;
using Hymma.Util;

namespace Hymma.Image.Animation
{
    public class HSE_Animation
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public List<Anim_Group> Groups { get; private set; }

        public HSE_Animation(string file_name)
        {
            using (FileStream strm = new FileStream(file_name, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(strm, Endian.Little);

                int test = reader.PeekReadInt32();

                if (reader.PeekReadInt32() == 5397068)
                {
                    reader = LZR.Decompress(reader);
                }

                Load_HSE(reader);
            }
        }

        public HSE_Animation(byte[] stream)
        {

        }

        public void Export_Animation(string file_name)
        {
            GifBitmapEncoder encoder = new GifBitmapEncoder();

            foreach (Anim_Group g in Groups)
            {
                var bmp = g.Group_Image.GetHbitmap();
                var src = Imaging.CreateBitmapSourceFromHBitmap(
                    bmp,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                encoder.Frames.Add(BitmapFrame.Create(src));
                DeleteObject(bmp);
            }

            using (FileStream strm = new FileStream(file_name, FileMode.Create))
            {
                encoder.Save(strm);
            }
        }

        public void Export_Frames(string dest_dir)
        {
            for (int i = 0; i < Groups.Count; i++)
            {
                if (i != 1)
                    continue;

                Groups[i].Group_Image.Save(Path.Combine(dest_dir, $"frame_{ i }.png"));
            }
        }

        private void Load_HSE(EndianBinaryReader reader)
        {
            Debug.Assert(reader.ReadString(3) == "HSE", "HSE animation did not have the correct magic!");

            reader.SkipByte(); // The fourth byte, the one after "HSE", doesn't mean anything. It always matches group_count below

            int frame_count_array_size = reader.ReadInt32();
            int group_count = reader.ReadInt32();

            // We're going to read in the frame counts first
            byte[] frame_counts = new byte[frame_count_array_size];

            // Groups are 10 bytes long
            reader.BaseStream.Seek(group_count * 10, System.IO.SeekOrigin.Current);

            for (int i = 0; i < frame_count_array_size; i++)
            {
                frame_counts[i] = reader.ReadByte();
            }

            Stream_Util.Skip_Padding(reader, frame_count_array_size, 8);

            List<Anim_Frame>[] frame_list = new List<Anim_Frame>[frame_count_array_size];

            for (int i = 0; i < frame_counts.Length; i++)
            {
                frame_list[i] = new List<Anim_Frame>();

                for (int j = 0; j < frame_counts[i]; j++)
                {
                    frame_list[i].Add(new Anim_Frame(reader));
                }
            }

            long frames_base_offset = reader.BaseStream.Position;
            reader.BaseStream.Seek(12, System.IO.SeekOrigin.Begin);

            Groups = new List<Anim_Group>();

            for (int i = 0; i < group_count; i++)
            {
                Anim_Group new_group = new Anim_Group(reader, frame_list);
                Groups.Add(new_group);
            }

            reader.BaseStream.Seek(frames_base_offset, System.IO.SeekOrigin.Begin);

            for (int i = 0; i < group_count; i++)
            {
                Groups[i].Load_Frames(reader);
            }
        }

        public void Generate_Animation(TIM sprite_sheet)
        {
            foreach (Anim_Group g in Groups)
            {
                g.Generate_Frame(sprite_sheet);
            }
        }
    }
}
