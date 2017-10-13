using System;
using System.Collections.Generic;

namespace mssqlstats
{
    class Option
    {
        /// <summary>
        /// SQL Server 認証を行う場合に、SQL Server 認証用のログインIDを指定します。
        /// </summary>
        [CommandLine.Option('U')]
        public string LoginID
        {
            get;
            set;
        }
        
        /// <summary>
        /// SQL Server 認証を行う場合に、SQL Server 認証用ログインIDのパスワードを指定します。
        /// </summary>
        [CommandLine.Option('P', DefaultValue = "")]
        public string Password
        {
            get;
            set;
        }
        
        /// <summary>
        /// Windows統合認証を行う場合に指定します。
        /// </summary>
        [CommandLine.Option('E')]
        public bool IsTrusted
        {
            get;
            set;
        }
        
        /// <summary>
        /// 接続先のSQL Serverサーバを指定します。
        /// 既定値は「(local)」です。
        /// フォーマットは以下の通りです。
        /// [protocol:]server[\instance_name][,port]
        /// </summary>
        [CommandLine.Option('S', DefaultValue = "(local)")]
        public string DataSource
        {
            get;
            set;
        }
        
        /// <summary>
        /// 分析対象のデータベースを指定します。
        /// </summary>
        [CommandLine.Option('d')]
        public string DatabaseName
        {
            get;
            set;
        }
        
        /// <summary>
        /// 分析対象のデータベースを','区切りで複数指定します。
        /// </summary>
        [CommandLine.OptionList('D', Separator = ',')]
        public List<String> DatabaseNameList
        {
            get;
            set;
        }
        
        /// <summary>
        /// 収集するデータの出力先フォルダをしていします。
        /// 相対パスを指定した場合はこのexe直下にフォルダを作成します。
        /// 既定値は「output」です。
        /// </summary>
        [CommandLine.Option('o', DefaultValue = @".\output")]
        public string OutputRootDir
        {
            get;
            set;
        }
        
        /// <summary>
        /// このパラメータの指定がない場合は収集実行前に対象となるDB数を通知し実行継続を確認します。
        /// この事前確認を実施しない場合にこのパラメータを指定します。
        /// </summary>
        [CommandLine.Option('s', DefaultValue = false)]
        public bool IsSilentMode
        {
            get;
            set;
        }
        
        /// <summary>
        /// コマンドタイムアウト値を秒単位で指定します。
        /// 既定値は「300秒（5分）」です。
        /// </summary>
        [CommandLine.Option('t', DefaultValue = 300)]
        public int CommandTimeout_sec
        {
            get;
            set;
        }
        
        /// <summary>
        /// ロックタイムアウト値を秒単位で指定します。
        /// 既定値は「180秒（3分）」です。
        /// </summary>
        [CommandLine.Option('l', DefaultValue = 180)]
        public int LockTimeout_sec
        {
            get;
            set;
        }
        
        /// <summary>
        /// 外部のコマンド（*.sqlファイルに記述されたT-SQL）を実行したい場合、
        /// そのコマンドファイルの格納されたフォルダを指定します。
        /// </summary>
        [CommandLine.Option('Q')]
        public string ExternalQueryDir
        {
            get;
            set;
        }
        
        /// <summary>
        /// ツールが既定で実行するコマンドは実行せず、外部のコマンドのみを実行したい場合にこのパラメータを指定します。
        /// </summary>
        [CommandLine.Option('q', DefaultValue = false)]
        public bool IsExternalQueryOnly
        {
            get;
            set;
        }
        
        /// <summary>
        /// 【内部用】バージョンチェックをバイパスします。
        /// </summary>
        [CommandLine.Option('b', DefaultValue = false)]
        public bool IsBypassVersionCheck
        {
            get;
            set;
        }

        /// <summary>
        /// 【内部用】データ収集完了後にDBCC SQLPERFを実行します。
        /// </summary>
        [CommandLine.Option('c', DefaultValue = false)]
        public bool IsStatsClear
        {
            get;
            set;
        }
        
        /// <summary>
        /// ZipFileインスタンスのdisposeをしているにも関わらずタイミングによってDeleteがエラーになる。
        /// System.IO.Compression.ZipFileでは問題なく動作するが、donetzipだとダメ（作りが悪い？）。
        /// 暫定対処としてリトライを入れる。
        /// </summary>
        [CommandLine.Option("sleep", DefaultValue = 10)]
        public int RetryCountOfOneSecSleep
        {
            get;
            set;
        }

        /// <summary>
        /// 【内部用】debugコードを実行します。
        /// </summary>
        [CommandLine.Option("debug", DefaultValue = false)]
        public bool IsDebug
        {
            get;
            set;
        }
        
        public string Protocol
        {
            get;
            set;
        }
        public string Server
        {
            get;
            set;
        }
        public string Instance
        {
            get;
            set;
        }
        public string Port
        {
            get;
            set;
        }
    }
}
