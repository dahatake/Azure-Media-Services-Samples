# Azure Media Services のサンプル
=============================
Azure Media Services を始めるためのサンプルコードです。

- 例外処理を記述していません
- パラメータとして、app.config か web.config に、自分のAzure Media Services の[アカウント名]と[アカウントキー]を設定します。

### 0. CleanUpWAMS (C#)
- コンソール
- Media Services .NET SDK Extensions

当該Azure Media Services アカウントの全てのオブジェクトを削除します。技術評価時のクリーンアップ用です。
注意をして使ってください。

### 1. AzureMediaPlayer_Simple
- html

最新の Azure Media Player ライブラリーを呼び出した、一番単純なサンプルです。

参考: http://amp.azure.net/libs/amp/latest/docs/


### 2. StandardDynamicPackaging (C#)
- コンソール
- Media Services .NET SDK Extensions


VoD用の配信の基本動作です。この中では以下を行います。
- 指定ファイルのアップロード。ストレージ内での暗号化なし。
- H.264 アダプティブ ビットレートへのエンコード。サイズは最小で。
- 配信設定
- 再生用URLをデスクトップ上のテキストファイルに出力


### 3. SecureDynamicPackaging (C#)
- コンソール
- Media Services .NET SDK Extensions


VoD用の配信で、セキュリティオプションを付けたものです。この中では以下を行います。
- 指定ファイルのアップロード。ストレージ内でファイルは暗号化オプション。
- H.264 アダプティブ ビットレートへのエンコード。サイズは最小で。ストレージ内でファイルは暗号化。
- 暗号化されたファイルを配信するための、動的暗号化設定。ここでは、認証処理はしていません。
- 配信設定
- 再生用URLをデスクトップ上のテキストファイルに出力

### 4. AzureMediaIndexer (C#)
- コンソール
- Media Services .NET SDK Extensions

Azure Media Indexerを使って、TTMLのファイルを作成します。そのままPlayerに入れると「字幕」付き動画になります。TTMLは時間軸がついてますので、シーン検索にも使えます。

Azure Media Indexer:
http://blogs.msdn.com/b/windowsazurej/archive/2014/09/30/blog-introducing-azure-media-indexer.aspx

このサンプルでは、Microsoft Translatorを使って、英語から日本語を含む、数か国語に機械翻訳をかけています。

Microsoft Translator:
https://msdn.microsoft.com/en-us/library/dd576287.aspx

こちらに、全体の説明があります。
https://daiyuhatakeyama.wordpress.com/2014/09/26/azure-media-indexer-%e3%81%a8-microsoft-translator-%e3%82%92%e4%bd%bf%e3%81%a3%e3%81%a6%e3%80%81%e8%8b%b1%e8%aa%9e%e3%81%ae%e5%8b%95%e7%94%bb%e3%81%ab%e3%80%81%e6%97%a5%e6%9c%ac%e8%aa%9e%e5%ad%97/


## 10. LiveAdmin (C#)
- Web (ASP.NET)
- Media Services .NET SDK Extensions

Live配信中のプレビューと本番の絵と音の確認ができます。
- ボタンの動作は少しバギーなのでご注意ください...

参考: http://azure.microsoft.com/ja-jp/documentation/services/media-services/
