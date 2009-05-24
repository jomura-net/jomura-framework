<%@ Page Language="C#" AutoEventWireup="true" CodeFile="page02.aspx.cs" Inherits="page02" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>無題のページ</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        問題なし<br />
        <br />
        <jo:Token ID="Token1" runat="server" />
        <br />
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="PostBack" /></div>
    </form>
</body>
</html>
