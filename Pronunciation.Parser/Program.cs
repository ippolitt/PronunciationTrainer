using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SqlServerCe;
using System.Data;

namespace Pronunciation.Parser
{
    class Program
    {
        private const string HtmlFolderPath = @"D:\LEARN\English\Pronunciation\LPD";
        private const string RootFolderPath = @"..\..\..\";

        private const string DataFolder = @"Data\";
        private const string AnalysisFolder = @"Analysis\";

        private const string SourceFile = DataFolder + "En-En-Longman_Pronunciation.dsl";
        private const string TopWordsFile = DataFolder + "TopWords.txt";
        private const string SoundsFolder = DataFolder + "Sounds";
        private const string SoundsCacheFolder = DataFolder + "SoundsCache";

        private const string NormalizedFile = AnalysisFolder + "Results_Normalize.txt";
        private const string XmlFile = AnalysisFolder + "Results.xml";
        private const string XmlLogFile = AnalysisFolder + "XmlConvert.log";
        private const string HtmlSourceFile = AnalysisFolder + "Results - Final.xml";
        private const string HtmlLogFile = AnalysisFolder + "HtmlConvert.log";

        private const string DbConnectionString = @"Data Source=D:\LEARN\English\Pronunciation\Trainer\Database\LPD.sdf;Max Database Size=4000;";

        static void Main(string[] args)
        {
            try
            {
                //UploadFiles();
                //UploadFilesBulk();
                //TestUpload();
                //CleanDatabase();
                //return;

                var rootFolder = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), RootFolderPath));

                //var data = File.ReadAllBytes(Path.Combine(HtmlFolderPath, @"Recordings\j\jump.mp3"));
                //var str = Convert.ToBase64String(data);
                //File.WriteAllText(Path.Combine(RootFolderPath, "mp3.txt"), str);
                //return;

                //var b = new TopWordsBuilder();
               // b.GroupWords();
                
                //CheckFiles();
                //return;

                //NormalizeLines(Path.Combine(rootFolder, SourceFile), Path.Combine(rootFolder, NormalizedFile));

                //var builder = new XmlBuilder(Path.Combine(rootFolder, XmlLogFile));
                //builder.ConvertToXml(
                //    Path.Combine(rootFolder, SourceFile),
                //    Path.Combine(rootFolder, XmlFile),
                //    true, false);

                bool isDatabaseMode = true;

                var fileLoader = new FileLoader(
                    Path.Combine(rootFolder, SoundsFolder),
                    Path.Combine(rootFolder, SoundsCacheFolder),
                    true);
                var htmlBuilder = new HtmlBuilder(
                    isDatabaseMode,
                    DbConnectionString,
                    Path.Combine(rootFolder, HtmlLogFile),
                    fileLoader,
                    Path.Combine(rootFolder, TopWordsFile));
                htmlBuilder.ConvertToHtml(
                    Path.Combine(rootFolder, HtmlSourceFile),
                    HtmlFolderPath, -1, true);

