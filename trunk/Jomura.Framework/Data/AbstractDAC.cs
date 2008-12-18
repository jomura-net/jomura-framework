using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

namespace Jomura.Data
{
    /// <summary>
    /// データアクセス用の抽象クラス。<br />
    /// SQL実行のための実装を簡略化する。<br />
    /// SqlConnection, SqlDataAdapter, SqlCommandの管理を
    /// DACから隠蔽する。<br />
    /// System.Data.SqlClient(SQL Server)依存。OracleやMySQLでは利用不可。
    /// </summary>
    /// <remarks>
    /// 全てのDACは、このクラスを継承して実装する。<br />
    /// 参照系DACサンプル：<br />
    /// <code>
    /// public SampleDataSet.DataTable1DataTable GetData()
    /// {
    ///     // SQLの生成
    ///
    ///     SampleDataSet.DataTable1DataTable dataTable =
    ///         new SampleDataSet.DataTable1DataTable();
    ///     this.Fill(dataTable, sql, parmList);
    ///     return dataTable;
    /// }
    /// </code>
    ///
    /// 更新系DACサンプル：<br />
    /// <code>
    /// public int Update(SampleDTO dto)
    /// {
    ///     // SQLの生成
    /// 
    ///     return this.ExecuteNonQuery(sql, parmList);
    /// }
    /// </code>
    /// 
    /// TODO: 1つのTransactionで1つのSqlConnectinオブジェクトに制限する
    /// TODO: SqlParameterをDACから隠蔽化する。
    /// </remarks>
    public abstract class AbstractDAC : IDisposable
    {
        #region Constructor

        /// <summary>
        /// デフォルト・コンストラクタ
        /// </summary>
        protected AbstractDAC()
        {
            InitConnection();
        }

        /// <summary>
        /// コンストラクタ
        /// 
        /// DB接続文字列を指定する。
        /// </summary>
        /// <param name="connectionStringName">DB接続文字列のキー名</param>
        protected AbstractDAC(string connectionStringName)
        {
            InitConnection(connectionStringName);
        }

        /// <summary>
        /// コンストラクタ
        /// 
        /// DB接続オブジェクトを指定する。
        /// 同一TransactionScope内で
        /// 複数のDACクラスを利用する際に、
        /// DB接続オブジェクトを共有するために利用する。
        /// </summary>
        /// <param name="connection">DB接続オブジェクト</param>
        protected AbstractDAC(SqlConnection connection)
        {
            m_Connection = connection;
        }

        #endregion

        #region IDbConnection related

        /// <summary>
        /// DBコネクションを取得する。
        /// 取得時、インスタンス化されていなければ
        /// インスタンス化して返す。
        /// </summary>
        protected SqlConnection Connection
        {
            get
            {
                if (m_Connection == null)
                {
                    InitConnection();
                }
                return m_Connection;
            }
        }
        SqlConnection m_Connection;

        /// <summary>
        /// DB接続オブジェクトをインスタンス化する。
        /// 
        /// アプリケーション構成ファイルに、最初に記述してある設定を利用する。
        /// </summary>
        /// <returns>DB接続オブジェクト</returns>
        void InitConnection()
        {
            SqlConnection connection = new SqlConnection();
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[1];
            connection.ConnectionString = settings.ConnectionString;
            m_Connection = connection;
        }

        /// <summary>
        /// 引数で指定したDB接続キー名で、DB接続オブジェクトをインスタンス化する。
        /// </summary>
        /// <param name="connectionStringName">DB接続キー名</param>
        /// <returns>DB接続オブジェクト</returns>
        void InitConnection(string connectionStringName)
        {
            SqlConnection connection = new SqlConnection();
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionStringName];
            connection.ConnectionString = settings.ConnectionString;
            m_Connection = connection;
        }

        #endregion

        #region IDbDataAdapter related

