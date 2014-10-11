using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    interface IFileLoader : IDisposable
    {
        string GetBase64Content(string fileKey);
        byte[] GetRawData(string fileKey);
    }
}