                //var topBuilder = new TopWordsBuilder();
                //topBuilder.MergeTopWords();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("Finished");
                Console.ReadLine();
            }
        }

        private static void NormalizeLines(string source, string target)
        {
            using (var reader = new StreamReader(source))
            {
                using (var writer = new StreamWriter(target, false, Encoding.Unicode))
                {
                    while (!reader.EndOfStream)
                    {
                        writer.WriteLine(reader.ReadLine());
                    }
                }
            }
        }

        private static void CheckFiles()
        {
            var dic = new Dictionary<string, string>();
            var myfiles = Directory.GetFiles(@"D:\WORK\NET\Pronunciation\Results\Processing\Output\");
            foreach (var file in myfiles)
            {
                var key = Path.GetFileName(file);
                if (dic.ContainsKey(key))
                {
                }
                else
                {
                    dic.Add(key, null);
                }
            }

            foreach (var folder in Directory.GetDirectories(@"D:\WORK\NET\Pronunciation\Results\Sounds"))
            {
                var files = Directory.GetFiles(folder);
                foreach (var file in files)
                {
                    var key = Path.GetFileName(file);
                    if (!dic.ContainsKey(key))
                    {
                    }
                    //if (!File.Exists(@"D:\WORK\NET\Pronunciation\Results\Processing\Output\" + Path.GetFileName(file)))
                    //{
                    //}
                }
            }
        }

        private static void MoveFiles()
        {
            int count = 0;
            int folders = 0;
            string target = null;
            foreach (var file in Directory.GetFiles(@"D:\WORK\NET\Pronunciation\Results\Sounds"))
            {
                if (count >= 5000)
                {
                    count = 0;
                }

                if (count == 0)
                {
                    folders++;
                    target = string.Format(@"D:\WORK\NET\Pronunciation\Results\Sounds2\{0}", folders);
                    if (!Directory.Exists(target))
                    {
                        Directory.CreateDirectory(target);
                    }
                    Console.WriteLine(folders);
                }

                File.Move(file, string.Format(@"{0}\{1}.mp3", target, Path.GetFileNameWithoutExtension(file)));
                count++;
            }
            return;
        }

        private static void CleanDatabase()
        {
            using (SqlCeConnection conn = new SqlCeConnection(DbConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand();
                cmd.Connection = conn;

                cmd.CommandText = "Delete Collocations";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "Delete Words";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "Delete Sounds";
                cmd.ExecuteNonQuery();
            }
        }

        private static void UploadFiles()
        {
            using (SqlCeConnection conn = new SqlCeConnection(DbConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand("INSERT Words(WordId, Keyword, Body) Values(newid(), @keyword, @body)", conn);
                var parmName = cmd.Parameters.Add("@keyword", SqlDbType.NVarChar, 200);
                var parmBody = cmd.Parameters.Add("@body", SqlDbType.Image);

                var files = Directory.GetFiles(@"D:\DOCS\Языки\English\Pronunciation\Trainer\LPD\Dic", "*.html", SearchOption.AllDirectories);
                int cnt = 0;
                foreach (var file in files)
                {
                    parmName.Value = Path.GetFileNameWithoutExtension(file);
                    parmBody.Value = File.ReadAllBytes(file);
                    int k = cmd.ExecuteNonQuery();
                    if (k != 1)
                        throw new InvalidOperationException();

                    cnt++;
                    Console.WriteLine("Added " + cnt);
                    //if (cnt >= 1000)
                    //    break;
                }
            }
        }

        private static void UploadFilesBulk()
        {
            using (SqlCeConnection conn = new SqlCeConnection(DbConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand("Words", conn);
                cmd.CommandType = CommandType.TableDirect;

                SqlCeResultSet resultSet = cmd.ExecuteResultSet(ResultSetOptions.Updatable);

                int baseId = 105;
                for (int i = 0; i < 1000; i++)
                {
                    var record = resultSet.CreateRecord();
                    record["WordId"] = baseId + i;
                    record["Keyword"] = "two";
                    record["HtmlPage"] = new byte[] { 12, 45, 78, 88, 99, 0 };

                    resultSet.Insert(record);
                }
            }
        }

        private static void TestUpload()
        {
            using (SqlCeConnection conn = new SqlCeConnection(DbConnectionString))
            {
                conn.Open();
               // SqlCeCommand cmd = new SqlCeCommand(
               //     "SELECT Body FROM Words WHERE Keyword = @keyword", conn);
               // var parmName = cmd.Parameters.Add("@keyword", SqlDbType.NVarChar, 200);
               // parmName.Value = "a-";

               // string result = (string)cmd.ExecuteScalar();

               //// File.WriteAllText(@"D:\temp.html", result);
               // Console.WriteLine(string.IsNullOrEmpty(result) ? false : true);

                //SqlCeCommand cmd = new SqlCeCommand(
                //    "SELECT RawData FROM Sounds WHERE SoundKey = 'uk_lpd_a__paper'", conn);

                //File.WriteAllBytes(@"D:\Test.mp3", (byte[])cmd.ExecuteScalar());

                SqlCeCommand cmd2 = new SqlCeCommand(
    "SELECT HtmlPage FROM Words WHERE Keyword = 'an'", conn);

                var data = Encoding.UTF8.GetString((byte[])cmd2.ExecuteScalar());

                var template = File.ReadAllText(@"D:\WORK\NET\PronunciationTrainer\Pronunciation.Parser\Html\DatabaseTemplate.html");
                var result = string.Format(template, "My title", "file:///D:/LEARN/English/Pronunciation/Trainer/LPD/", data);
                File.WriteAllText(@"D:\Test.html", result, Encoding.UTF8);

                //

            }
        }
    }
}
