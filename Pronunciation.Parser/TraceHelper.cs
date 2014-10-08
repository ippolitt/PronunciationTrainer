using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class TraceHelper
    {
        public static void WriteToFile(string fileName, IEnumerable<string> items)
        {
            File.WriteAllText(string.Format(@"D:\{0}.txt", fileName), string.Join(Environment.NewLine, items.OrderBy(x => x)));
        }
    }
}
