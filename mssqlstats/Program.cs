using CommandLine;
using CsvHelper;
using Ionic.Zip;
using Ionic.Zlib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
//using System.IO.Compression; .NET4以降
using System.Management;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace mssqlstats
{
    class Program
    {
        private static readonly log4net.ILog logger
            = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Option Parameters = new Option();

        [Flags]
        enum SQLVersion
        {
            UNKNOWN = 0x000
            , SQL2005 = 0x001        //  9.0
            , SQL2008 = 0x002        // 10.0
            , SQL2008R2 = 0x004      // 10.5
            , SQL2012 = 0x008        // 11.0
            , SQL2014 = 0x010        // 12.0
            , SQL2016 = 0x020        // 13.0
            , SQL2017 = 0x040        // 14.0
        }

        static void DebugCode(SqlCommand Cmd)
        {
            //System.OperatingSystem os = System.Environment.OSVersion;

            //Cmd.CommandText = "use db01";
            //int res = Cmd.ExecuteNonQuery();//ExecuteNonQueryのようなものだとタイムアウトしない

            //Cmd.CommandText = "exec test_no_proc 1";
            //res = Cmd.ExecuteNonQuery();//ExecuteNonQueryのようなものだとタイムアウトしない

            //Cmd.CommandText = "EXEC sp_executesql N'/*aaa*/ SELECT * FROM dataset WHERE id = @param1', N'@param1 int', @param1 = 1";
            //res = Cmd.ExecuteNonQuery();//ExecuteNonQueryのようなものだとタイムアウトしない

            //Cmd.CommandText = "select * from sys.databases";
            //res = Cmd.ExecuteNonQuery();//ExecuteNonQueryのようなものだとタイムアウトしない

            //Cmd.CommandText = "use db01; update table_1 set name = 'oota' where id = 1"; //このselectがロックウェイトするように先行Txでupdateしておく

            //using (SqlDataReader reader1 = Cmd.ExecuteReader())//コマンドタイムアウトはここでException。
            //{
            //    res = reader1.FieldCount;
            //    do
            //    {
            //        for (int i = 0; i < reader1.FieldCount; i++)
            //        {
            //            logger.Debug(reader1.GetName(i));
            //        }

            //        while (reader1.Read()) //locktimeoutはここでエスカレーション
            //        {
            //            for (int i = 0; i < reader1.FieldCount; i++)
            //            {
            //                logger.Debug(reader1.GetValue(i).ToString());
            //            }
            //        }
            //    } while (reader1.NextResult());
            //}
        }

        static void Main(string[] args)
        {
            SqlConnection Conn = null;
            SqlCommand Cmd = null;
            SQLVersion DBVersion = SQLVersion.UNKNOWN;
            List<String> DBNameList = null;
            Dictionary<String, Query> QueryList = null;
            String OutputDir = String.Empty;

            try
            {
                //実行環境の設定(作業ディレクトリの設定とlog4netによるログ出力先の設定)
                SetProcEnv();

                //１．処理開始の通知
                logger.Info(" 1/10. Start.");

                //２．コマンドライン引数の解析
                logger.Info(" 2/10. Parse CommandLine Parameters.");
                ParseCommandLineParams(args);

                using (Conn = new SqlConnection(BuildConnectionString(Parameters)))
                using (Cmd = new SqlCommand())
                {
                    //３．コネクションオープン
                    logger.Info(" 3/10. Try to Connect SQL Server.");
                    Conn.Open();
                    Cmd.Connection = Conn;
                    SetSessionEnv(Cmd);
#if DEBUG
                    if (Parameters.IsDebug)
                        DebugCode(Cmd);
#endif
                    //４．バージョンチェック
                    logger.Info(" 4/10. Check SQL Server Version.");
                    DBVersion = CheckDBVersion(Cmd);
                    if (DBVersion == SQLVersion.UNKNOWN)
                    {
                        // エラーのケースは２通り
                        // ・DBのバージョンがツールのサポート対象外のケース
                        // ・設定ファイルで指定した対象DBバージョンと実際のDBバージョンが異なるケース
                        logger.Error("This database version does not support tool execution.");
                        //このデータベースバージョンはツールの実行がサポートされていません。
                        Environment.Exit(1);
                    }

                    //５．対象DB一覧取得
                    logger.Info( " 5/10. Get Database List.");
                    DBNameList = GetDBNameList(Cmd);
                    if (DBNameList == null)
                    {
                        // エラーのケースは１通り
                        // ・コマンドライン引数 -d が無指定 ＆ システムDB以外のユーザDBが一つも存在しないケース
                        logger.Error("Please specify the target database with -d or -D.");
                        // -d or -D で対象のデータベースを指定してください。
                        Environment.Exit(1);
                    }
                    else
                    {
                        if (!Parameters.IsSilentMode)
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                "  The target SQL Server has "
                                + DBNameList.Count.ToString()
                                + " databases to collect data."
                                + "Processing takes time depending on the number of databases."
                                );
                            Console.WriteLine();
                            Console.WriteLine("  To continue the process, type Y, otherwise type N.");
                            Console.WriteLine();
                            Console.Write("  (Type Y or N) : ");
                            //対象のSQL Serverには情報収集の対象となるデータベースがN個あります。
                            //データベースの個数に応じて処理に時間を要します。
                            //処理を継続する場合はYをそうでなければNをタイプしてください。
                            if (!String.Equals(Console.ReadLine(), "Y"))
                            {
                                logger.Info("Execution canceled.");
                                //処理をキャンセルしました。
                                Environment.Exit(1);
                            }
                        }
                    }

                    //６．出力フォルダ（OutputRootDir + yyyyMMddhhmmss）作成
                    logger.Info(" 6/10. Create Result Folder.");
                    OutputDir = Path.Combine(Parameters.OutputRootDir
                        , @"mssqlstats_" + DateTime.Now.ToString("yyyyMMddhhmmss"));
                    foreach (String dbname in DBNameList)
                    {
                        Directory.CreateDirectory(Path.Combine(OutputDir, dbname));
#if TRACE
                        logger.Debug("OutputDir = " + Path.Combine(OutputDir, dbname));
#endif
                    }

                    //７．実行クエリのロード
                    logger.Info(" 7/10. Load Query.");
                    QueryList = LoadQuery(DBVersion);
                    if (QueryList == null)
                    {
                        logger.Fatal("Executable query was not found.");
                        //実行可能なクエリが見つかりませんでした。
                        Environment.Exit(1);
                    }

                    //８．クエリ実行 ＆ CSV出力
                    logger.Info(" 8/10. Execute Query.");
                    SqlDataReader reader = null;
                    foreach (String key in QueryList.Keys)
                    {
                        try
                        {
                            switch (QueryList[key].Target)
                            {
                                case Query.QueryTarget.System:

#if TRACE
                                    logger.Debug("System:" + key + "->" + OutputDir + @"\" + key + ".csv");
#endif
                                    ManagementScope scope = new ManagementScope(String.Format(@"\\{0}\root\cimv2", Parameters.Server));
                                    WriteCSV(scope, QueryList[key].Text, OutputDir + @"\" + key + ".csv");
                                    //scope = new ManagementScope(String.Format(@"\\{0}\root\rsop\computer", m.Groups["server"].Value));
                                    //WriteCSV(scope, "SELECT Name FROM RSOP_UserPrivilegeRight", OutputDir + @"\" + key + ".csv");

                                    break;

                                case Query.QueryTarget.Instance:

#if TRACE
                                    logger.Debug("Instance:" + key + "->" + OutputDir + @"\" + key + ".csv");
#endif
                                    Cmd.CommandText = "USE [master]; " + QueryList[key].Text;
                                    using (reader = Cmd.ExecuteReader())
                                    {
                                        WriteCSV(reader, OutputDir + @"\" + key + ".csv");
                                    }
                                    break;

                                case Query.QueryTarget.Database:
                                    foreach (String dbname in DBNameList)
                                    {
#if TRACE
                                        logger.Debug("DB(" + dbname + "):" + key + "->" + OutputDir + @"\" + dbname + @"\" + key + ".csv");
#endif
                                        Cmd.CommandText = "USE [" + dbname + @"];" + QueryList[key].Text;
                                        using (reader = Cmd.ExecuteReader())
                                        {
                                            WriteCSV(reader, OutputDir + @"\" + dbname + @"\" + key + ".csv");
                                        }
                                    }
                                    break;
                                default:
                                    logger.Fatal("It is an illegal execution route...");
                                    //不正な実行ルートです。
                                    Environment.Exit(1);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error("query exec error.");
                            logger.Error(ex.Message);
                            logger.Error(ex.StackTrace);
                        }
                    }

                    //stats系DMVをクリア（既定はクリアしない）
                    if (Parameters.IsStatsClear)
                    {
                        logger.Info("Clear Stats.");
                        ClearStats(Cmd);
                    }

                    //９．出力フォルダをZIP圧縮 ＆ 出力フォルダを削除
                    logger.Info(" 9/10. Archive Result Folder.");
                    ArchiveOutputFiles(OutputDir);

                    //１０．処理完了の通知
                    logger.Info("10/10. Completed.");
                    CopyLogFile(OutputDir);
                }
            }
            //ユーザエラー
            catch (MssqlStatsException ex)
            {
                logger.Error(ex.Message);
            }
            //内部エラー
            catch (Exception ex)
            {
                logger.Fatal("The tool terminated abnormally.");
                logger.Fatal(ex.Message);
                logger.Fatal(ex.StackTrace);
            }
            logger.Info("Done.");
        }

        private static void SetProcEnv()
        {
            //作業ディレクトリを実行exeのディレクトリに移動
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            //log4netのログ出力先を設定
            //（log4netのファイル出力先に相対パスをしているすると既定は実行ディレクトリからの相対パスとなる。）
            Environment.SetEnvironmentVariable("LogBaseDir", Directory.GetCurrentDirectory());
            log4net.Config.XmlConfigurator.Configure();
        }

        private static String BuildConnectionString(Option opt)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder()
            {
                DataSource = opt.DataSource,
                InitialCatalog = "master"
            };
            if (opt.DatabaseName != null)
                builder.InitialCatalog = opt.DatabaseName;

            if (opt.IsTrusted)
            {
                builder.IntegratedSecurity = opt.IsTrusted;
            }
            else
            {
                builder.Password = opt.Password;
                builder.UserID = opt.LoginID;
            }

#if TRACE
            logger.Debug("ConnectionString = " + builder.ConnectionString);
#endif

            return builder.ConnectionString;
        }

        static void ParseCommandLineParams(string[] args)
        {

#if TRACE
            foreach (string arg in args)
                logger.Debug("args = " + arg);
#endif
            //コマンドライン引数は大文字小文字を区別する
            Parser p = new Parser(with => with.CaseSensitive = true);

            if (!p.ParseArguments(args, Parameters))
                //コマンドライン引数の解析に失敗しました。
                throw new MssqlStatsException("Failed to parse command line arguments.");
            
            if (Parameters.DatabaseName != null && Parameters.DatabaseNameList != null)
                //コマンドライン引数 -d と -D は同時に指定できません。
                throw new MssqlStatsException("Command line arguments -d and -D can not be specified at the same time.");
            
            if (Parameters.ExternalQueryDir == null && Parameters.IsExternalQueryOnly)
                //-q を指定する場合は同時に -Q も指定する必要があります。
                throw new MssqlStatsException("When specifying -q you also need to specify -Q at the same time.");

            if (!Parameters.IsTrusted && Parameters.LoginID == null)
                //SQL Server認証の場合は-U（LoginID）と-P（パスワード）を指定してください。Windows認証の場合は-Eを指定してください。
                throw new MssqlStatsException("Please specify -U(LoginID) and -P(Password) for SQL Server authentication. " +
                    "Or -E for Windows authentication.");

            if (Parameters.IsTrusted && Parameters.LoginID != null)
                //認証はWindows認証かSQL Server認証のいずれかを選択してください。
                throw new MssqlStatsException("For authentication, please choose either Windows authentication(using -E) or SQL Server authentication(Using -U and -P).");

            if (Parameters.ExternalQueryDir != null)
            {
                if (!Directory.Exists(Parameters.ExternalQueryDir))
                    //-Q で指定されたディレクトリが存在しません。
                    throw new MssqlStatsException("The directory specified by -Q does not exist.");
            }

            // -Sパラメータ（[protocol:]server[\instance_name][,port]）を分解
            Regex r = new Regex(@"(?<protocol>\w+:)?(?<server>\w+)(?<instance>\\\w+)?(?<port>,\d+)?", RegexOptions.IgnoreCase);
            Match m = r.Match(Parameters.DataSource);
            if (!m.Success)
                throw new Exception("Can not parse DataSource string.");

            Parameters.Protocol = m.Groups["protocol"].Value;
            Parameters.Server = m.Groups["server"].Value;
            Parameters.Instance = m.Groups["instance"].Value;
            Parameters.Port = m.Groups["port"].Value;
#if TRACE
            logger.Debug("protocol = " + Parameters.Protocol);
            logger.Debug("server   = " + Parameters.Server);
            logger.Debug("instance = " + Parameters.Instance);
            logger.Debug("port     = " + Parameters.Port);
#endif
        }

        static void SetSessionEnv(SqlCommand cmd)
        {
            //アイソレーションレベルをReadUnCommittedに設定
            SetTransactionIsolationLevelReadUnCommitted(cmd);
            //コマンドタイムアウト設定
            cmd.CommandTimeout = Parameters.CommandTimeout_sec;
            //ロックタイムアウト設定（ReadUnCommittedでもSch-*なロック獲得が必要なクエリがあり）
            SetLockTimeout(cmd, Parameters.LockTimeout_sec * 1000);
#if TRACE
            logger.Debug("Query Timeout = " + Parameters.CommandTimeout_sec.ToString() + "[sec]");
            logger.Debug("Lock Timeout  = " + Parameters.LockTimeout_sec.ToString() + "[sec]");
#endif
        }

        private static SQLVersion CheckDBVersion(SqlCommand cmd)
        {
            SQLVersion Version = SQLVersion.UNKNOWN;
            cmd.CommandText = Properties.Resources._GET_PRODUCT_VERSION;
            String v = cmd.ExecuteScalar().ToString();
            Regex r = new Regex(@"^\d+\.\d+", RegexOptions.IgnoreCase);
            switch (r.Match(v).Value)
            {
                case "9.0":
                    Version = SQLVersion.SQL2005;
                    break;
                case "10.0":
                    Version = SQLVersion.SQL2008;
                    break;
                case "10.50":
                    Version = SQLVersion.SQL2008R2;
                    break;
                case "11.0":
                    Version = SQLVersion.SQL2012;
                    break;
                case "12.0":
                    Version = SQLVersion.SQL2014;
                    break;
                case "13.0":
                    Version = SQLVersion.SQL2016;
                    break;
                case "14.0":
                    Version = SQLVersion.SQL2017;
                    break;
                default:
                    //DBのバージョンがツールのサポート対象外のケース
                    Version = SQLVersion.UNKNOWN;
                    break;
            }

#if TRACE
            logger.Debug("SQL Server ProductVersion = " + v);
            logger.Debug("SQL Server Version = " + Version.ToString());
            logger.Debug("DB version specified in App.config = " + ConfigurationManager.AppSettings["verbit"]);
#endif

            if (!Parameters.IsBypassVersionCheck)
            {

                //ツールが想定しているDBバージョン（＝App.configでの設定値）と対象となる実際のDBバージョンが等しいかをチェック
                //if (!String.Format("0x{0:X4}", (int)Version).Equals(ConfigurationManager.AppSettings["verbit"]))
                if (( ( (SQLVersion)Convert.ToInt32(ConfigurationManager.AppSettings["verbit"], 16)) 
                    & Version) != Version)
                {
                    //設定ファイルで指定した対象DBバージョン(16進数)と実際のDBバージョンが異なるケース
                    /*  SQL2005(9.0) = 0x0001
                        SQL2008(10.0) = 0x0002
                        SQL2008R2(10.50) = 0x0004
                        SQL2012(11.0) = 0x0008
                        SQL2014(12.0) = 0x0010
                        SQL2016(13.0) = 0x0020
                        SQL2017(14.0) = 0x0040 */
                Version = SQLVersion.UNKNOWN;
                }
            }
            return Version;
        }

        private static List<String> GetDBNameList(SqlCommand cmd)
        {
            List<String> list = new List<string>();
            SqlDataReader reader = null;
            if (Parameters.DatabaseName == null && Parameters.DatabaseNameList == null)
            {
                cmd.CommandText = Properties.Resources._GET_DB_NAME_LIST;
                using (reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(reader.GetValue(0).ToString());
                    }
                }
            }
            else if (Parameters.DatabaseName == null && Parameters.DatabaseNameList != null)
                list.AddRange(Parameters.DatabaseNameList);
            else if (Parameters.DatabaseName != null && Parameters.DatabaseNameList == null)
                list.Add(Parameters.DatabaseName);
            else
            {
                //すでにチェック済みで通らないパス
            }
            return list;
        }

        private static Dictionary<String, Query> LoadQuery(SQLVersion ver)
        {
            Dictionary<String, Query> list = new Dictionary<String, Query>();
            CollectConfigHandler appconfig = null;
            Query.QueryTarget target = Query.QueryTarget.Instance;
            ResourceSet res = null;
            IDictionaryEnumerator files = null;
            CollectConfigItem conf = null;

            //  外部クエリのロード
            if (Parameters.ExternalQueryDir != null)
            {
                foreach (string filename in System.IO.Directory.GetFiles(Parameters.ExternalQueryDir, "*.sql"))
                {
                    using (StreamReader sr = new StreamReader(filename, System.Text.Encoding.Default))
                    {
                        // ファイル名が"Instance（大文字小文字を区別しない）"で開始されていた場合はインスタンスレベルのクエリであると解釈する
                        if (Path.GetFileName(filename).StartsWith("Instance", true, null))
                            target = Query.QueryTarget.Instance;
                        // そうでない場合は、DB名が指定されている場合はDBレベルのクエリであると解釈する
                        else
                        {
                            if (Parameters.DatabaseName != null || Parameters.DatabaseNameList != null)
                                target = Query.QueryTarget.Database;
                            else
                                target = Query.QueryTarget.Instance;
                        }
                        
                        list.Add(Path.GetFileNameWithoutExtension(filename)
                            , new Query(sr.ReadToEnd(), target));
#if TRACE
                        logger.Debug("ExternalQuery = " + Path.GetFileNameWithoutExtension(filename));
#endif
                    }
                }
            }

            //  内部クエリのロード
            if (!Parameters.IsExternalQueryOnly)
            {
                appconfig = (CollectConfigHandler)ConfigurationManager.GetSection("CollectConfig");

                //内部クエリのロード
                res = Properties.Resources.ResourceManager.GetResourceSet(new CultureInfo("en-US"), true, true);
                if (res == null)
                    //リソースファイルが見つかりません
                    throw new Exception("Resource file can not be found");

                files = res.GetEnumerator();
                while (files.MoveNext())
                {
                    String key = files.Entry.Key.ToString();
                    conf = (CollectConfigItem)appconfig.Query[key];
                    if (conf != null)
                    {
                        //HasFlag は .net v4以降
                        //if (conf.Run
                        //    && ((SQLVersion)conf.Version).HasFlag(ver))
                        if (
                            conf.Run
                            &&
                            (
                              ((((SQLVersion)conf.Version) & ver) == ver)
                              ||
                              (conf.Version == 0)
                            )
                           )
                        {
                            if (conf.Target.ToString().Equals("System"))
                                target = Query.QueryTarget.System;
                            else if (conf.Target.ToString().Equals("Instance"))
                                target = Query.QueryTarget.Instance;
                            else if (conf.Target.ToString().Equals("Database"))
                                target = Query.QueryTarget.Database;
                            else
                                logger.Error(String.Format("Invalid target string({0}),", conf.Target.ToString()));

                            list.Add(files.Entry.Key.ToString()
                                , new Query(files.Entry.Value.ToString(), target));
                        }

#if TRACE
                        logger.Debug("InternalQuery = " + conf.Name.ToString());
                        logger.Debug("  Version = " + String.Format("0x{0:X4}", conf.Version));
                        logger.Debug("  Target = " + conf.Target.ToString());
                        logger.Debug("  Run = " + conf.Run.ToString());
                        //logger.Debug("  Query = " + files.Entry.Value.ToString());
#endif

                        conf = null;
                    }
                }
            }
            return list;
        }

        private static void SetTransactionIsolationLevelReadUnCommitted(SqlCommand cmd)
        {
            cmd.CommandText = Properties.Resources._SET_TRANSACTION_ISOLATION_LEVEL_READUNCOMMITTED;
            int res = cmd.ExecuteNonQuery();
        }

        private static void SetLockTimeout(SqlCommand cmd, int timeout_ms)
        {
            cmd.CommandText = String.Format(Properties.Resources._SET_LOCK_TIMEOUT, timeout_ms);
            int res = cmd.ExecuteNonQuery();
        }

        private static void WriteCSV(SqlDataReader reader, String csvfile)
        {
            CsvWriter writer = null;
            using (writer = new CsvWriter(new StreamWriter(csvfile)))
            {
                do
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        writer.WriteField(reader.GetName(i));
                    }
                    writer.NextRecord();

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            writer.WriteField(reader.GetValue(i).ToString());
                        }
                        writer.NextRecord();
                    }

                } while (reader.NextResult());
            }
            writer = null;
        }

        private static void ClearStats(SqlCommand cmd)
        {
            cmd.CommandText = Properties.Resources._DBCC_SQLPERF_CLEAR_STATS;
            int res = cmd.ExecuteNonQuery();
        }

        static void WriteCSV(ManagementScope scope, string wmi_query, string csvfile)
        {
            CsvWriter writer = null;
            ManagementObjectSearcher searcher = null;
            SelectQuery query = new SelectQuery();
            try
            {
                scope.Connect();

                query.QueryString = wmi_query;
                using (searcher = new ManagementObjectSearcher(scope, query))
                using (writer = new CsvWriter(new StreamWriter(csvfile)))
                {

                    for (int i = 0; i < query.SelectedProperties.Count; i++)
                    {
                        writer.WriteField(query.SelectedProperties[i].ToString());
                    }
                    writer.NextRecord();
                    
                    foreach (System.Management.ManagementObject service in searcher.Get())
                    {
                        for (int i = 0; i < query.SelectedProperties.Count; i++)
                        {

                            //if ((service[query.SelectedProperties[i].ToString()]) is string[])
                            //{
                            //    writer.WriteField(string.Join(" *** ", (string[])service[query.SelectedProperties[i].ToString()]));
                            //}
                            //else if ((service[query.SelectedProperties[i].ToString()]) is int[]) { }
                            //else
                            //{
                            //    writer.WriteField(service[query.SelectedProperties[i].ToString()]);
                            //}
                            writer.WriteField(service[query.SelectedProperties[i].ToString()]);
                        }
                        writer.NextRecord();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Can not connect Management target.");
                logger.Error(ex.Message);
                logger.Error(ex.StackTrace);
            }
        }

        private static void ArchiveOutputFiles(String dest)
        {            
            //system.io.compression.filesystem.zipfile は .net v4以降
            //ZipFile.CreateFromDirectory(OutputDir, OutputDir + ".zip");
            ZipFile zip = null;
            using (zip = new ZipFile(Encoding.GetEncoding("Shift_JIS")))
            {
                zip.CompressionLevel = CompressionLevel.BestCompression;
                zip.AddDirectory(dest);
                zip.Save(dest + ".zip");
            }

            int trynum = 0;
            int retrynum = Parameters.RetryCountOfOneSecSleep;
            do
            {
                try
                {
                    trynum++;
                    Directory.Delete(dest, true);
                    break;
                }
                catch (Exception ex)
                {
                    //using で ZipFileインスタンスのdisposeをしているにも関わらずタイミングによって
                    //Deleteがエラーになる。System.IO.Compression.ZipFileでは問題なく動作するが、
                    //donetzipだとダメ。暫定対処としてリトライを入れる。
                    logger.Info(ex.Message);
                    logger.InfoFormat("Retry({0}/{1}) delete folder.", trynum, retrynum);
                    if (trynum < retrynum)
                        System.Threading.Thread.Sleep(1000);
                    else
                    {
                        logger.Fatal(ex.Message);
                        logger.Fatal("Can not delete folder.");
                            break;
                    }
                }
            }
            while (true);
        }

        private static void CopyLogFile(String dest)
        {
            var rootLogger = ((log4net.Repository.Hierarchy.Hierarchy)logger.Logger.Repository).Root;
            var appender = rootLogger.GetAppender("FileAppender") as log4net.Appender.FileAppender;
            using (ZipFile zip = ZipFile.Read( dest + ".zip" ))
            {
                //zip直下にlogフォルダを掘りそこにログファイルを追加（すでに同名のエントリがあれば上書き）
                zip.UpdateFile(appender.File, "log");
                zip.Save();
            }
        }
    }
}
