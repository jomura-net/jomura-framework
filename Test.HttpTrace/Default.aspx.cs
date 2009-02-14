using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Diagnostics;
using System.Threading;
using Jomura;

public partial class _Default : System.Web.UI.Page 
{
    Random rand = new Random();

    protected void Page_Load(object sender, EventArgs e)
    {
        Thread.Sleep(rand.Next(5000));

        //Test.ExUriBuilder
        UriBuilderEx urib1 = new UriBuilderEx("http://localhost/app/test.aspx?aaa=bbb&ccc=ddd");
        urib1.AddQueryString("eee", "エフエフエフ");
        Debug.WriteLine("(1) " + urib1);

        UriBuilderEx urib2 = new UriBuilderEx("http://localhost/app/test.aspx?aaa=bbb&ccc=%E3%83%87%E3%82%A3%E3%83%BCD");
        urib2.RemoveQueryString("aaa");
        Debug.WriteLine("(2) " + urib2);

        urib2.RemoveQueryString("eee");
        Debug.WriteLine("(3) " + urib2);


    }
}