        /// <summary>
        /// データアダプタを取得する。
        /// 取得時、インスタンス化されていなければ
        /// インスタンス化して返す。
        /// </summary>
        SqlDataAdapter Adapter
        {
            get
            {
                if (m_Adapter == null)
                {
                    InitAdapter();
                }
                return m_Adapter;
            }
        }
        SqlDataAdapter m_Adapter;

        /// <summary>
        /// データアダプタをインスタンス化する。
        /// </summary>
        void InitAdapter()
        {
            m_Adapter = new SqlDataAdapter();
        }

        #endregion

        #region IDbCommand related

        /// <summary>
        /// 引数で渡されたSQL文とSQLパラメータから、
        /// SQLコマンドインスタンスを生成する。
        /// コネクションの設定や、
        /// SQL・パラメータのログ出力を行う。
        /// </summary>
        /// <param name="commandText">SQL文</param>
        /// <param name="parameterList">SQLパラメータ</param>
        /// <returns>SQLコマンドインスタンス</returns>
        SqlCommand InitCommand(string commandText,
            Collection<SqlParameter> parameterList)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = Connection;
            command.CommandType = CommandType.Text;
            command.CommandText = commandText;

            if (CommandTimeout != -1)
            {
                command.CommandTimeout = CommandTimeout;
            }

            if (parameterList != null)
            {
                SqlParameter[] parameters = new SqlParameter[parameterList.Count];
                parameterList.CopyTo(parameters, 0);

                command.Parameters.Clear();
                command.Parameters.AddRange(parameters);
            }

            // SQL ログ出力
            LogSql(command);

