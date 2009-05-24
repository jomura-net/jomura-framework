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

public partial class page01 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        //Debug.WriteLine("page01.Page_Load token : " + Token1.Value); //Can't get token. 

        //Token1.SaveToken(Context);

        //Debug.WriteLine("page01.Page_Load token : " + Token1.Value); //token available.

        //this.HyperLink1.NavigateUrl += "?Token1=" + Token1.Value;
    }
    protected void LinkButton1_Click(object sender, EventArgs e)
    {
        this.Response.Redirect("page02.aspx?Token1=" + Token1.Value);
    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        Debug.WriteLine("form:" + Request.Form["Token1"] + " prop:" + Token1.Value);

        this.Response.Redirect("page02.aspx?Token1=" + Token1.Value);
    }
}
