using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Remoting.Messaging;

namespace Jomura.Data.Util
{
    /// <summary>
    /// データベースを差分複製する。
    /// 
    /// ・差異がないレコードについては何もしない。
    /// ・差異があってもよい（更新しない）カラムの指定が可能。
    /// ・新規テーブルの自動生成、テーブルの自動削除が可能。PrimaryKeyの複製に対応。
    ///   （外部キーの複製には非対応）
    /// ・テーブル単位で、並列に処理する。
    /// 
    /// ・同一DBMS内転送(接続元と接続先のDB接続文字列が同じ)の場合、表毎に、対象行を一括更新(高速)。
    /// ・異DBMS間転送(接続元と接続先のDB接続文字列が異なる)の場合、行毎更新(低速)。
    /// </summary>
    public class DatabaseCopy
    {
        /// <summary>
        /// 行一致判定をする際に、無視するカラム
        /// 
        /// PKは含めないでください。
        /// </summary>
        string[] ignoreColumns = new string[0];

        /// <summary>
        /// 行毎にSQLを発行する場合に、一括実行するSQL数
        /// </summary>
        readonly int BATCH_SIZE = 200;
        
        // マルチスレッド化のためのデリゲート
        delegate void CopyTableDgt(string srcConnStr, string destConnStr, string tableName);

        /// <summary>
        /// コマンドタイムアウト時間。
        /// 指定しなければ"-1"が設定され、.NETのdefaut値(60秒)が適応される。
        /// </summary>
        public int CommandTimeout
        {
            get { return m_CommandTimeout; }
            set { m_CommandTimeout = value; }
        }
        int m_CommandTimeout = -1;

        /// <summary>
        /// SQL種別毎の処理数
        /// Counts[Create|Drop] … 処理されたテーブル数
        /// Counts[Insert|Update|Delete] … 処理されたレコード数
        /// </summary>
        public Dictionary<SqlType, int> Counts
        {
            get { return counts; }
        }
        Dictionary<SqlType, int> counts = new Dictionary<SqlType, int>();

        #region Constructors

        /// <summary>
        /// 差分比較する際に無視するカラム名を指定しないコンストラクタ
        /// </summary>
        public DatabaseCopy()
        {
            foreach (SqlType sqlType in Enum.GetValues(typeof(SqlType)))
            {
                counts[sqlType] = 0;
            }
        }

        /// <summary>
        /// 差分比較する際に無視するカラム名を指定するコンストラクタ
        /// </summary>
        /// <param name="ignoreColumns">無視するカラム名</param>
        public DatabaseCopy(string[] ignoreColumns)
            : this()
        {
            if (ignoreColumns != null)
            {
                this.ignoreColumns = ignoreColumns;
            }
        }

        #endregion

