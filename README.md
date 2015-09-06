# Azure Media Services のサンプル
=============================
Azure Media Services を始めるためのサンプルコードです。

- 例外処理を記述していません
- パラメータとして、app.config か web.config に、自分のAzure Media Services の[アカウント名]と[アカウントキー]を設定します。

### CleanUpWAMS (C#)
- コンソール
- Media Services .NET SDK Extensions

当該Azure Media Services アカウントの全てのオブジェクトを削除します。技術評価時のクリーンアップ用です。
注意をして使ってください。

### StandardDynamicPackaging (C#)
- コンソール
- Media Services .NET SDK Extensions


VoD用の配信の基本動作です。この中では以下を行います。
- 指定ファイルのアップロード。ストレージ内での暗号化なし。
- H.264 アダプティブ ビットレートへのエンコード。サイズは最小で。
- 配信設定
- 再生用URLをデスクトップ上のテキストファイルに出力


### SecureDynamicPackaging (C#)
- コンソール
- Media Services .NET SDK Extensions


VoD用の配信で、セキュリティオプションを付けたものです。この中では以下を行います。
- 指定ファイルのアップロード。ストレージ内でファイルは暗号化オプション。
- H.264 アダプティブ ビットレートへのエンコード。サイズは最小で。ストレージ内でファイルは暗号化。
- 暗号化されたファイルを配信するための、動的暗号化設定。ここでは、認証処理はしていません。
- 配信設定
- 再生用URLをデスクトップ上のテキストファイルに出力

## LiveAdmin (C#)
- Web (ASP.NET)
- Media Services .NET SDK Extensions

Live配信中のプレビューと本番の絵と音の確認ができます。
- ボタンの動作は少しバギーなのでご注意ください...

ご参考: http://azure.microsoft.com/ja-jp/documentation/services/media-services/
