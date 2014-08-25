using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer.ValueConverters
{
    public class StateToStringConverter : IValueConverter
    {
        public string TrueStringTemplate { get; set; }
        public string FalseStringTemplate { get; set; }
        public IStateToStringArgsProvider FormatArgsProvider { get; set; } 

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (FormatArgsProvider == null)
            {
                return (bool)value ? TrueStringTemplate : FalseStringTemplate;
            }
            else
            {
                return (bool)value 
                    ? string.Format(TrueStringTemplate, FormatArgsProvider.GetTrueStringFormatArgs())
                    : string.Format(FalseStringTemplate, FormatArgsProvider.GetFalseStringFormatArgs());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
