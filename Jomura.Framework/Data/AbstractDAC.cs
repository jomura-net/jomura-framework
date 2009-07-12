using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace Jomura.Data
{
    /// <summary>
    /// データアクセス用の抽象クラス。<br />
    /// SQL実行のための実装を簡略化する。<br />
    /// SqlConnection, SqlDataAdapter, SqlCommandの管理を
    /// DACから隠蔽する。<br />
    /// System.Data.SqlClient(SQL Server)非依存。
    /// ConnectionStringSettingsでProviderを設定すること。
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
    /// TODO: 1つのDbConnectionオブジェクトを複数のDACで使い回す。
    /// </remarks>
    public abstract class AbstractDAC : IDisposable
    {
        #region Constructor

        /// <summary>
        /// デフォルト・コンストラクタ
        /// 
        /// DB接続オブジェクトをインスタンス化する。
        /// アプリケーション構成ファイルに、最初に記述してある設定を利用する。
        /// </summary>
        protected AbstractDAC()
        {
            InitConnection(ConfigurationManager.ConnectionStrings[1]);
        }

        /// <summary>
        /// コンストラクタ
        /// 
        /// DB接続オブジェクトをインスタンス化する。
        /// アプリケーション構成ファイルの記述と、引数のDB接続キー名が一致する
        /// DB接続オブジェクトをインスタンス化する。
        /// </summary>
        /// <param name="connectionStringName">DB接続文字列のキー名</param>
        protected AbstractDAC(string connectionStringName)
        {
            InitConnection(ConfigurationManager.ConnectionStrings[connectionStringName]);
        }

        // 以下のコンストラクタは、DbProviderFactoryをインスタンス化できないので却下。
        /* 
        /// <summary>
        /// コンストラクタ
        /// 
        /// DB接続オブジェクトを指定する。
        /// 同一TransactionScope内で
        /// 複数のDACクラスを利用する際に、
        /// DB接続オブジェクトを共有するために利用する。
        /// </summary>
        /// <param name="connection">DB接続オブジェクト</param>
        protected AbstractDAC(DbConnection connection)
        {
            m_Connection = connection;
        }
        */

        #endregion

        #region DbProviderFactory related

        DbProviderFactory factory;

        /// <summary>
        /// DBパラメータインスタンスを生成する。
        /// </summary>
        /// <param name="parameterName">パラメータ名</param>
        /// <param name="value">パラメータ値</param>
        /// <returns>DBパラメータインスタンス</returns>
        protected DbParameter CreateParameter(string parameterName, object value)
        {
            DbParameter parameter = factory.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;
            return parameter;
        }

        #endregion

        #region IDbConnection related

        /// <summary>
        /// DBコネクションを取得する。
        /// 取得時、インスタンス化されていなければ
        /// インスタンス化して返す。
        /// </summary>
        protected DbConnection Connection
        {
            get
            {
                if (m_Connection == null)
                {
                    InitConnection(ConfigurationManager.ConnectionStrings[1]);
                }
                return m_Connection;
            }
        }
        DbConnection m_Connection;

        void InitConnection(ConnectionStringSettings settings)
        {
            factory = DbProviderFactories.GetFactory(settings.ProviderName);
            DbConnection connection = factory.CreateConnection();
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
        DbDataAdapter Adapter
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
        DbDataAdapter m_Adapter;

        /// <summary>
        /// データアダプタをインスタンス化する。
        /// </summary>
        void InitAdapter()
        {
            m_Adapter = factory.CreateDataAdapter();
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
        /// <param name="parameters">SQLパラメータ配列</param>
        /// <param name="commandType">命令型。</param>
        /// <returns>SQLコマンドインスタンス</returns>
        DbCommand InitCommand(string commandText,
            DbParameter[] parameters,
            CommandType? commandType)
        {
            DbCommand command = Connection.CreateCommand();

            if (commandType.HasValue)
            {
                command.CommandType = commandType.Value;
            }

            if (CommandTimeout != -1)
            {
                command.CommandTimeout = CommandTimeout;
            }

            command.CommandText = commandText;

            if (parameters != null)
            {
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
        /// <param name="parameters">SQLパラメータ配列</param>
        /// <param name="commandType">命令型</param>
        /// <returns>
        /// 正常に追加または更新された行数。
        /// この行数には、行を返さないステートメントの
        /// 影響を受ける行は含まれません。 
        /// </returns>
        protected int Fill(DataTable dataTable, string sql,
            DbParameter[] parameters, CommandType? commandType)
        {
            //最初にデータをクリアする。
            dataTable.Clear();

            Adapter.SelectCommand = InitCommand(sql, parameters, commandType);
            try
            {
                return Adapter.Fill(dataTable);
            }
            catch (DbException de)
            {
                Trace.TraceError(de.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// overload
        /// </summary>
        protected int Fill(DataTable dataTable, string sql,
            DbParameter[] parameters)
        {
            return Fill(dataTable, sql, parameters, null);
        }

        /// <summary>
        /// overload
        /// </summary>
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
        /// <param name="parameters">SQLパラメータ配列</param>
        /// <param name="commandType">命令型</param>
        /// <returns>DataReaderのインスタンス</returns>
        /// <remarks>
        /// CommandBehavior列挙体に、
        /// CloseConnectionが指定されているので、
        /// SqlDataReaderオブジェクト消滅時に、
        /// Connectionオブジェクトも破棄されます。
        /// </remarks>
        protected DbDataReader ExecuteReader(string sql,
            DbParameter[] parameters, CommandType? commandType)
        {
            DbCommand command = InitCommand(sql, parameters, commandType);
            try
            {
                command.Connection.Open();
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (DbException de)
            {
                Trace.TraceError(de.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// overload
        /// </summary>
        protected DbDataReader ExecuteReader(string sql, DbParameter[] parameters)
        {
            return ExecuteReader(sql, parameters, null);
        }

        /// <summary>
        /// overload
        /// </summary>
        protected DbDataReader ExecuteReader(string sql)
        {
            return ExecuteReader(sql, null, null);
        }

        /// <summary>
        /// クエリを実行し、そのクエリが返す結果セットの
        /// 最初の行にある最初の列を返します。
        /// 残りの列または行は無視されます。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameters">SQLパラメータ配列</param>
        /// <param name="commandType">命令型</param>
        /// <returns>
        /// 結果セットの最初の行の最初の列。
        /// 結果セットが空の場合は、null 参照。
        /// </returns>
        protected object ExecuteScalar(string sql,
            DbParameter[] parameters, CommandType? commandType)
        {
            DbCommand command = InitCommand(sql, parameters, commandType);
            try
            {
                command.Connection.Open();
                return command.ExecuteScalar();
            }
            catch (DbException de)
            {
                Trace.TraceError(de.StackTrace);
                throw;
            }
            finally
            {
                command.Connection.Close();
            }
        }

        /// <summary>
        /// overload
        /// </summary>
        protected object ExecuteScalar(string sql, DbParameter[] parameters)
        {
            return ExecuteScalar(sql, parameters, null);
        }

        /// <summary>
        /// overload
        /// </summary>
        protected object ExecuteScalar(string sql)
        {
            return ExecuteScalar(sql, null, null);
        }

        #endregion

        #region "UPDATE" query

        /// <summary>
        /// 更新系クエリの実行を行う。
        /// </summary>
        /// <param name="sql">SQL文</param>
        /// <param name="parameters">
        /// 値が入っているSQLパラメータ配列
        /// </param>
        /// <param name="commandType">命令型</param>
        /// <returns>正常に更新された行の数。</returns>
        protected int ExecuteNonQuery(string sql,
            DbParameter[] parameters, CommandType? commandType)
        {
            DbCommand command = InitCommand(sql, parameters, commandType);
            try
            {
                command.Connection.Open();
                return command.ExecuteNonQuery();
            }
            catch (DbException de)
            {
                Trace.TraceError(de.StackTrace);
                throw;
            }
            finally
            {
                command.Connection.Close();
            }
        }

        /// <summary>
        /// overload
        /// </summary>
        protected int ExecuteNonQuery(string sql, DbParameter[] parameters)
        {
            return ExecuteNonQuery(sql, parameters, null);
        }

        /// <summary>
        /// overload
        /// </summary>
        protected int ExecuteNonQuery(string sql)
        {
            return ExecuteNonQuery(sql, null, null);
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
                Trace.TraceInformation(command.CommandText + sqlParamsStr);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.StackTrace);
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