        /// <summary>
        /// データベースの内容を複製する。
        /// ただし、以下を前提とする。
        /// (1)コピー先に対象のデータベースは存在している
        /// (2)コピー先テーブルのスキーマは、コピー元の同名テーブルと同じ
        /// 
        /// データベース名はDB接続文字列(Initial Catalog)で指定する。
        /// </summary>
        /// <param name="srcConnStr">コピー元DB接続</param>
        /// <param name="destConnStr">コピー先DB接続</param>
        public void CopyDataBase(string srcConnStr, string destConnStr)
        {
            // コピー元のテーブル一覧を取得する
            List<string> srcTableNames = GetTableNames(srcConnStr);

            // コピー先のテーブル一覧を取得する
            List<string> destTableNames = GetTableNames(destConnStr);

            List<IAsyncResult> results = new List<IAsyncResult>();
            foreach (string destTableName in destTableNames)
            {
                if (!srcTableNames.Contains(destTableName))
                {
                    // DropされたテーブルをDropする
                    //TODO ログ出力(destTableName, "drop");
                    Trace.TraceInformation("drop " + destTableName);

                    string sql = MakeDropTableSql(destTableName);
                    counts[SqlType.Drop] -= ExecuteNonQuery(destConnStr, sql);
                }
                else
                {
                    // コピー先にあるテーブルを差分Updateする。
                    // この処理は、非同期に実行する。
                    //TODO ログ出力(destTableName, "update");
                    Trace.TraceInformation("update " + destTableName);

                    CopyTableDgt copyTableDgt = new CopyTableDgt(CopyTable);
                    IAsyncResult result = copyTableDgt.BeginInvoke(srcConnStr, destConnStr, destTableName,
                        new AsyncCallback(CopyTableCallback), destTableName);
                    results.Add(result);
                }
            }

            // 並列処理の終了を待つ
            foreach (IAsyncResult result in results)
            {
                result.AsyncWaitHandle.WaitOne();
            }

            results.Clear();

            foreach (string srcTableName in srcTableNames)
            {
                if (!destTableNames.Contains(srcTableName))
                {
                    // CreateされたテーブルをCreateする。
                    string sql = MakeCreateTableSql(srcConnStr, srcTableName);
                    counts[SqlType.Create] -= ExecuteNonQuery(destConnStr, sql);

                    // Createされたテーブルにデータを全件Insertする。
                    //TODO ログ出力(srcTableName, "insert");
                    Trace.TraceInformation("insert " + srcTableName);

                    CopyTableDgt copyTableDgt = new CopyTableDgt(CopyTable);
                    IAsyncResult result = copyTableDgt.BeginInvoke(srcConnStr, destConnStr, srcTableName,
                        new AsyncCallback(CopyTableCallback), srcTableName);
                    results.Add(result);
                }
            }

            // 並列処理の終了を待つ
            foreach (IAsyncResult result in results)
            {
                result.AsyncWaitHandle.WaitOne();
            }
        }

        #region Overloads of CopyDataBase

        /// <summary>
        /// 別サーバの別名DBへ複製する。
        /// 
        /// 各接続文字列の"Initial Catalog"を、指定されたDB名に変更してから、処理を開始する。
        /// </summary>
        /// <param name="srcConnStr">複製元DB接続文字列</param>
        /// <param name="destConnStr">複製先DB接続文字列</param>
        /// <param name="srcDBName">複製元DB名</param>
        /// <param name="destDBName">複製先DB名</param>
        public void CopyDataBase(string srcConnStr, string destConnStr,
            string srcDBName, string destDBName)
        {
            SqlConnectionStringBuilder srcConnBuilder = new SqlConnectionStringBuilder(srcConnStr);
            srcConnBuilder.InitialCatalog = srcDBName;
            SqlConnectionStringBuilder destConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            destConnBuilder.InitialCatalog = destDBName;

            CopyDataBase(srcConnBuilder.ConnectionString, destConnBuilder.ConnectionString);
        }

        /// <summary>
        /// 同一サーバの別名DBへ複製する。
        /// 
        /// 接続文字列の"Initial Catalog"を、指定されたDB名に変更してから、処理を開始する。
        /// </summary>
        /// <param name="destConnStr">複製先DB接続</param>
        /// <param name="srcDBName">複製元DB名</param>
        /// <param name="destDBName">複製先DB名</param>
        public void CopyDataBase(string destConnStr, string srcDBName, string destDBName)
        {
            SqlConnectionStringBuilder srcConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            srcConnBuilder.InitialCatalog = srcDBName;
            SqlConnectionStringBuilder destConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            destConnBuilder.InitialCatalog = destDBName;

            CopyDataBase(srcConnBuilder.ConnectionString, destConnBuilder.ConnectionString);
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// 各並列処理の終了時に、例外が発生していた場合の処理
        /// </summary>
        /// <param name="ar">非同期実行結果</param>
        static void CopyTableCallback(IAsyncResult ar)
        {
            AsyncResult aResult = ar as AsyncResult;
            CopyTableDgt dgt = aResult.AsyncDelegate as CopyTableDgt;
            try
            {
                dgt.EndInvoke(ar);
            }
            catch (Exception ex)
            {
                string tableName = ar.AsyncState as string;

                //TODO エラーログ出力
                Trace.TraceError(tableName + " : " + ex.Message);

                //throw;
            }
        }

        #endregion


        #region Database関連操作メソッド

        List<string> GetTableNames(string connStr)
        {
            List<string> tables = new List<string>();

            string sql = @"
select name
 from sys.objects
 where type = N'U'
 and is_ms_shipped = 0
 order by name
";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand selectCmd = new SqlCommand(sql, conn);
                if (CommandTimeout != -1)
                {
                    selectCmd.CommandTimeout = CommandTimeout;
                }
                using (IDataReader dr = selectCmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        tables.Add(dr["name"] as String);
                    }
                }
            }
            return tables;
        }

