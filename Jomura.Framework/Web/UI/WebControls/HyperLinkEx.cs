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
    /// System.Web.UI.WebControls.HyperLinkの拡張
    /// </summary>
    [ToolboxData("<{0}:HyperLinkEx runat=\"server\"></{0}:HyperLinkEx>")]
    public class HyperLinkEx : HyperLink
    {
        /// <summary>
        /// trueならURLにTokenを付与する。
        /// defaut値はfalse.
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
        /// PreRenderイベントハンドラ。
        /// 
        /// Transaction==trueなら、Page内のTokenコントロールを再帰的に検索して、
        /// Tokenがあれば、NavigateUriに付加する。
        /// </summary>
        /// <param name="e">イベント引数</param>
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
