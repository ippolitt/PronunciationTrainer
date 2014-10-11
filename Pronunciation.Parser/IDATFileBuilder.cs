using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    interface IDATFileBuilder
    {
        DataIndex AppendEntity(string entityKey, byte[] data);
        void Flush();
    }

    public class DataIndex
    {
        public long Offset;
        public long Length;

        public string BuildKey()
        {
            return string.Format("{0}|{1}", Offset, Length);
        }

        public static DataIndex Parse(string indexKey)
        {
            string[] index = indexKey.Split('|');
            if (index.Length != 2)
                throw new ArgumentException("Invalid format of data index!");

            return new DataIndex { Offset = long.Parse(index[0]), Length = long.Parse(index[1]) };
        }
    }
}
