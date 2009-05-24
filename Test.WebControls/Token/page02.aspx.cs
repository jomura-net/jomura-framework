using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Diagnostics;

public partial class page02 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Token1.IsTokenValid(Context))
        {
            Response.StatusCode = 403;
            Response.StatusDescription = "TransactionError";
            Response.End();
            return;
        }

        //時間のかかる処理
        System.Threading.Thread.Sleep(5000);

        Debug.WriteLine("処理終了");

        //Token1.SaveToken(Context);
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        Debug.WriteLine("postbacked");
    }
}