        #endregion
        
        #region Table操作関連メソッド

        int ExecuteNonQuery(string connStr, string sql)
        {
            return ExecuteNonQuery(connStr, sql, null);
        }

        int ExecuteNonQuery(string connStr, string sql, SqlParameter[] parameters)
        {
            //TODO SQLログ出力
            Trace.TraceInformation("sql : " + sql);

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                if (null != parameters)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                if (CommandTimeout != -1)
                {
                    cmd.CommandTimeout = CommandTimeout;
                }
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 複製元テーブルから"Create Table"文を生成する。
        /// </summary>
        /// <param name="connStr">複製元DB接続文字列</param>
        /// <param name="tableName">テーブル名</param>
        /// <returns>"Create Table"SQL文</returns>
        string MakeCreateTableSql(string connStr, string tableName)
        {
            StringBuilder createSql = new StringBuilder();
            createSql.AppendFormat("create table {0} (\n", tableName);

            string sql = @"
select c.name 'columnname'
      ,t.name 'type'
      ,case t.name
        when 'binary' THEN '(' + cast(c.length as varchar) + ')'
        when 'char' THEN '(' + cast(c.length as varchar) + ')'
        when 'nchar' THEN '(' + cast(c.length as varchar) + ')'
        when 'nvarchar' THEN '(' + (case c.length when -1 then 'MAX' else cast(c.length as varchar) end) + ')'
        when 'varbinary' THEN '(' + (case c.length when -1 then 'MAX' else cast(c.length as varchar) end) + ')'
        when 'varchar' THEN '(' + (case c.length when -1 then 'MAX' else cast(c.length as varchar) end) + ')'
        when 'decimal' THEN '(' + cast(c.xprec as varchar) + ',' + cast(c.xscale as varchar) + ')'
        when 'numeric' THEN '(' + cast(c.xprec as varchar) + ',' + cast(c.xscale as varchar) + ')'
        else ''
       END 'size'
      ,case c.isnullable
        when 0 THEN 'NOT NULL'
        when 1 THEN 'NULL'
       END 'nullable'
 from sysobjects o
 inner join syscolumns c
  on o.id = c.id
 inner join systypes t
  on c.xusertype = t.xusertype
 where o.type = N'U'
 and o.name = @tablename
 order by c.colid
";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand selectCmd = new SqlCommand(sql, conn);
                selectCmd.Parameters.Add(new SqlParameter("@tablename", tableName));
                if (CommandTimeout != -1)
                {
                    selectCmd.CommandTimeout = CommandTimeout;
                }

                using (IDataReader dr = selectCmd.ExecuteReader())
                {
                    string comma = " ";
                    while (dr.Read())
                    {
                        object[] prms = new object[] { comma, dr["columnname"], dr["type"], dr["size"], dr["nullable"] };
                        createSql.AppendFormat(CultureInfo.CurrentCulture, " {0}{1} {2}{3} {4}\n", prms);
                        comma = ",";
                    }
                }
            }

            createSql.Append(MakePrimaryKeyStr(connStr, tableName));
            createSql.Append(")");
            return createSql.ToString();
        }

        string MakePrimaryKeyStr(string connStr, string tableName)
        {
            string[] pkeys = GetPrimaryKeys(connStr, tableName);
            if (pkeys.Length == 0)
            {
                return string.Empty;
            }
            string pkStr = string.Join(", ", pkeys);
            return string.Format(CultureInfo.CurrentCulture, " ,PRIMARY KEY ({0})\n", pkStr);
        }

        static string MakeDropTableSql(string tableName)
        {
            return string.Format(CultureInfo.CurrentCulture, "drop table {0}", tableName);
        }

        #endregion

        #region Row操作関連メソッド

