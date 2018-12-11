using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using GameFormatReader.Common;

namespace Hymma.Image.Animation
{
    public class Anim_Frame
    {
        private Point m_Sprite_Sheet_Coord;
        private Point m_World_Position;

        private int unknown_int_1;

        private short sprite_width;
        private short sprite_height;
        private float sprite_rotation;

        private short unknown_short_1;

        private short alpha;

        private bool flip_vertically;
        private bool flip_horizontally;

        private short unknown_short_2;

        private short m_X_Scale;
        private short m_Y_Scale;

        private byte unknown_byte_1;

        private byte blend_mode;

        private short unknown_short_3;

        public Anim_Frame(EndianBinaryReader reader)
        {
            m_Sprite_Sheet_Coord = new Point(reader.ReadInt16(), reader.ReadInt16());
            m_World_Position = new Point(reader.ReadInt16(), reader.ReadInt16());

            unknown_int_1 = reader.ReadInt32();

            sprite_width = reader.ReadInt16();
            sprite_height = reader.ReadInt16();

            sprite_rotation = reader.ReadUInt16();

            unknown_short_1 = reader.ReadInt16();

            ushort alpha_flags = reader.ReadUInt16();

            alpha = (short)((alpha_flags & 0xFFF0) >> 4);

            flip_vertically = (alpha_flags & 0x1) == 1 ? true : false;
            flip_horizontally = (alpha_flags & 0x2) == 2 ? true : false;

            unknown_short_2 = reader.ReadInt16();

            m_X_Scale = reader.ReadInt16();
            m_Y_Scale = reader.ReadInt16();

            unknown_byte_1 = reader.ReadByte();
            blend_mode = reader.ReadByte();

            unknown_short_3 = reader.ReadInt16();
        }

        public void Append_Sprite(TIM sprite_sheet, Bitmap dest)
        {
            if (alpha != 3072)
                return;

            if (sprite_width > sprite_sheet.Image.Width)
                sprite_width = (short)sprite_sheet.Image.Width;
            Bitmap sprite = sprite_sheet.Image.Clone(new Rectangle(m_Sprite_Sheet_Coord, new Size(sprite_width, sprite_height)), sprite_sheet.Image.PixelFormat);
            sprite.MakeTransparent(Color.FromArgb(0, 0, 0, 0));

            if (flip_vertically)
            {
                sprite.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }

            if (flip_horizontally)
            {
                sprite.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }

            Bitmap tempBitmap = new Bitmap(dest.Width, dest.Height);

            using (Graphics grD = Graphics.FromImage(dest))
            {
                grD.DrawImage(sprite, m_World_Position);
            }
        }
    }
}
