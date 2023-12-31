# JeqDB-Converter

気象庁震度データベース処理ソフト

## **[.NET 8.0 ランタイム x64](https://dotnet.microsoft.com/ja-jp/download/dotnet/8.0)** が必要です。

.NET デスクトップ ランタイム8.0.x x64をインストールすればいいはずです。だめだったらx86などを試してください。

---

## 同梱

### フォント:[Koruri](https://koruri.github.io/)

### 日本地図データ:[気象庁 予報区等GISデータ](https://www.data.jma.go.jp/developer/gis.html)

### 世界地図データ:[Natural Earth](https://www.naturalearthdata.com/downloads/)

を加工しています。

## 概要

コンソールに表示される内容に従ってください。

"入力してください"とあるときは、Enterキーを押して確定してください。"空文字を入力してください"とあるときは何も入力せずにEnterキーを押してください。

### 複数ファイルの結合

1000以上の情報を描画する際に使用してください。(描画ではファイルの読み込みは1つのみです。)順番は気にしなくてもいいです。

### 画像描画

情報をすべて描画します。古い順に描画されます。`output\image\`に保存されます。

### 動画作成

設定されたとおりに描画されます。古い順に描画されます。`output\videoimage\`に保存されます。

終了時コマンドが表示されるので、画像があるフォルダで実行してください。※ffmpeg.exeが環境変数に設定されている必要があります。

### csv取得

震度データベースのAPIにリクエストを送ってcsv形式にして保存します。`output\csv\`に保存されます。一部設定のみです。

---

## 更新履歴

### v1.0.0

2023/12/31

複数ファイルの結合、画像描画、動画作成、csv取得機能追加