            return command;
        }

        /// <summary>
        /// タイムアウト時間を設定する。
        /// デフォルト値は、
        /// パラメータ"CommandTimeout"で指定する。
        /// </summary>
        protected int CommandTimeout
        {
            get { return m_CommandTimeout; }
            set { m_CommandTimeout = value; }
        }
        int m_CommandTimeout = -1;

        #endregion

        #region "SELECT" query

        /// <summary>
        /// 参照系クエリの実行を行う。
        /// </summary>
        /// <param name="dataTable">参照結果テーブル</param>
        /// <param name="sql">SQL文</param>
        /// <param name="parameterList">SQLパラメータのリスト</param>
        /// <returns>
        /// 正常に追加または更新された行数。
        /// この行数には、行を返さないステートメントの
        /// 影響を受ける行は含まれません。 
        /// </returns>
        protected int Fill(DataTable dataTable, string sql,
            Collection<SqlParameter> parameterList)
        {
            //最初にデータをクリアする。
            dataTable.Clear();

            Adapter.SelectCommand = InitCommand(sql, parameterList);
            try
            {
                return Adapter.Fill(dataTable);
            }
            catch (SqlException se)
            {
                //TODO ログ出力
                Debug.WriteLine(se);
                throw;
            }
        }

        /// <summary>
        /// 参照系クエリの実行を行う。
        /// </summary>
        /// <param name="dataTable">参照結果テーブル</param>
        /// <param name="sql">SQL文</param>
        /// <returns>
        /// 正常に追加または更新された行数。
        /// この行数には、行を返さないステートメントの
        /// 影響を受ける行は含まれません。 
        /// </returns>
        protected int Fill(DataTable dataTable, string sql)
        {
            return this.Fill(dataTable, sql, null);
        }

        /// <summary>
        /// 参照系クエリの実行を行うDataReaderを返す。
        /// 処理終了時には、自動的にコネクションは
        /// Closeする。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameterList">SQLパラメータのリスト</param>
        /// <returns>DataReaderのインスタンス</returns>
        /// <remarks>
        /// CommandBehavior列挙体に、
        /// CloseConnectionが指定されているので、
        /// SqlDataReaderオブジェクト消滅時に、
        /// Connectionオブジェクトも破棄されます。
        /// </remarks>
        protected SqlDataReader ExecuteReader(string sql,
            Collection<SqlParameter> parameterList)
        {
            SqlCommand command = InitCommand(sql, parameterList);
            try
            {
                command.Connection.Open();
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (SqlException se)
            {
                //TODO ログ出力
                Debug.WriteLine(se);
                throw;
            }
        }

        /// <summary>
        /// 参照系クエリの実行を行うDataReaderを返す。
        /// 処理終了時には、自動的にコネクションは
        /// Closeする。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <returns>DataReaderのインスタンス</returns>
        /// <remarks>
        /// CommandBehavior列挙体に、
        /// CloseConnectionが指定されているので、
        /// SqlDataReaderオブジェクト消滅時に、
        /// Connectionオブジェクトも破棄されます。
        /// </remarks>
        protected SqlDataReader ExecuteReader(string sql)
        {
            return ExecuteReader(sql, null);
        }

        /// <summary>
        /// クエリを実行し、そのクエリが返す結果セットの
        /// 最初の行にある最初の列を返します。
        /// 残りの列または行は無視されます。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameterList">SQLパラメータのリスト</param>
        /// <returns>
        /// 結果セットの最初の行の最初の列。
        /// 結果セットが空の場合は、null 参照。
        /// </returns>
        protected object ExecuteScalar(string sql,
            Collection<SqlParameter> parameterList)
        {
            SqlCommand command = InitCommand(sql, parameterList);
            try
            {
                command.Connection.Open();
                return command.ExecuteScalar();
            }
            catch (SqlException se)
            {
                //TODO ログ出力
                Debug.WriteLine(se);
                throw;
            }
            finally
            {
                command.Connection.Close();
            }
        }

        /// <summary>
        /// クエリを実行し、そのクエリが返す結果セットの
        /// 最初の行にある最初の列を返します。
        /// 残りの列または行は無視されます。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <returns>
        /// 結果セットの最初の行の最初の列。
        /// 結果セットが空の場合は、null 参照。
        /// </returns>
        protected object ExecuteScalar(string sql)
        {
            return ExecuteScalar(sql, null);
        }

        #endregion

        #region "UPDATE" query

        /// <summary>
        /// クエリの実行を行う。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameterList">
        /// 値が入っているSQLパラメータのリスト
        /// </param>
        /// <returns>正常に更新された行の数。</returns>
        protected int ExecuteNonQuery(string sql,
            Collection<SqlParameter> parameterList)
        {
            SqlCommand command = InitCommand(sql, parameterList);
            try
            {
                command.Connection.Open();
                return command.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                //TODO ログ出力
                Debug.WriteLine(se);
                throw;
            }
            finally
            {
                command.Connection.Close();
            }
        }

        #endregion

        #region utility

        /// <summary>
        /// SQL文とクエリ値をログ出力する。
        /// </summary>
        /// <param name="command">SQLコマンド</param>
        static void LogSql(IDbCommand command)
        {
            try
            {
                // SQLパラメータ
                StringBuilder sqlParamsStr = new StringBuilder();
                for (int i = 0, max = command.Parameters.Count; i < max; i++)
                {
                    IDataParameter sqlParameter = (IDataParameter)command.Parameters[i];
                    object type = "null";
                    object value = "null";
                    if (sqlParameter.Value != null && sqlParameter.Value != DBNull.Value)
                    {
                        type = sqlParameter.Value.GetType();
                        value = sqlParameter.Value;
                    }
                    sqlParamsStr.AppendFormat(System.Globalization.CultureInfo.CurrentCulture,
                        " {{{0}, {1}, {2}}}", new object[] { sqlParameter.ParameterName, type, value });
                }

                //TODO SQLログ出力
                Debug.WriteLine(command.CommandText + sqlParamsStr);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Console.WriteLine(e);

                //Do Nothing
            }
        }

        #endregion


        #region IDisposable メンバ

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        /// <param name="disposing"></param>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_Adapter != null)
                {
                    m_Adapter.Dispose();
                    m_Adapter = null;
                }
                if (m_Connection != null)
                {
                    m_Connection.Dispose();
                    m_Connection = null;
                }
            }
        }

        #endregion

    }//end class
}//end namespace
