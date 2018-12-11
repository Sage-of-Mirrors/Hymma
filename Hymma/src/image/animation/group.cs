using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.Drawing;

namespace Hymma.Image.Animation
{
    public class Anim_Group
    {
        public List<Anim_Frame> Frames { get; private set; }
        public Bitmap Group_Image { get { return m_Group_Image; } }
        public int Anim_Length { get { return m_Anim_Length; } }

        private Bitmap m_Group_Image;

        private int m_Frame_Count;
        private int m_Anim_Length;
        private int unknown_1;
        private int unknown_2;

        public Anim_Group(EndianBinaryReader reader, List<Anim_Frame>[] master_frame_list)
        {
            Frames = new List<Anim_Frame>();

            int frame_count_index = reader.ReadInt16();
            m_Anim_Length = reader.ReadInt16();
            unknown_1 = reader.ReadInt16();
            unknown_2 = reader.ReadInt32();

            Frames.AddRange(master_frame_list[frame_count_index]);
        }

        public void Load_Frames(EndianBinaryReader reader)
        {
            for (int i = 0; i < m_Frame_Count; i++)
            {
                Anim_Frame frame = new Anim_Frame(reader);
                Frames.Add(frame);
            }
        }

        public void Generate_Frame(TIM sprite_sheet)
        {
            m_Group_Image = new Bitmap(sprite_sheet.Image.Width, sprite_sheet.Image.Height);

            foreach (Anim_Frame f in Frames)
            {
                f.Append_Sprite(sprite_sheet, m_Group_Image);
            }
        }
    }
}
