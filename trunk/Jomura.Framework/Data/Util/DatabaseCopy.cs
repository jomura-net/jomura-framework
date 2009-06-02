using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.Runtime.Remoting.Messaging;
using System.Data.Common;

namespace Jomura.Data.Util
{
    /// <summary>
    /// データベースを差分複製する。
    /// 
    /// ・差異がないレコードについては何もしない。
    /// ・差異があってもよいカラムの指定が可能。
    /// ・新規テーブルの自動生成、テーブルの自動削除が可能。PrimaryKeyの複製に対応。
    ///   （外部キーの複製には非対応）
    /// ・テーブル単位で、非同期に複製処理を実施する。
    /// </summary>
    public class DatabaseCopy
    {
        /// <summary>
        /// 行一致判定をする際に、無視するカラム
        /// 
        /// PKは含めないでください。
        /// </summary>
        string[] ignoreColumns = new string[0];

        // マルチスレッド化のためのデリゲート
        delegate void CopyTableDgt(SqlConnection srcConn, SqlConnection destConn,
            string databaseName, string tableName);

        /// <summary>
        /// DDL種別毎の処理数
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
        /// (2)コピー先テーブルのスキーマを変更していない
        /// </summary>
        /// <param name="srcConn">コピー元DB接続</param>
        /// <param name="destConn">コピー先DB接続</param>
        /// <param name="databaseName">データベース名</param>
        public void CopyDataBase(SqlConnection srcConn, SqlConnection destConn, string databaseName)
        {
            // コピー元のテーブル一覧を取得する
            OpenConnectionAndChangeDatabase(srcConn, databaseName);
            List<string> srcTableNames = GetTableNames(srcConn, databaseName);

            // コピー先のテーブル一覧を取得する
            OpenConnectionAndChangeDatabase(destConn, databaseName);
            List<string> destTableNames = GetTableNames(destConn, databaseName);

            List<IAsyncResult> results = new List<IAsyncResult>();
            foreach (string destTableName in destTableNames)
            {
                if (!srcTableNames.Contains(destTableName))
                {
                    // DropされたテーブルをDropする
                    //TODO ログ出力(databaseName, destTableName, "drop");
                    counts[SqlType.Drop] += DropTable(destConn, databaseName, destTableName);
                }
                else
                {
                    // コピー先にあるテーブルを差分Updateする
                    // DataSetを使って、非接続でテーブル一括更新を行う
                    // メモリ消費、反映の早さから、テーブル単位で更新する
                    CopyTableDgt copyTableDgt = new CopyTableDgt(CopyTable);
                    //TODO ログ出力(databaseName, destTableName, "update");
                    IAsyncResult result = copyTableDgt.BeginInvoke(srcConn, destConn, databaseName, destTableName,
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
                    string sql = GetCreateTableSql(srcConn, databaseName, srcTableName);
                    counts[SqlType.Create] += ExecuteNonQuery(destConn, sql);

                    // Createされたテーブルにデータを全件Insertする。
                    //TODO ログ出力(databaseName, srcTableName, "insert");
                    CopyTableDgt copyTableDgt = new CopyTableDgt(CopyTable);
                    IAsyncResult result = copyTableDgt.BeginInvoke(srcConn, destConn, databaseName, srcTableName,
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
                Debug.WriteLine("tableName:" + tableName  + " " + ex);

                //throw;
            }
        }

        #endregion


        #region Database関連操作メソッド

        static List<string> GetTableNames(SqlConnection conn, string databaseName)
        {
            List<string> tables = new List<string>();

            string sql = string.Format(CultureInfo.CurrentCulture, @"
select name
 from sys.objects
 where type = N'U'
 and is_ms_shipped = 0
 order by name
", databaseName);

            SqlCommand selectCmd = new SqlCommand(sql, conn);
            using (IDataReader dr = selectCmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    tables.Add(dr["name"] as String);
                }
            }
            return tables;
        }

        static void OpenConnectionAndChangeDatabase(SqlConnection conn, string databaseName)
        {
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            conn.ChangeDatabase(databaseName);
        }

        #endregion
        
        #region Table操作関連メソッド

        static int ExecuteNonQuery(SqlConnection conn, string sql)
        {
            //TODO SQLログ出力

            SqlCommand cmd = new SqlCommand(sql, conn);
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// コピー元テーブルから"Create Table"文を生成する。
        /// </summary>
        /// <param name="conn">コピー元DB接続</param>
        /// <param name="databaseName">データベース名</param>
        /// <param name="tableName">テーブル名</param>
        /// <returns>"Create Table"SQL文</returns>
        static string GetCreateTableSql(SqlConnection conn, string databaseName, string tableName)
        {
            StringBuilder createSql = new StringBuilder();
            createSql.AppendFormat("create table {0} (\n", tableName);

            string sql = string.Format(CultureInfo.CurrentCulture, @"
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
", databaseName); 
            
            SqlCommand selectCmd = new SqlCommand(sql, conn);
            selectCmd.Parameters.Add(new SqlParameter("@tablename", tableName));

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

            createSql.Append(GetPrimaryKeyStr(conn, databaseName, tableName));
            createSql.Append(")");
            return createSql.ToString();
        }

        static string GetPrimaryKeyStr(SqlConnection conn, string databaseName, string tableName)
        {
            string[] pkeys = GetPrimaryKeys(conn, databaseName, tableName);
            if (pkeys.Length == 0)
            {
                return string.Empty;
            }
            string pkStr = string.Join(", ", pkeys);
            return string.Format(CultureInfo.CurrentCulture, " ,PRIMARY KEY ({0})\n", pkStr);
        }

        static int DropTable(SqlConnection conn, string databaseName, string tableName)
        {
            string sql = string.Format(CultureInfo.CurrentCulture, @"
drop table {1}
", databaseName, tableName);

            return ExecuteNonQuery(conn, sql);
        }

        #endregion

        #region Row操作関連メソッド

        /// <summary>
        /// テーブルを複製する
        /// </summary>
        /// <param name="srcConnection">複製元DB接続</param>
        /// <param name="destConnection">複製先DB接続</param>
        /// <param name="databaseName">DB名</param>
        /// <param name="tableName">テーブル名</param>
        public void CopyTable(DbConnection srcConnection, DbConnection destConnection,
            string databaseName, string tableName)
        {
            using (SqlConnection srcConn = new SqlConnection(srcConnection.ConnectionString))
            using (SqlConnection destConn = new SqlConnection(destConnection.ConnectionString))
            {
                OpenConnectionAndChangeDatabase(srcConn, databaseName);
                OpenConnectionAndChangeDatabase(destConn, databaseName);

                // (1)コピー先のテーブルデータをDataSetに読み込む
                DataSet destDS = new DataSet();
                destDS.Locale = CultureInfo.InvariantCulture;
                GetTableData(destConn, databaseName + ".." + tableName, destDS);

                // (2)コピー元のテーブルデータをDataSetに読み込む
                DataSet srcDS = new DataSet();
                srcDS.Locale = CultureInfo.InvariantCulture;
                GetTableData(srcConn, databaseName + ".." + tableName, srcDS);

                DataTable destDT = destDS.Tables[databaseName + ".." + tableName];
                DataTable srcDT = srcDS.Tables[databaseName + ".." + tableName];

                Debug.WriteLine(DateTime.Now + " " + databaseName + ".." + tableName + ".Rows.Count(before) : " + destDT.Rows.Count);
                Debug.WriteLine(DateTime.Now + " " + databaseName + ".." + tableName + ".Rows.Count(after) : " + srcDT.Rows.Count);

                // PrimaryKeys取得
                List<string> pks = new List<string>(GetPrimaryKeys(destConn, databaseName, tableName));

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
                        counts[SqlType.Delete] += ExecuteNonQuery(destConn, deleteSql.ToString());
                    }
                }

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
                        counts[SqlType.Update] += ExecuteNonQuery(destConn, updateSql.ToString());
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
                        counts[SqlType.Insert] += ExecuteNonQuery(destConn, insertSql.ToString());
                    }
                }

                Debug.WriteLine(DateTime.Now + " " + databaseName + ".." + tableName + " updated");
            }//close SqlConnections
        }

        static void GetTableData(SqlConnection conn, string tableName, DataSet ds)
        {
            string sql = string.Format(CultureInfo.CurrentCulture, "SELECT * FROM {0}", tableName);

            // データアダプタの作成
            SqlDataAdapter da = new SqlDataAdapter();

            // select用コマンド・オブジェクトの作成
            da.SelectCommand = new SqlCommand(sql, conn);

            // データセットへの読み込み
            da.Fill(ds, tableName);
        }

        static string[] GetPrimaryKeys(SqlConnection conn, string databaseName, string tableName)
        {
            List<string> pkeys = new List<string>();

            SqlCommand selectCmd = new SqlCommand("sp_pkeys", conn);
            selectCmd.CommandType = CommandType.StoredProcedure;
            selectCmd.Parameters.Add(new SqlParameter("@table_name", tableName));

            OpenConnectionAndChangeDatabase(conn, databaseName);
            using (IDataReader dr = selectCmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    pkeys.Add(dr["COLUMN_NAME"] as string);
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
