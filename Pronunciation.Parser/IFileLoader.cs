using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    interface IFileLoader
    {
        bool FlushCache();
        bool LoadCache(string cacheKey);
        void ClearCache();
        string GetBase64Content(string fileKey);
        byte[] GetRawData(string fileKey);
    }
}
