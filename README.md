# Azure Media Services のサンプル
Azure Media Services を始めるためのサンプルコードです

- 例外処理を記述していません
- パラメータとして、app.config か web.config に、自分のAzure Media Services の[アカウント名]と[アカウントキー]を設定します
- 参考ドキュメント:
https://azure.microsoft.com/ja-jp/documentation/services/media-services/

## VoD
Video on Demand用です

### 00. CleanUpWAMS (C#)
- コンソール
- Media Services .NET SDK Extensions

当該Azure Media Services アカウントの全てのオブジェクトを削除します。技術評価時のクリーンアップ用です
やり直しができませんので、注意をして使ってください

### 01. StandardDynamicPackaging (C#)
- コンソール
- Media Services .NET SDK Extensions


VoD用の配信の基本動作です。この中では以下を行います
- 指定ファイルのアップロード。ストレージ内での暗号化なし
- H.264 アダプティブ ビットレートへのエンコード。サイズは最小で
- 配信設定
- 再生用URLをデスクトップ上のテキストファイルに出力

### 02. SecureDynamicPackaging (C#)
- コンソール
- Media Services .NET SDK Extensions

VoD用の配信で、セキュリティオプションを付けたものです。この中では以下を行います
- 指定ファイルのアップロード。ストレージ内でファイルは暗号化オプション
- H.264 アダプティブ ビットレートへのエンコード。サイズは最小で。ストレージ内でファイルは暗号化
- 暗号化されたファイルを配信するための、動的暗号化設定。ここでは、認証処理はしていません
- 配信設定
- 再生用URLをデスクトップ上のテキストファイルに出力

### 03. JavaVoD (Java)
- コンソール
- Java SDK
JavaのVoDの一番シンプルな制御になります
比較的大き目のファイルをアップロードできるようにしています

Azure SDK for Java - Media Services:
https://github.com/Azure/azure-sdk-for-java/tree/master/services/azure-media

## Live
Liveストリーミング用です

### 10. LiveAdmin (C#)
- Web (ASP.NET)
- Media Services .NET SDK Extensions

Live配信中のプレビューと本番の絵と音の確認ができます
- ボタンの動作は少しバギーなのでご注意ください...

### 11. LiveViewing (HTML)
- Azure Mobile Services
- Application Insight

ライブ視聴の際には、ユーザー投稿と、視聴データの2つが最低限必要となります。ここでは、それらを既存のAzureのモジュールで実現するサンプルとなります

Application Insight:
https://azure.microsoft.com/ja-jp/documentation/articles/app-insights-javascript/

### 12. LiveConsole (C#)
- コンソール
- Media Services .NET SDK Extensions

Live配信用のチャネルの作成と、配信実行を行います

参考: http://azure.microsoft.com/ja-jp/documentation/services/media-services/

## Media Intelligence
動画解析などを行えます。
Media Processorを差し替える事で利用ができます。VoDの延長です。

### 21. AzureMediaIndexer (C#)
- コンソール
- Media Services .NET SDK Extensions

Azure Media Indexerを使って、TTMLのファイルを作成します。そのままPlayerに入れると「字幕」付き動画になります。TTMLは時間軸がついてますので、シーン検索にも使えます。

結果のサンプル画面:
http://dahatakettml.azurewebsites.net/


Azure Media Indexer:
http://blogs.msdn.com/b/windowsazurej/archive/2014/09/30/blog-introducing-azure-media-indexer.aspx

このサンプルでは、Microsoft Translatorを使って、英語から日本語を含む、数か国語に機械翻訳をかけています。

Microsoft Translator:
https://msdn.microsoft.com/en-us/library/dd576287.aspx

こちらに、全体の説明があります。
https://daiyuhatakeyama.wordpress.com/2014/09/26/azure-media-indexer-%e3%81%a8-microsoft-translator-%e3%82%92%e4%bd%bf%e3%81%a3%e3%81%a6%e3%80%81%e8%8b%b1%e8%aa%9e%e3%81%ae%e5%8b%95%e7%94%bb%e3%81%ab%e3%80%81%e6%97%a5%e6%9c%ac%e8%aa%9e%e5%ad%97/

## Player
主にAzure Media Playerのサンプルになります

### 31. AzureMediaPlayer_Simple
- html

最新の Azure Media Player ライブラリーを呼び出す、一番単純なサンプルです

参考: http://amp.azure.net/libs/amp/latest/docs/

### 32. SwitchVideoPlayerURL (C#)
- Web (ASP.NET SignalR)
- Azure Media Player

再生中のPlayerに、ASP.NET SignalRを使って、再生URLを一斉送信します

以下、動作サンプルです。
ユーザー画面 (index.html) 
http://dahatakesignalrplayer.azurewebsites.net/
 
管理側の画面 (admin.html: 
http://dahatakesignalrplayer.azurewebsites.net/admin.html

ASP.NET SignalR:
http://www.asp.net/signalr


##33. Simple UWP Player
- Universal Windows Platform

Windows 10つまり、UWPでは、H.264ベースのMPEG-DASH/HLSが、OSレベルのMedia Foundationにてサポートされています。
これまでは、Smooth Streamingの追加のプラグインが必要でしたら、UWPではそれが不要です。
このサンプルではMPEG-DASHのストリームを再生しています。肝は1行です。ご覧あれ。



