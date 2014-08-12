using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace Pronunciation.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("{0:00}-{1:00}", 3, 4);
                int number;
                bool res = int.TryParse(null, out number);

                int k = "".CompareTo(null);

                //Image i = Image.FromFile(@"D:\WORK\NET\PronunciationTrainer\Pronunciation.Trainer\Resources\AudioWaveform.png");
                //Console.WriteLine(i.HorizontalResolution + " " + i.VerticalResolution);
                //Console.WriteLine(i.Width + " " + i.Height);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("Finished");
                Console.ReadLine();
            }
        }
    }
}
