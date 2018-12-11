using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hymma.Image;
using Hymma.Image.Animation;

namespace Ciel
{
    class Program
    {
        static void Main(string[] args)
        {
            TIM tim = new TIM(@"C:\Users\Dylan\Downloads\exar2\fpack_extract\anm\M027A\M027A_12.TIM.lzr");
            HSE_Animation hse = new HSE_Animation(@"C:\Users\Dylan\Downloads\exar2\fpack_extract\anm\M027A\M027A_01WA.hse.lzr");
            hse.Generate_Animation(tim);
            hse.Export_Animation(@"D:\Github\Hymma\anim.gif");
            hse.Export_Frames(@"D:\Github\Hymma\");
        }
    }
}
