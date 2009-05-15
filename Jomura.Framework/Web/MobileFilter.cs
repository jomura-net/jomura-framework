using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Net;
using System.Configuration;

namespace Jomura.Framework
{
    /// <summary>
    /// ���o�C���[���ȊO�̃��N�G�X�g���֎~����B(403 forbidden)
    /// ���ʂ́AUserAgent�݂̂ōs���B
    /// UserAgent�́AAppSettings["MobileAgents"]�ɁAcomma��؂�ŕ����w�肷��B
    /// �� ���p�\��UserAgent�ɂ́A���o�C���[���݂̂łȂ��A
    /// �����G���W�����܂߂邱�Ƃ�������������ǂ��B
    /// 
    /// IP�A�h���X�Ő�������ꍇ�́A
    /// IIS�́uIP �A�h���X�ƃh���C�����̐����v�@�\��p����Ƃ悢�B
    /// </summary>
    public sealed class MobileFilter : IHttpModule
    {
        #region IHttpModule �����o

        void IHttpModule.Init(HttpApplication context)
        {
            string confMobileAgents = ConfigurationManager.AppSettings
                ["Jomura.Web.MobileFilter.MobileAgents"];
            mobileAgents = confMobileAgents.Split(',');

            context.BeginRequest += new EventHandler(CheckMobile);
        }

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
                //TODO �񃂃o�C���G���[����
                context.Response.StatusCode = 403;
                context.Response.StatusDescription = "Please use a mobile client.";
                context.Response.End();
                return;
            }
        }

        #endregion

        #region ���o�C���@�픻��(UserAgent)

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
