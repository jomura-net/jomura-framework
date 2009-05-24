using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Jomura.Web.UI.WebControls
{
    /// <summary>
    /// System.Web.UI.WebControls.HyperLink�̊g��
    /// </summary>
    [ToolboxData("<{0}:HyperLinkEx runat=\"server\"></{0}:HyperLinkEx>")]
    public class HyperLinkEx : HyperLink
    {
        /// <summary>
        /// true�Ȃ�URL��Token��t�^����B
        /// defaut�l��false.
        /// </summary>
        public bool Transaction
        {
            get
            {
                object obj = ViewState["Transaction"];
                return obj == null ? false : (bool)obj;
            }
            set
            {
                ViewState["Transaction"] = value;
            }
        }

        /// <summary>
        /// PreRender�C�x���g�n���h���B
        /// 
        /// Transaction==true�Ȃ�APage����Token�R���g���[�����ċA�I�Ɍ������āA
        /// Token������΁ANavigateUri�ɕt������B
        /// </summary>
        /// <param name="e">�C�x���g����</param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            if (Transaction)
            {
                Token token = ControlHelper.FindControl(typeof(Token), Page) as Token;
                if (null != token)
                {
                    NavigateUrl = UriBuilderEx.RemoveQueryString(NavigateUrl, token.ID);
                    NavigateUrl = UriBuilderEx.AddQueryString(NavigateUrl, token.ID, token.Value);
                }
            }
        }
    }
}
