using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Pronunciation.Trainer
{
    public class ConnectionStrings
    {
        private const string _lpdDatabaseKey = "LPD";

        public string LPD { get; private set; }

        public ConnectionStrings()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[_lpdDatabaseKey];
            LPD = settings.ConnectionString;
        }
    }
}
