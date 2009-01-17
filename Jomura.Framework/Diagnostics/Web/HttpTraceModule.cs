using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace Jomura.Diagnostics.Web
{
    /// <summary>
    /// アプリケーション構成ファイルのパラメータ"HttpTracePath"
    /// で指定されたフォルダに、Http要求と返答を記録したファイルを生成する。
    /// パラメータ"TraceContent"に"true"がセットされていれば、Responseもファイル出力する。
    /// <example>
    /// &lt;configuration&gt;
    ///   &lt;appSettings&gt;
    ///     &lt;add key=&quot;HttpTracePath&quot; value=&quot;D:\tracelog&quot; /&gt;
    ///   &lt;/appSettings&gt;
    /// 	&lt;system.web&gt;
    ///     &lt;httpModules&gt;
    ///       &lt;add type=&quot;Jomura.Web.HttpTraceModule,Jomura.Web&quot; name=&quot;HttpTracer&quot; /&gt;
    ///     &lt;/httpModules&gt;
    ///         （…以下略）
    /// </example>
    /// </summary>
    public class HttpTraceModule : IHttpModule
    {
        #region IHttpModule メンバ

        void IHttpModule.Init(HttpApplication app)
        {
            Debug.WriteLine("HttpTraceModule Init.");

            // Responseの内容をファイルに出力するかどうかのフラグ
            string traceContentStr = ConfigurationManager.AppSettings["TraceContent"] ?? "false";
            traceContent = Convert.ToBoolean(traceContentStr);

            // ファイルを出力するパス。未設定なら%TEMP%
            string defaultPath = Path.GetTempPath() + @"\HttpTrace\";
            string path = ConfigurationManager.AppSettings["HttpTracePath"] ?? string.Empty;
            httpTraceBasePath = Path.Combine(defaultPath, path);
            // フォルダが無ければ作る。
            Directory.CreateDirectory(httpTraceBasePath);

            // Request開始イベントに、TraceRequestメソッドを紐付け
            app.BeginRequest += new EventHandler(TraceRequest);

            // Request終了イベントに、TraceResponseメソッドを紐付け
            app.EndRequest += new EventHandler(TraceResponse);
        }

        void IHttpModule.Dispose()
        {
            Debug.WriteLine("HttpTraceModule Disposed.");
        }

        #endregion

        static readonly string DATE_FORMAT = "yyyyMMddTHHmmssfff";
            
        // ファイルを出力するパス。未設定なら%TEMP%
        string httpTraceBasePath;

        // Responseの内容をファイルに出力するかどうかのフラグ
        bool traceContent = false;


        void TraceRequest(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;

            DateTime startTime = DateTime.Now;

            //ファイル名は、「IPアドレス＋現在時刻」
            string ipaddr = app.Request.UserHostAddress;
            string formattedTime = startTime.ToString(DATE_FORMAT);
            string url = FileUtil.GetValidFileName(app.Request.Path);
            string filename = ipaddr + "_" + formattedTime + url + ".txt";
            string path = httpTraceBasePath
                + @"\" + startTime.ToString("yyyyMMdd") + @"\";

            // フォルダが無ければ作る。
            Directory.CreateDirectory(path);
            string filepath = Path.Combine(path, filename);
            // TraceResponseで使うので保存
            app.Context.Items["TraceFilePath"] = filepath;
            app.Context.Items["StartTime"] = startTime;

            // Request内容をファイルに出力する。
            app.Request.SaveAs(filepath, true);

            // RequestとResponseの区切り文字
            File.AppendAllText(filepath, Environment.NewLine + Environment.NewLine
                + "-- " + Environment.NewLine + Environment.NewLine);

            if (traceContent)
            {
                // Response内容をファイル出力する。
                app.Response.Filter = new ResponseWriter(app.Response.Filter, filepath);
            }
        }

        void TraceResponse(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;

            string filepath = app.Context.Items["TraceFilePath"] as string;
            DateTime startTime = (DateTime)app.Context.Items["StartTime"];

            // Request終了時刻をファイルに出力
            DateTime endTime = DateTime.Now;
            File.AppendAllText(filepath, Environment.NewLine + "-----" 
                + Environment.NewLine + endTime.ToString(DATE_FORMAT)
                + Environment.NewLine + (endTime - startTime)
                );

            /* 例外が出力されます
            foreach (string key in app.Response.Headers)
            {
                File.AppendAllText(httpTracePath,
                    key + ": " + app.Response.Headers[key] + Environment.NewLine);
            }
            */
        }

    }//eo class

    class ResponseWriter : Stream
    {
        private Stream m_originalStream;
        private Stream m_filterStream;
        private string m_filepath;

        public ResponseWriter(Stream originalStream, string filepath)
        {
            m_originalStream = originalStream;
            m_filterStream = new FileStream(filepath, FileMode.Append);
            m_filepath = filepath;
        }

        public override bool CanRead
        {
            get { return m_originalStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return m_originalStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return m_originalStream.CanWrite; }
        }

        public override void Flush()
        {
            m_originalStream.Flush();
        }

        public override long Length
        {
            get { return m_originalStream.Length; }
        }

        public override long Position
        {
            get
            {
                return m_originalStream.Position;
            }
            set
            {
                m_originalStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_originalStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_originalStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_originalStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_filterStream.Write(buffer, offset, count);
            m_originalStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (null != m_filterStream)
            {
                m_filterStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }//eo class

    class FileUtil
    {
        public static string GetValidFileName(string rawFileName)
        {
            string validFileName = rawFileName;
            char[] invalidChars = Path.GetInvalidFileNameChars();

            foreach (char c in invalidChars)
            {
                validFileName = validFileName.Replace(c, '-');
            }
            return validFileName;
        }
    }
}//eo namespace
