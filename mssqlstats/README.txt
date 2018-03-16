＜ツールの動作要件＞

    ● 対象DB：SQL Server 2005、SQL Server 2008、SQL Server 2008 R2、SQL Server 2012
               SQL Server 2014、SQL Server 2016、SQL Server 2017

    ● 対象OS：上記の SQL Server がサポートするOSに準じます。

    ● SW要件：.Net Framework 3.5
    
      ▼ SQL Server 2008 ～ SQL Server 2014 は .Net Framework 3.5 が動作要件であるため、
         これらの SQL Server サーバ上であれば、このツールの動作要件を満たします。
      
      ▼ SQL Server 2005 は.Net Framework 2.0、SQL Server 2016 は.Net Framework 4.6 が
         動作要件であるため、これらの SQL Server が対象となる場合は .Net Framework 3.5
         がインストールされたリモートクライアント上でこのツールを実行してください。
        
      ▼ なお、Windows 7 は .Net Framework 3.5 がプリインストールされています。
	  
    ● 実行権限：SQL Server の 管理者権限（sysadmin権限）をもつユーザで実行してください。

＜ツール実行パラメータ一覧＞

  ■ 必須 ■
    ● 認証パラメータ
      ▼ U：SQL Server 認証を行う場合に、SQL Server 認証用のログインIDを指定します。
      ▼ P：SQL Server 認証を行う場合に、SQL Server 認証用ログインIDのパスワードを指定します。
      ▼ E：Windows統合認証を行う場合に指定します。
    ● 接続先パラメータ
      ▼ S：接続先のSQL Serverサーバを指定します。既定値は「(local)」です。
            フォーマットは以下の通りです。
            [protocol:]server[\instance_name][,port]
  ■ 任意 ■
    ● 動作制御パラメータ
      ▼ d：分析対象のデータベースを指定します。
            指定のない場合はすべてのユーザDBを対象に処理します。
      ▼ D：分析対象のデータベースを','区切りで複数指定します。
            指定のない場合はすべてのユーザDBを対象に処理します。
      ▼ o：収集するデータの出力先フォルダをしていします。相対パスを指定した場合は
            このexe直下にフォルダを作成します。既定値は「output」です。
      ▼ s：このパラメータの指定がない場合は収集実行前に対象となるDB数を通知し実行
            継続を確認します。この事前確認を実施しない場合にこのパラメータを指定します。
      ▼ t：コマンドタイムアウト値を秒単位で指定します。既定値は「300秒（5分）」です。
            0の指定は無期限の待機を意味します。負の値は指定できません。
      ▼ l：ロックタイムアウト値を秒単位で指定します。既定値は「180秒（3分）」です。
            0の指定は待機をしないことを意味します。負の値の指定は無期限の待機を意味します。
    ● 外部コマンド指定パラメータ
      ▼ Q：外部のコマンド（*.sqlファイルに記述されたT-SQL）を実行したい場合、そのコマンド
            ファイルの格納されたフォルダを指定します。
      ▼ q：ツールが既定で実行するコマンドは実行せず、外部のコマンドのみを実行したい場合に
            このパラメータを指定します。


＜ツール実行パラメータ解説＞

  ■ 必須 ■（パラメータ名と設定値の解釈はsqlcmdのパラメータに準拠しています）

    ● 認証パラメータ
    
      ▼ U：SQL Server 認証を行う場合に、SQL Server 認証用のログインIDを指定します。
      ▼ P：SQL Server 認証を行う場合に、SQL Server 認証用ログインIDのパスワードを指定します。
      ▼ E：Windows統合認証を行う場合に指定します。

        例）

        ・ Windows統合認証
    
            > mssqlstats.exe –E
    
        ・ SQL Server認証
    
            > mssqlstats.exe -U login -P password

    ● 接続先パラメータ

      ▼ S：接続先のSQL Serverサーバを指定します。既定値は「(local)」です。
            フォーマットは以下の通りです。
            [protocol:]server[\instance_name][,port]

        例）

        ・ 既定のインスタンスへの接続
          
            > mssqlstats.exe -E -S server

        ・ 名前付きインスタンスへの接続
        
            > mssqlstats.exe -E -S server\instance_name

        ・ 既定のインスタンスへポート指定で接続
          
            > mssqlstats.exe -E -S server,1433

        ・ プロトコル指定での接続
            
            > mssqlstats.exe -E -S tcp:server

  ■ 任意 ■
    
    ● 動作制御パラメータ
    
      ▼ d：分析対象のデータベースを指定します。
            指定のない場合はすべてのユーザDBを対象に処理します。
      ▼ D：分析対象のデータベースを','区切りで複数指定します。
            指定のない場合はすべてのユーザDBを対象に処理します。
      ▼ o：収集するデータの出力先フォルダをしていします。相対パスを指定した場合は
            このexe直下にフォルダを作成します。既定値は「output」です。
      ▼ s：このパラメータの指定がない場合は収集実行前に対象となるDB数を通知し実行継続を確認します。
            ------------------------------------------------------------------------------------------------------------------
            The target SQL Server has 2 databases to collect data.Processing takes time depending on the number of databases.

            To continue the process, type Y, otherwise type N.

            (Type Y or N) :
            ------------------------------------------------------------------------------------------------------------------
            この事前確認を実施しない場合にこのパラメータを指定します。
      ▼ t：コマンドタイムアウト値を秒単位で指定します。既定値は「300秒（5分）」です。
      ▼ l：ロックタイムアウト値を秒単位で指定します。既定値は「180秒（3分）」です。

        例）

        ・ 単一DB指定
            > mssqlstats.exe -E -S server -d db_name

        ・ 複数DB指定
            > mssqlstats.exe -E -S server -D db_name, db_name, ...

        ・ 結果出力先指定
            > mssqlstats.exe -E -S server -o path

        ・ サイレントモード（データ収集の実行前に事前確認を実施しない）
            > mssqlstats.exe -E -S server –s
    
    ● 外部コマンド指定パラメータ

      ▼ Q：外部のコマンド（*.sqlファイルに記述されたT-SQL）を実行したい場合、そのコマンド
            ファイルの格納されたフォルダを指定します。
			ファイル名が"Instance（大文字小文字を区別しない）"で開始されていた場合はインスタンスレベルのクエリであると解釈し、
			そうでない場合かつDB名が指定されている場合はDBレベルのクエリであると解釈します。
      ▼ q：ツールが既定で実行するコマンドは実行せず、外部のコマンドのみを実行したい場合に
            このパラメータを指定します。

        例）

        ・ 外部のコマンドフォルダ指定（該当フォルダ配下の*.sqlがすべて実行されます）
            > mssqlstats.exe -E -S server -Q dir_path

        ・ 外部のコマンドのみを実行
            > mssqlstats.exe -E -S server -Q dir_path -q
