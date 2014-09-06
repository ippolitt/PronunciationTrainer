using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlServerCe;

namespace Pronunciation.Parser
{
    class DataRecordWrapper
    {
        private readonly SqlCeResultSet _resultSet;
        private readonly SqlCeUpdatableRecord _record;
        
        private bool IsRecord
        {
            get { return _record != null; }
        }

        public DataRecordWrapper(SqlCeResultSet resultSet)
        {
            _resultSet = resultSet;
        }

        public DataRecordWrapper(SqlCeUpdatableRecord record)
        {
            _record = record;
        }

        public object this[string name] 
        {
            get 
            { 
                return IsRecord ? _record[name] : _resultSet[name]; 
            }
            set 
            { 
                if (IsRecord)
                {
                    _record[name] = value;
                }
                else
                {
                    _resultSet.SetValue(_resultSet.GetOrdinal(name), value); 
                }
            }
        }

        public object this[int index]
        {
            get
            {
                return IsRecord ? _record[index] : _resultSet[index];
            }
            set
            {
                if (IsRecord)
                {
                    _record[index] = value;
                }
                else
                {
                    _resultSet.SetValue(index, value);
                }
            }
        }
    }
}
