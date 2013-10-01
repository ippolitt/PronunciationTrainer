using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Pronunciation.Core.Parsers;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for DataImporter.xaml
    /// </summary>
    public partial class DataImporter : UserControl
    {
        public DataImporter()
        {
            InitializeComponent();
        }

        private void btnImportTopics_Click(object sender, RoutedEventArgs e)
        {
            TopicParser parser = new TopicParser(txtTopicsFile.Text);
            parser.ImportTopics();

            MessageBox.Show("Import succesfully completed!");
        }

//with orderedrows as 
//(SELECT TopicId,
//5*ROW_NUMBER() OVER (PARTITION BY BookId ORDER BY TopicId) as sort
//FROM     Topic)
//update Topic
//set ordinal = rr .sort
//from topic tp 
//inner join orderedrows  rr on tp.TopicId = rr.TopicId
    }
}
