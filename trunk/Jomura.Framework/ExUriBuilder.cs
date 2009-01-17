using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace Jomura
{
    /// <summary>
    /// Uriの作成・加工をサポートするユーティリティクラス
    /// </summary>
    public class ExUriBuilder : System.UriBuilder
    {
        #region Constructors

        /// <summary>
        /// 引数なしコンストラクタ
        /// </summary>
        public ExUriBuilder()
            : base()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="uri">URI文字列</param>
        public ExUriBuilder(string uri)
            : base(uri)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="uri">URI</param>
        public ExUriBuilder(Uri uri)
            : base(uri)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="schemeName">スキーマ名</param>
        /// <param name="hostName">ホスト名</param>
        public ExUriBuilder(string schemeName, string hostName)
            : base(schemeName, hostName)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="scheme">スキーマ名</param>
        /// <param name="host">ホスト名</param>
        /// <param name="portNumber">ポート番号</param>
        public ExUriBuilder(string scheme, string host, int portNumber)
            : base(scheme, host, portNumber)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="scheme">スキーマ名</param>
        /// <param name="host">ホスト名</param>
        /// <param name="port">ポート番号</param>
        /// <param name="pathValue">パス</param>
        public ExUriBuilder(string scheme, string host, int port, string pathValue)
            : base(scheme, host, port, pathValue)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="scheme">スキーマ名</param>
        /// <param name="host">ホスト名</param>
        /// <param name="port">ポート番号</param>
        /// <param name="path">パス</param>
        /// <param name="extraValue">追加文字列</param>
        public ExUriBuilder(string scheme, string host, int port, string path, string extraValue)
            : base(scheme, host, port, path, extraValue)
        {
        }

        #endregion

        #region AddQuery methods

        /// <summary>
        /// URLにクエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="queryStringName">クエリ文字列名</param>
        /// <param name="queryStringValue">クエリ文字列値</param>
        public void AddQueryString(string queryStringName, string queryStringValue)
        {
            Query = AddQueryStringToUri(Query, queryStringName, queryStringValue);
        }

        /// <summary>
        /// URLにクエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="queryStrings">クエリ文字列のコレクション</param>
        public void AddQueryStrings(NameValueCollection queryStrings)
        {
            Query = AddQueryStringsToUri(Query, queryStrings);
        }

        #endregion

        #region static methods

        /// <summary>
        /// URLにクエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="uriString">URL文字列</param>
        /// <param name="queryStringName">クエリ文字列名</param>
        /// <param name="queryStringValue">クエリ文字列値</param>
        /// <returns>クエリ文字列が追加されたURL文字列</returns>
        public static string AddQueryStringToUri(string uriString, string queryStringName, string queryStringValue)
        {
            //それぞれの引数がnullの場合への対応
            uriString = uriString ?? string.Empty;
            if (string.IsNullOrEmpty(queryStringName)) return uriString;
            queryStringValue = queryStringValue ?? string.Empty;

            StringBuilder urlb = new StringBuilder(uriString);
            urlb.Append(uriString.IndexOf('?') == -1 ? "?" : "&");
            urlb.Append(queryStringName);
            urlb.Append("=");
            urlb.Append(Uri.EscapeUriString(queryStringValue));
            return urlb.ToString();
        }

        /// <summary>
        /// URLにクエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="uriString">URL文字列</param>
        /// <param name="queryStrings">クエリ文字列のコレクション</param>
        /// <returns>クエリ文字列が追加されたURL文字列</returns>
        public static string AddQueryStringsToUri(string uriString, NameValueCollection queryStrings)
        {
            string returnString = uriString;
            foreach (string queryStringName in queryStrings)
            {
                returnString = AddQueryStringToUri(returnString, queryStringName, queryStrings[queryStringName]);
            }
            return returnString;
        }

        #endregion

    }//eof class
}//eof namespace