        /// <summary>
        /// テーブル内のデータを差分複製する。
        /// 同じスキーマのテーブルが、複製元・複製先の双方に存在していることが前提。
        /// 
        /// 複製元と複製先のDataSourceが同じ場合は、
        /// 表毎にSQL3つでまとめて処理するので高速。
        /// 複製元と複製先のDataSourceが異なる場合は、
        /// 行毎にSQLを発行するので低速。
        /// </summary>
        /// <param name="srcConnStr">複製元DB接続</param>
        /// <param name="destConnStr">複製先DB接続</param>
        /// <param name="tableName">複製する表名</param>
        public void CopyTable(string srcConnStr, string destConnStr, string tableName)
        {
            SqlConnectionStringBuilder srcConnBuilder = new SqlConnectionStringBuilder(srcConnStr);
            SqlConnectionStringBuilder destConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            if (srcConnBuilder.DataSource == destConnBuilder.DataSource)
            {
                CopyTableLocal(destConnStr, srcConnBuilder.InitialCatalog,
                    destConnBuilder.InitialCatalog, tableName);
            }
            else
            {
                CopyTableRemote(srcConnStr, destConnStr, tableName);
            }
        }

        void CopyTableLocal(string connStr, string srcDbName, string destDbName, string tableName)
        {
            string sql = @"
--declare @tablename varchar(255),@src_db varchar(255),@dst_db varchar(255)
--set @tablename = 'T_IDX_AREA'
--set @src_db = 'LOKMSTIDX'
--set @dst_db = 'LOKMSTIDX_Test'

-- 更新行数
DECLARE @INS_CNT int
       ,@UPD_CNT int
       ,@DEL_CNT int

-- ループ変数
DECLARE @loop_count int
DECLARE @name varchar(255)
DECLARE @nullable int

-- カーソル
DECLARE pk_cursor CURSOR FOR 
	SELECT C.name 'columnname'
	FROM sysindexkeys IK, sysobjects O, syscolumns C,
	 (SELECT I.id, I.indid, I.status
	 FROM sysindexes I
	 WHERE (I.status & 2048)<>0
	 ) V_PR
	WHERE IK.id = O.id
	AND IK.id = C.id
	AND IK.colid = C.colid
	AND IK.id = V_PR.id
	AND IK.indid = V_PR.indid
	AND O.name = @tablename
	ORDER BY IK.id, IK.indid, IK.keyno
DECLARE notpk_cursor CURSOR FOR 
	select c.name
	 from sysobjects o
	 inner join syscolumns c
	  on o.id = c.id
	 where o.type = N'U'
	 and o.name = @tablename
	 and not exists (
			SELECT *
			FROM sysindexkeys IK,
			 (SELECT I.id, I.indid, I.status
			 FROM sysindexes I
			 WHERE (I.status & 2048)<>0
			 ) V_PR
			WHERE IK.id = o.id
			AND IK.id = c.id
			AND IK.colid = c.colid
			AND IK.id = V_PR.id
			AND IK.indid = V_PR.indid
	 )
	 order by c.colid
DECLARE col_cursor CURSOR FOR 
	select c.name
		  ,c.isnullable
	 from sysobjects o
	 inner join syscolumns c
	  on o.id = c.id
	 where o.type = N'U'
	 and o.name = @tablename
     and c.name not in (
		 '" + string.Join(@"'
		,'", ignoreColumns) + @"')
	 order by c.colid


-- (1)DELETE
	DECLARE @del_sql nvarchar(1024)
	SET @del_sql='DELETE X FROM ' + @dst_db + '..' + @tablename + ' X' + CHAR(13) + CHAR(10)
	 + ' WHERE NOT EXISTS (' + CHAR(13) + CHAR(10)
	 + '   SELECT *' + CHAR(13) + CHAR(10)
	 + '   FROM ' + @src_db + '..' + @tablename + ' Y' + CHAR(13) + CHAR(10)
	OPEN pk_cursor 
	SET @loop_count = 0
	FETCH NEXT FROM pk_cursor INTO @name
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @loop_count = 0
			set @del_sql = @del_sql + ' WHERE'
		ELSE
			set @del_sql = @del_sql + ' AND'
		set @del_sql = @del_sql + ' X.' + @name + ' = Y.' + @name + CHAR(13) + CHAR(10)
		SET @loop_count = @loop_count + 1
		FETCH NEXT FROM pk_cursor INTO @name
	END
	CLOSE pk_cursor
	set @del_sql = @del_sql + ' )'

--	print '@del_sql : ' + @del_sql
	EXECUTE sp_executesql @del_sql
    SET @DEL_CNT = @@ROWCOUNT    --件数

-- (2)INSERT
	DECLARE @ins_sql nvarchar(1024)
	SET @ins_sql='INSERT INTO ' + @dst_db + '..' + @tablename + CHAR(13) + CHAR(10)
	 + '   SELECT *' + CHAR(13) + CHAR(10)
	 + '   FROM ' + @src_db + '..' + @tablename + ' Y' + CHAR(13) + CHAR(10)
     + '   WHERE NOT EXISTS (' + CHAR(13) + CHAR(10)
     + '     SELECT *' + CHAR(13) + CHAR(10)
     + '     FROM ' + @dst_db + '..' + @tablename + ' X' + CHAR(13) + CHAR(10)
	OPEN pk_cursor 
	SET @loop_count = 0
	FETCH NEXT FROM pk_cursor INTO @name
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @loop_count = 0
			set @ins_sql = @ins_sql + ' WHERE'
		ELSE
			set @ins_sql = @ins_sql + ' AND'
		set @ins_sql = @ins_sql + ' X.' + @name + ' = Y.' + @name + CHAR(13) + CHAR(10)
		SET @loop_count = @loop_count + 1
		FETCH NEXT FROM pk_cursor INTO @name
	END
	CLOSE pk_cursor
	set @ins_sql = @ins_sql + ' )'

--	print '@ins_sql : ' + @ins_sql
	EXECUTE sp_executesql @ins_sql
    SET @INS_CNT = @@ROWCOUNT    --件数

-- (3)UPDATE
	DECLARE @upd_sql nvarchar(4000)
	SET @upd_sql='UPDATE ' + @dst_db + '..' + @tablename + CHAR(13) + CHAR(10)
	OPEN notpk_cursor 
	SET @loop_count = 0
	FETCH NEXT FROM notpk_cursor INTO @name
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @loop_count = 0
			set @upd_sql = @upd_sql + ' SET '
		ELSE
			set @upd_sql = @upd_sql + '    ,'
		set @upd_sql = @upd_sql + @name + ' = Y.' + @name + CHAR(13) + CHAR(10)
		SET @loop_count = @loop_count + 1
		FETCH NEXT FROM notpk_cursor INTO @name
	END
	CLOSE notpk_cursor
    set @upd_sql = @upd_sql + ' FROM ' + @dst_db + '..' + @tablename + ' X' + CHAR(13) + CHAR(10)
        + ' INNER JOIN ' + @src_db + '..' + @tablename + ' Y' + CHAR(13) + CHAR(10)
	OPEN pk_cursor 
	SET @loop_count = 0
	FETCH NEXT FROM pk_cursor INTO @name
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @loop_count = 0
			set @upd_sql = @upd_sql + ' ON '
		ELSE
			set @upd_sql = @upd_sql + ' AND'
		set @upd_sql = @upd_sql + ' X.' + @name + ' = Y.' + @name + CHAR(13) + CHAR(10)
		SET @loop_count = @loop_count + 1
		FETCH NEXT FROM pk_cursor INTO @name
	END
	CLOSE pk_cursor
    set @upd_sql = @upd_sql + ' WHERE NOT EXISTS (' + CHAR(13) + CHAR(10)
        + '   SELECT * FROM ' + @src_db + '..' + @tablename + ' Z' + CHAR(13) + CHAR(10)

	OPEN col_cursor 
	SET @loop_count = 0
	FETCH NEXT FROM col_cursor INTO @name, @nullable
	WHILE @@FETCH_STATUS = 0
	BEGIN
		IF @loop_count = 0
			set @upd_sql = @upd_sql + ' WHERE'
		ELSE
			set @upd_sql = @upd_sql + ' AND'
		set @upd_sql = @upd_sql + ' (X.' + @name + ' = Z.' + @name
		IF @nullable = 0
	        set @upd_sql = @upd_sql + ')' + CHAR(13) + CHAR(10)
		ELSE
	        set @upd_sql = @upd_sql + ' OR (X.' + @name + ' is null AND Z.' + @name + ' is null))' + CHAR(13) + CHAR(10)
		SET @loop_count = @loop_count + 1
		FETCH NEXT FROM col_cursor INTO @name, @nullable
	END
	CLOSE col_cursor
	set @upd_sql = @upd_sql + ' )'

--	print '@upd_sql : ' + @upd_sql
	EXECUTE sp_executesql @upd_sql
    SET @UPD_CNT = @@ROWCOUNT    --件数

-- fin.
DEALLOCATE pk_cursor
DEALLOCATE notpk_cursor
DEALLOCATE col_cursor

SELECT @INS_CNT AS InsertRows,
       @UPD_CNT AS UpdateRows,
       @DEL_CNT AS DeleteRows
";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand selectCmd = new SqlCommand(sql, conn);
                selectCmd.Parameters.Add(new SqlParameter("@tablename", tableName));
                selectCmd.Parameters.Add(new SqlParameter("@src_db", srcDbName));
                selectCmd.Parameters.Add(new SqlParameter("@dst_db", destDbName));
                if (CommandTimeout != -1)
                {
                    selectCmd.CommandTimeout = CommandTimeout;
                }
                using (IDataReader dr = selectCmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lock (counts)
                        {
                            counts[SqlType.Delete] += (int)dr["DeleteRows"];
                            counts[SqlType.Insert] += (int)dr["InsertRows"];
                            counts[SqlType.Update] += (int)dr["UpdateRows"];
                        }
                    }
                }
            }
        }

        void CopyTableRemote(string srcConnStr, string destConnStr, string tableName)
        {
            //更新行数
            int delCnt = 0;
            int insCnt = 0;
            int updCnt = 0;

            using (DataSet destDS = new DataSet())
            using (DataSet srcDS = new DataSet())
            {
                // (1)コピー先のテーブルデータをDataSetに読み込む
                destDS.Locale = CultureInfo.InvariantCulture;
                GetTableData(destConnStr, tableName, destDS);

                // (2)コピー元のテーブルデータをDataSetに読み込む
                srcDS.Locale = CultureInfo.InvariantCulture;
                GetTableData(srcConnStr, tableName, srcDS);

                DataTable destDT = destDS.Tables[tableName];
                DataTable srcDT = srcDS.Tables[tableName];

                Debug.WriteLine(DateTime.Now + " " + tableName + ".Rows.Count(before) : " + destDT.Rows.Count);
                Debug.WriteLine(DateTime.Now + " " + tableName + ".Rows.Count(after) : " + srcDT.Rows.Count);

                // PrimaryKeys取得
                List<string> pks = new List<string>(GetPrimaryKeys(destConnStr, tableName));

                List<string> batch_del = new List<string>();

                // (3)コピー先の行データのうち、コピー元に存在しない行を削除
                foreach (DataRow destRow in destDT.Rows)
                {
                    // PK一致の行があるか？ Yes→continue  No→DELETE
                    List<string> expression = new List<string>();
                    foreach (DataColumn column in destDT.Columns)
                    {
                        if (pks.Contains(column.ColumnName))
                        {
                            expression.Add(column.ColumnName + "='" + escape(destRow[column]) + "'");
                        }
                    }
                    DataRow[] pkRow = srcDT.Select(string.Join(" and ", expression.ToArray()));
                    if (pkRow.Length == 0)
                    {
                        // DELETEクエリ作成・実行
                        StringBuilder deleteSql = new StringBuilder();
                        deleteSql.Append("delete from " + tableName);
                        deleteSql.Append(" where " + string.Join(" and ", expression.ToArray()));

                        //Debug.WriteLine("delete sql : " + deleteSql.ToString());
                        batch_del.Add(deleteSql.ToString());
                        if (batch_del.Count > BATCH_SIZE)
                        {
                            delCnt += ExecuteNonQuery(destConnStr, string.Join("\n", batch_del.ToArray()));
                            batch_del.Clear();
                        }
                    }
                }
                if (batch_del.Count > 0)
                {
                    delCnt += ExecuteNonQuery(destConnStr, string.Join("\n", batch_del.ToArray()));
                    batch_del.Clear();
                }

                List<string> batch_upd = new List<string>();
                List<string> batch_ins = new List<string>();

                // (4)DataSetの値とコピー元のテーブルデータを比較し、
                //    DataSetを更新する。
                foreach (DataRow srcRow in srcDT.Rows)
                {
                    // 全カラム一致の行があるか？ Yes→continue  No→INSERT or UPDATE
                    List<string> expression = new List<string>();
                    List<string> expression4pk = new List<string>();
                    foreach (DataColumn column in srcDT.Columns)
                    {
                        // 行一致条件に含めないカラムを無視
                        if (-1 != Array.IndexOf(ignoreColumns, column.ColumnName))
                        {
                            continue;
                        }

                        string itemOfExpression = string.Empty;
                        // 行一致条件文作成
                        if (srcRow[column] is DBNull)
                        {
                            itemOfExpression = column.ColumnName + " is null";
                        }
                        else
                        {
                            itemOfExpression = column.ColumnName + "='" + escape(srcRow[column]) + "'";
                        }
                        expression.Add(itemOfExpression);

                        // PK一致条件文作成
                        if (pks.Contains(column.ColumnName))
                        {
                            expression4pk.Add(itemOfExpression);
                        }
                    }

                    string expressionStr = string.Join(" and ", expression.ToArray());

                    DataRow[] destRows = destDT.Select(expressionStr);
                    if (destRows.Length != 0) continue;

                    // PK一致の行があるか？ Yes→UPDATE  No→INSERT
                    string expression4pkStr = string.Join(" and ", expression4pk.ToArray());
                    DataRow[] pkRow = destDT.Select(expression4pkStr);
                    if (pkRow.Length != 0)
                    {
                        // UPDATE
                        // SQL分作成
                        StringBuilder updateSql = new StringBuilder();
                        updateSql.Append("update " + tableName + " set ");
                        List<string> expression4upd = new List<string>();
                        foreach (DataColumn column in srcDT.Columns)
                        {
                            if (!pks.Contains(column.ColumnName))
                            {
                                if (srcRow[column] is DBNull)
                                {
                                    expression4upd.Add(column.ColumnName + " = null");
                                }
                                else
                                {
                                    expression4upd.Add(column.ColumnName + " = '" + escape(srcRow[column]) + "'");
                                }
                            }
                        }
                        updateSql.Append(string.Join(", ", expression4upd.ToArray()));
                        updateSql.Append(" where " + expression4pkStr);

                        //Debug.WriteLine("update sql : " + updateSql.ToString());
                        batch_upd.Add(updateSql.ToString());
                        if (batch_upd.Count > BATCH_SIZE)
                        {
                            updCnt += ExecuteNonQuery(destConnStr, string.Join("\n", batch_upd.ToArray()));
                            batch_upd.Clear();
                        }
                    }
                    else
                    {
                        // INSERT
                        // SQL分作成
                        StringBuilder insertSql = new StringBuilder();
                        insertSql.Append("insert into " + tableName + " values (");
                        List<string> expression4ist = new List<string>();
                        foreach (DataColumn column in srcDT.Columns)
                        {
                            if (srcRow[column] is DBNull)
                            {
                                expression4ist.Add("null");
                            }
                            else
                            {
                                expression4ist.Add("'" + escape(srcRow[column]) + "'");
                            }
                        }
                        insertSql.Append(string.Join(",", expression4ist.ToArray()) + ")");

                        //Debug.WriteLine("insert sql : " + insertSql.ToString());
                        batch_ins.Add(insertSql.ToString());
                        if (batch_ins.Count > BATCH_SIZE)
                        {
                            insCnt += ExecuteNonQuery(destConnStr, string.Join("\n", batch_ins.ToArray()));
                            batch_ins.Clear();
                        }
                    }
                }//end foreach loop
                if (batch_upd.Count > 0)
                {
                    updCnt += ExecuteNonQuery(destConnStr, string.Join("\n", batch_upd.ToArray()));
                    batch_upd.Clear();
                }
                if (batch_ins.Count > 0)
                {
                    insCnt += ExecuteNonQuery(destConnStr, string.Join("\n", batch_ins.ToArray()));
                    batch_ins.Clear();
                }
            }

            lock (counts)
            {
                counts[SqlType.Delete] += delCnt;
                counts[SqlType.Insert] += insCnt;
                counts[SqlType.Update] += updCnt;
            }

            Debug.WriteLine(DateTime.Now + " " + tableName + " updated");
        }

        #region Overloads of CopyTable

        /// <summary>
        /// 別サーバの別名DBへ複製する。
        /// 
        /// 各接続文字列の"Initial Catalog"を、指定されたDB名に変更してから、処理を開始する。
        /// </summary>
        /// <param name="srcConnStr">複製元DB接続文字列</param>
        /// <param name="destConnStr">複製先DB接続文字列</param>
        /// <param name="srcDBName">複製元DB名</param>
        /// <param name="destDBName">複製先DB名</param>
        /// <param name="tableName">表名</param>
        public void CopyTable(string srcConnStr, string destConnStr,
            string srcDBName, string destDBName, string tableName)
        {
            SqlConnectionStringBuilder srcConnBuilder = new SqlConnectionStringBuilder(srcConnStr);
            srcConnBuilder.InitialCatalog = srcDBName;
            SqlConnectionStringBuilder destConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            destConnBuilder.InitialCatalog = destDBName;

            CopyTable(srcConnBuilder.ConnectionString, destConnBuilder.ConnectionString, tableName);
        }

        /// <summary>
        /// 同一サーバの別名DBへ複製する。
        /// 
        /// 接続文字列の"Initial Catalog"を、指定されたDB名に変更してから、処理を開始する。
        /// </summary>
        /// <param name="destConnStr">複製先DB接続文字列</param>
        /// <param name="srcDBName">複製元DB名</param>
        /// <param name="destDBName">複製先DB名</param>
        /// <param name="tableName">表名</param>
        public void CopyTable(string destConnStr, string srcDBName, string destDBName, string tableName)
        {
            SqlConnectionStringBuilder srcConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            srcConnBuilder.InitialCatalog = srcDBName;
            SqlConnectionStringBuilder destConnBuilder = new SqlConnectionStringBuilder(destConnStr);
            destConnBuilder.InitialCatalog = destDBName;

            CopyTable(srcConnBuilder.ConnectionString, destConnBuilder.ConnectionString, tableName);
        }

        #endregion

        void GetTableData(string connStr, string tableName, DataSet ds)
        {
            string sql = string.Format(CultureInfo.CurrentCulture, "SELECT * FROM {0}", tableName);

            using (SqlConnection conn = new SqlConnection(connStr))
            // データアダプタの作成
            using (SqlDataAdapter da = new SqlDataAdapter())
            {
                conn.Open();
                // select用コマンド・オブジェクトの作成
                da.SelectCommand = new SqlCommand(sql, conn);
                if (CommandTimeout != -1)
                {
                    da.SelectCommand.CommandTimeout = CommandTimeout;
                }

                // データセットへの読み込み
                da.Fill(ds, tableName);
            }
        }

        string[] GetPrimaryKeys(string connStr, string tableName)
        {
            List<string> pkeys = new List<string>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand selectCmd = new SqlCommand("sp_pkeys", conn);
                selectCmd.CommandType = CommandType.StoredProcedure;
                selectCmd.Parameters.Add(new SqlParameter("@table_name", tableName));
                if (CommandTimeout != -1)
                {
                    selectCmd.CommandTimeout = CommandTimeout;
                }

                using (IDataReader dr = selectCmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        pkeys.Add(dr["COLUMN_NAME"] as string);
                    }
                }
            }

            return pkeys.ToArray();
        }

        #endregion 

        #region その他のユーティリティメソッド

        static string escape(object sqlval)
        {
            return Convert.ToString(sqlval, CultureInfo.CurrentCulture).Replace("'", "''");
        }

        #endregion

    }//eof class
}//eof namespace
