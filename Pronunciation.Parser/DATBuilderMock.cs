using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    class DATBuilderMock : IDATFileBuilder
    {
        public DataIndex AppendEntity(string entityKey, byte[] data)
        {
            return new DataIndex { Offset = 1, Length = 2 };
        }

        public void Flush()
        {
        }
    }
}
