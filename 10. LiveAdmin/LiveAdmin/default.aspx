<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="LiveAdmin._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Azure Media Services - ライブ配信モニタリング ツール</title>
	<link href="//amp.azure.net/libs/amp/latest/skins/amp-default/azuremediaplayer.min.css" rel="stylesheet" />
	<script src= "//amp.azure.net/libs/amp/latest/azuremediaplayer.min.js"></script>

</head>
<body style="font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif">
    <form id="form1" runat="server">
    <div>
		<label id="status" />
		<asp:Label ID="title" runat="server" Text="Azure Media Services - ライブモニタリングツール" style="font-size: xx-large"></asp:Label>
		<asp:Label ID="lblMessage" runat="server"></asp:Label>
		<br />
		<asp:Table ID="tableListChannel" runat="server" 
			BorderWidth="1px" BorderStyle="Solid" GridLines="Both"
			width="100%">
		</asp:Table>

    </div>
    </form>
</body>
</html>
