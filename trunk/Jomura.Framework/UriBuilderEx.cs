using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Web;

namespace Jomura
{
    /// <summary>
    /// Uriの作成・加工をサポートするユーティリティクラス
    /// </summary>
    public class UriBuilderEx : System.UriBuilder
    {
        #region Constructors

        /// <summary>
        /// 引数なしコンストラクタ
        /// </summary>
        public UriBuilderEx()
            : base()
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="uri">URI文字列</param>
        public UriBuilderEx(string uri)
            : base(uri)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="uri">URI</param>
        public UriBuilderEx(Uri uri)
            : base(uri)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="schemeName">スキーマ名</param>
        /// <param name="hostName">ホスト名</param>
        public UriBuilderEx(string schemeName, string hostName)
            : base(schemeName, hostName)
        {
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="scheme">スキーマ名</param>
        /// <param name="host">ホスト名</param>
        /// <param name="portNumber">ポート番号</param>
        public UriBuilderEx(string scheme, string host, int portNumber)
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
        public UriBuilderEx(string scheme, string host, int port, string pathValue)
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
        public UriBuilderEx(string scheme, string host, int port, string path, string extraValue)
            : base(scheme, host, port, path, extraValue)
        {
        }

        #endregion

        #region AddQuery methods

        /// <summary>
        /// クエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="queryStringName">クエリ文字列名</param>
        /// <param name="queryStringValue">クエリ文字列値</param>
        public void AddQueryString(string queryStringName, string queryStringValue)
        {
            Query = AddQueryStringToUri(Query, queryStringName, queryStringValue).Substring(1);
        }

        /// <summary>
        /// クエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="queryStrings">クエリ文字列のコレクション</param>
        public void AddQueryStrings(NameValueCollection queryStrings)
        {
            Query = AddQueryStringsToUri(Query, queryStrings).Substring(1);
        }

        #endregion

        /// <summary>
        /// クエリ文字列から特定のクエリ項目を削除する。
        /// </summary>
        /// <param name="queryStringName">クエリ文字列名</param>
        public void RemoveQueryString(string queryStringName)
        {
            if (string.IsNullOrEmpty(Query)) return;

            string[] queryArr = Query.Substring(1).Split('&');
            NameValueCollection queryStrings = new NameValueCollection(queryArr.Length);
            foreach (string queryItem in queryArr)
            {
                string[] nameAndValue = queryItem.Split('=');
                queryStrings.Add(nameAndValue[0], Uri.UnescapeDataString(nameAndValue[1]));
            }

            queryStrings.Remove(queryStringName);

            Query = string.Empty;
            AddQueryStrings(queryStrings);
        }

        #region static methods

        /// <summary>
        /// URLにクエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="uriStr">URL文字列</param>
        /// <param name="queryStringName">クエリ文字列名</param>
        /// <param name="queryStringValue">クエリ文字列値</param>
        /// <returns>クエリ文字列が追加されたURL文字列</returns>
        public static string AddQueryStringToUri(string uriStr, string queryStringName, string queryStringValue)
        {
            //それぞれの引数がnullの場合への対応
            string uriString = uriStr ?? string.Empty;
            if (string.IsNullOrEmpty(queryStringName)) return uriString;
            queryStringValue = queryStringValue ?? string.Empty;

            StringBuilder urlb = new StringBuilder(uriString);
            urlb.Append(uriString.IndexOf('?') == -1 ? "?" : "&");
            urlb.Append(queryStringName);
            urlb.Append("=");
            urlb.Append(Uri.EscapeDataString(queryStringValue));
            return urlb.ToString();
        }

        /// <summary>
        /// URLにクエリ文字列を追加する。
        /// 
        /// クエリ文字列は、Uriエンコードされる。
        /// 既に同じクエリ名が存在している場合でも、追加登録される。
        /// </summary>
        /// <param name="uriStr">URL文字列</param>
        /// <param name="queryStrings">クエリ文字列のコレクション</param>
        /// <returns>クエリ文字列が追加されたURL文字列</returns>
        public static string AddQueryStringsToUri(string uriStr, NameValueCollection queryStrings)
        {
            string returnString = uriStr;
            foreach (string queryStringName in queryStrings)
            {
                returnString = AddQueryStringToUri(returnString, queryStringName, queryStrings[queryStringName]);
            }
            return returnString;
        }

        #endregion

    }//eof class
}//eof namespace
