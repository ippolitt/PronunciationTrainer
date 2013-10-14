using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Pronunciation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Image i = Image.FromFile(@"D:\WORK\Images\temp\player_stop.png");
                Console.WriteLine(i.HorizontalResolution + " " + i.VerticalResolution);
                Console.WriteLine(i.Width + " " + i.Height);

                Image i2 = Image.FromFile(@"D:\WORK\Images\temp\Stop4.png");
                Console.WriteLine(i2.HorizontalResolution + " " + i2.VerticalResolution);
                Console.WriteLine(i2.Width + " " + i2.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
