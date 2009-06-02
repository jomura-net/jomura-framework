using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Configuration;

namespace Jomura.Web
{
    /// <summary>
    /// モバイル端末以外のリクエストを禁止する。(403 forbidden)
    /// 判別は、UserAgentのみで行う。
    /// UserAgentは、AppSettings["Jomura.Web.MobileFilter.MobileAgents"]に、
    /// comma区切りで複数指定する。
    /// ※ 利用可能なUserAgentには、モバイル端末のみでなく、
    /// 検索エンジンを含めることも検討する方が良い。
    /// 
    /// IPアドレスで制限する場合は、
    /// IISの「IP アドレスとドメイン名の制限」機能を用いるとよい。
    /// </summary>
    public sealed class MobileFilter : IHttpModule
    {
        #region IHttpModule メンバ

        /// <summary>
        /// 利用可能なUserAgentを読み込み、
        /// Request開始イベントハンドラとして、CheckMobileメソッドを登録する。
        /// </summary>
        /// <param name="context">Httpコンテキスト</param>
        void IHttpModule.Init(HttpApplication context)
        {
            string confMobileAgents = ConfigurationManager.AppSettings
                ["Jomura.Web.MobileFilter.MobileAgents"];
            mobileAgents = confMobileAgents.Split(',');

            context.BeginRequest += new EventHandler(CheckMobile);
        }

        /// <summary>
        /// 何もしない。
        /// </summary>
        void IHttpModule.Dispose()
        {
            //Do Nothing
        }

        #endregion

        #region EventHandlers

        void CheckMobile(object sender, EventArgs e)
        {
            HttpApplication context = sender as HttpApplication;

            if (!IsMobileAgent(context.Request.UserAgent))
            {
                //TODO 非モバイルエラー処理
                context.Response.StatusCode = 403;
                context.Response.StatusDescription = "Please use a mobile client.";
                context.Response.End();
                return;
            }
        }

        #endregion

        #region モバイル機器判定(UserAgent)

        bool IsMobileAgent(string userAgent)
        {
            return !string.IsNullOrEmpty(Array.Find(mobileAgents, delegate(string agent)
            {
                return userAgent.Contains(agent);
            }));
        }

        string[] mobileAgents;

        #endregion
    }
}
