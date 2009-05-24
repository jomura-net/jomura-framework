using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Jomura.Web.UI.WebControls
{
    /// <summary>
    /// TransactionToken機能を提供する。
    /// Redirect時、Postback時に不正な画面遷移、ボタン２度押しを検出する。
    /// 
    /// 画面に配置すると、初期画面表示時にTokenを生成しFormに登録する。
    /// Redirect先の次画面でTokenチェックを実施する場合には、
    /// RedirectUriにGETパラメータとしてTokenを付与する必要がある。
    /// その際、GETパラメータ名は、次画面のTokenコントロールのIDと同一とすること。
    /// 
    /// チェックする画面では、IsTokenValid()の戻り値判定を実装する必要がある。
    /// </summary>
    [ToolboxData("<{0}:Token runat=\"server\"></{0}:Token>")]
    public class Token : HiddenField
    {
        const string TOKEN_SESSION_NAME = "Jomura.Framework.token";

        /// <summary>
        /// get時、Valueが空ならTokenを生成する。
        /// </summary>
        public override string Value
        {
            get
            {
                if (string.IsNullOrEmpty(base.Value))
                {
                    base.Value = SaveToken(Context);
                }
                return base.Value;
            }
        }

        /// <summary>
        /// Tokenの桁数。
        /// 
        /// 通常は変更の必要はないが、URL長を短縮する場合には、
        /// 当プロパティで数値を減らすことが可能。Defaultは32桁。
        /// ちなみに、Tokenは16進数文字列。
        /// </summary>
        [Bindable(true)]
        [Category("Format")]
        public short Figure
        {
            get
            {
                object obj = ViewState["Figure"];
                return obj == null ? (short)32 : (short)obj;
            }
            set
            {
                ViewState["Figure"] = value;
            }
        }

        /// <summary>
        /// Tokenチェック結果を返す。
        /// 画面のPage_Load()の最初で利用する。
        /// Redirect時、Postback時の両方に利用可能。
        /// 
        /// また、GET(or Form)パラメータ名は、このTokenコントロールのIDと同一であること。
        /// 
        /// 本メソッドを実行するとTokenが更新される。
        /// </summary>
        /// <param name="context">Httpコンテキスト</param>
        /// <returns>Tokenが正しければtrue, 不正ならfalse</returns>
        public bool IsTokenValid(HttpContext context)
        {
            string token = context.Request.Form[ID] ?? context.Request.QueryString[ID];
            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("no token found for token name " + ID
                    + " -> Invalid token ");
                return false;
            }

            string sessionToken = context.Session[TOKEN_SESSION_NAME] as string;
            if (token != sessionToken)
            {
                Trace.TraceWarning("Form token {0} does not match the session token {1}.",
                    new object[] { token, sessionToken });
                return false;
            }

            // remove the token so it won't be used again
            //context.Session.Remove(TOKEN_SESSION_NAME);
            CreateToken(context);
            return true;
        }

        /// <summary>
        /// Tokenを新規生成し、FormとSessionに登録する。
        /// ただし、非ポストバック時のみ。
        /// 
        /// Page_Load()でHyperLink.NavigateUrl等にTokenを付加する場合は、
        /// 先にこのメソッドを実行し、Tokenを生成しておくこと。
        /// </summary>
        /// <param name="context">Httpコンテキスト</param>
        /// <returns>Token</returns>
        string SaveToken(HttpContext context)
        {
            //ポストバック時には、トークンを新規生成しない。
            return Page.IsPostBack 
                ? context.Session[TOKEN_SESSION_NAME] as string
                : CreateToken(context);
        }

        string CreateToken(HttpContext context)
        {
            //Token生成
            string token = Guid.NewGuid().ToString("N");
            if (32 > Figure && Figure > 0)
            {
                token = token.Substring(32 - Figure);
            }

            //Sessionに登録
            context.Session[TOKEN_SESSION_NAME] = token;
            //Request.Formに登録
            base.Value = token;
            
            return token;
        }

    }//eof class
}//eof namespace
