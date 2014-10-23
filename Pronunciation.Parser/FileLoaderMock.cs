using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Parser
{
    class FileLoaderMock : IFileLoader
    {
        public string GetBase64Content(string fileKey)
        {
            return "t";
        }

        public byte[] GetRawData(string fileKey)
        {
            return new byte[1];
        }

        public void Dispose()
        {
        }
    }
}
