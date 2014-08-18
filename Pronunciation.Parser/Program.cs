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
        private const string RootFolderPath = @"..\..\..\";
        private const string DataFolder = @"Data\";
        private const string AnalysisFolder = @"Analysis\";

        // Used for generation
        private const string SoundsFolder = DataFolder + "Sounds";
        private const string SoundsCacheFolder = DataFolder + "SoundsCache";
        private const string HtmlSourceFile = DataFolder + "Results - Final.xml";
        private const string TopWordsFile = DataFolder + "TopWords.txt";

        private const string HtmlFolderDB = @"D:\LEARN\English\Pronunciation\Trainer\LPD_DB";
        private const string HtmlFolderFiles = @"D:\LEARN\English\Pronunciation\Trainer\LPD_File";
        private const string LPDConnectionString = @"Data Source=D:\LEARN\English\Pronunciation\Trainer\Database\LPD.sdf;Max Database Size=4000;";
        private const string TrainerConnectionString =
@"Data Source=D:\LEARN\English\Pronunciation\Trainer\Database\PronunciationTrainer.sdf;Max Database Size=1000;Temp File Max Size=25;";

        // Used for analysis
        private const string SourceFile = DataFolder + "En-En-Longman_Pronunciation.dsl";
        private const string NormalizedFile = AnalysisFolder + "Results_Normalize.txt";
        private const string XmlFile = AnalysisFolder + "Results.xml";
        private const string XmlLogFile = AnalysisFolder + "XmlConvert.log";
        private const string HtmlLogFile = AnalysisFolder + "HtmlConvert.log";

        static void Main(string[] args)
        {
            try
            {
                //MigrateRecordingsToDB();
                StoreLargeData();
                //UploadFiles();
                //UploadFilesBulk();
                //TestUpload();
                //MigrateRecordings();
                return;

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
                bool isFakeMode = true;
                if (isDatabaseMode && !isFakeMode)
                {
                    CleanDatabase();
                }

                var fileLoader = new FileLoader(
                    Path.Combine(rootFolder, SoundsFolder),
                    Path.Combine(rootFolder, SoundsCacheFolder),
                    true);
                var htmlBuilder = new HtmlBuilder(
                    isDatabaseMode,
                    LPDConnectionString,
                    Path.Combine(rootFolder, HtmlLogFile),
                    fileLoader,
                    Path.Combine(rootFolder, TopWordsFile));
                htmlBuilder.ConvertToHtml(
                    Path.Combine(rootFolder, HtmlSourceFile),
                    isDatabaseMode ? HtmlFolderDB : HtmlFolderFiles, 
                    -1,
                    isFakeMode);

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
            using (SqlCeConnection conn = new SqlCeConnection(LPDConnectionString))
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
            using (SqlCeConnection conn = new SqlCeConnection(LPDConnectionString))
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
            using (SqlCeConnection conn = new SqlCeConnection(LPDConnectionString))
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
            using (SqlCeConnection conn = new SqlCeConnection(LPDConnectionString))
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

        private static void MigrateRecordings()
        {
            var baseFolder = @"D:\LEARN\English\Pronunciation\Trainer\Recordings\LPD";
            var files = Directory.GetFiles(baseFolder, "*.mp3", SearchOption.AllDirectories);

            using (SqlCeConnection conn = new SqlCeConnection(LPDConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand(
@"SELECT s.SoundKey FROM Words w 
    INNER JOIN Sounds s ON w.SoundIdUS = s.SoundId
    WHERE w.Keyword = @parm1", conn);
                var parm = new SqlCeParameter("@parm1", SqlDbType.NVarChar);
                cmd.Parameters.Add(parm);

                foreach (var filePath in files)
                {
                    parm.Value = Path.GetFileNameWithoutExtension(filePath);
                    string audioKey = (string)cmd.ExecuteScalar();
                    if (string.IsNullOrEmpty(audioKey))
                    {
                        Console.WriteLine("Not found '{0}'", parm.Value);
                        continue;
                    }

                    var fileDest = Path.Combine(baseFolder, string.Format("{0}.mp3", audioKey));
                    if (File.Exists(fileDest))
                    {
                        Console.WriteLine("File exists '{0}'", audioKey);
                        continue;
                    }

                    File.Copy(filePath, fileDest);
                }
            }
        }

        private static void UploadExercises()
        {
            byte[] data = File.ReadAllBytes(@"D:\temp\main.png");
            byte[] audio1 = File.ReadAllBytes(@"D:\temp\1.1.mp3");
            byte[] audio2 = File.ReadAllBytes(@"D:\temp\1.2.mp3");

            //byte[] data = new byte[] { 23, 45, 67 };
            using (SqlCeConnection conn = new SqlCeConnection(TrainerConnectionString))
            {
                conn.Open();

                SqlCeCommand cmd = new SqlCeCommand("Exercise", conn);
                cmd.CommandType = CommandType.TableDirect;
                SqlCeResultSet resultSet = cmd.ExecuteResultSet(ResultSetOptions.Updatable);

                SqlCeCommand cmdAudio = new SqlCeCommand("ExerciseAudio", conn);
                cmdAudio.CommandType = CommandType.TableDirect;
                SqlCeResultSet resultSetAudio = cmdAudio.ExecuteResultSet(ResultSetOptions.Updatable);

                while(resultSet.Read())
                {
                    resultSet.SetValue(resultSet.GetOrdinal("ExerciseData"), data);
                    resultSet.Update();

                    Guid exerciseId = resultSet.GetGuid(resultSet.GetOrdinal("ExerciseId"));

                    var record1 = resultSetAudio.CreateRecord();
                    FillExercisesAudio(record1, exerciseId, "1.1", audio1);
                    resultSetAudio.Insert(record1);

                    var record2 = resultSetAudio.CreateRecord();
                    FillExercisesAudio(record2, exerciseId, "1.2", audio2);
                    resultSetAudio.Insert(record2);
                }
            }
        }

        private static void FillExercisesAudio(SqlCeUpdatableRecord record, Guid exerciseId, string audioName, byte[] data)
        {
            record["AudioId"] = Guid.NewGuid();
            record["ExerciseId"] = exerciseId;
            record["AudioName"] = audioName;
            record["RawData"] = data;
        }

        private static void MigrateRecordingsToDB()
        {
            using (SqlCeConnection conn = new SqlCeConnection(TrainerConnectionString))
            {
                conn.Open();

                var migration = new RecordingsMigration(conn);
                migration.Migrate();
            }
        }

        private static void StoreLargeData()
        {
            using (SqlCeConnection conn = new SqlCeConnection(TrainerConnectionString))
            {
                conn.Open();
               // return;

                SqlCeCommand cmd = new SqlCeCommand(
"UPDATE Training SET ReferenceAudioData = @body WHERE TrainingId='93cd79f5-7d5b-44ac-b428-cf8cda8cf102'", conn);
                var parmBody = cmd.Parameters.Add("@body", SqlDbType.Image);
                parmBody.Value = File.ReadAllBytes(@"D:\Temp\Recordings\track 48.mp3");//track 01.mp3
                //track 48
                int res = cmd.ExecuteNonQuery();
            }
        }
    }
}
