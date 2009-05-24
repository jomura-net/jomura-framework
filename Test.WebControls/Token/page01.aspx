<%@ Page Language="C#" AutoEventWireup="true" CodeFile="page01.aspx.cs" Inherits="page01" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>無題のページ</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:LinkButton ID="LinkButton1" runat="server" OnClick="LinkButton1_Click">page02へ</asp:LinkButton><br />
        <br />
        &nbsp;<asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="page０２へ" /><br />
        <br />
        <jo:HyperLinkEx ID="HyperLink1" runat="server" NavigateUrl="page02.aspx" 
            Transaction="True">page02へ</jo:HyperLinkEx><br />
        <br />
        <jo:Token ID="Token1" runat="server" Figure="4" />
        <br />
        <asp:Button ID="Button2" runat="server" Text="PostBack" />
    </div>
    </form>
</body>
</html>
