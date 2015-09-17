<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="LiveAdmin._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml" lang="ja">
<head runat="server">
    <title>Azure Media Services - ライブ配信監視 ツール</title>

	<meta charset="utf-8" />
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
	<meta http-equiv="Pragma" content="no-cache" />
	<meta http-equiv="Cache-Control" content="no-cache" />
	<meta http-equiv="Expires" content="Thu, 01 Dec 1994 16:00:00 GMT" />

	<link href="//amp.azure.net/libs/amp/latest/skins/amp-default/azuremediaplayer.min.css" rel="stylesheet" />
	<script src= "//amp.azure.net/libs/amp/latest/azuremediaplayer.min.js"></script>

</head>
<body style="font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif">
    <form id="form1" runat="server">
    <div>
		<label id="status" />
		<asp:Label ID="title" runat="server" Text="Azure Media Services - ライブ配信監視 ツール" style="font-size: xx-large"></asp:Label>
		<h2>各ボタンを押した後は、しばらくそのままでお待ちください。<br />最新の状態にするには、手動でリフレッシュしてください </h2>
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
