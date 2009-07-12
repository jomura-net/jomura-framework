using System;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web;

namespace Jomura.Web.UI.WebControls
{
    /// <summary>
    /// System.Web.UI.WebControls.Labelの拡張
    /// 
    /// (1)Modeプロパティが&quot;Encode&quot;の場合、TextをHtmlEncodeして表示します。
    /// </summary>
    [DataBindingHandler("System.Web.UI.Design.TextDataBindingHandler, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ControlValueProperty("Text")]
    [ControlBuilder(typeof(LabelControlBuilder))]
    [DefaultProperty("Text")]
    [ParseChildren(false)]
    [Designer("System.Web.UI.Design.WebControls.LabelDesigner, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [ToolboxData(@"<{0}:LabelEx runat=""server"" Text=""LabelEx""></{0}:LabelEx>")]
    public class LabelEx : Label
    {
        /// <summary>
        /// LabelEx コントロールの内容を表示する方法を指定する列挙体の値を取得または設定します。
        /// 既定値は PassThrough です。
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(LabelExMode.PassThrough)]
        [Description("テキストをエンコードするかを決定します。")]
        public LabelExMode Mode
        {
            get
            {
                object obj = ViewState["Mode"];
                return obj == null ? LabelExMode.PassThrough : (LabelExMode)obj;
            }
            set
            {
                ViewState["Mode"] = value;
            }
        }

        /// <summary>
        /// Textプロパティの内容を指定したWriterに表示します。
        /// </summary>
        /// <param name="writer">クライアントに HTML のコンテンツを表示する出力ストリーム。</param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (LabelExMode.Encode == Mode)
            {
                writer.Write(HttpUtility.HtmlEncode(Text));
            }
            else
            {
                base.RenderContents(writer);
            }
        }
    }
}
