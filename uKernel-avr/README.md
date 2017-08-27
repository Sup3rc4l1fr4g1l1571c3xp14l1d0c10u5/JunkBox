# uKernel for AVR

## これは何？

 - 教育用リアルタイムOS `uKernel` を AVRマイコン向けに移植＋αしたものです。
 - ATmega328P (Arduino Uno) で動作確認しました。
   
## 開発環境

 - Windows 10 Pro (x64)
 - Atmel Studio 7

## Atmel Studio 7 導入手順

### 用意するもの

 - Windows PC
 - インターネット接続 
   - インストーラのサイズが 800MB 強あるため、ブロードバンド回線接続環境下が望ましい
 - 飲み物、お菓子、暇つぶし道具
   - インストール時間がそこそこ長いため

### 手順

 1. http://www.atmel.com/ja/jp/tools/atmelstudio.aspx にアクセスし、`Get the latest version of AtmelR Studio from Microchip.` をクリック
 2. `Atmel Studio 7.0 (build 1417) offline installer` をクリックしてインストーラをダウンロード
 3. PCを再起動
    - 大抵の場合、Windows Updateやソフトウェア自動更新後の再起動必要フラグに引っかかって、手順8で `Pending system reboot` に×印がつくため先に再起動しておくと良い。
 4. インストーラを起動
 5. `End user license agreement` が表示される。きちんと読んで同意できるなら `I agree to the license terms and conditions` にチェックを入れて `Next` をクリック。
 6. `Select Architecture` では `AVR 8-bit MCU` を選択、残りは選択を解除して `Next` をクリックする。
 7. `Select extensions` では `Atmel Software Framework and Example Projects` の選択を解除して `Next` をクリックする。
 8. `System validation` ですべての項目が緑チェックになっていれば Next がクリックできるのでクリックする。
     - 手順3で再起動をサボると大体ここで `Pending system reboot` に×印がついていて再起動を要求される。
 9. `Important notes` が表示されるので、きちんと読んで理解してから `Install` をクリック
     - 意訳: AVRデバイス向けのヘッダファイルが更新されているから、違う（以前の）バージョンのヘッダを使う場合は、`Tool` メニューの `Device Pack Manager` でお目当てのバージョンのヘッダをインストールした後に `Project` プロパティを開いて `Components` タブから使うヘッダの設定変えてね。
 10. UACダイアログが表示されるので変更を許可する。
 11. お茶を飲みながら待つ。
     - 途中で `VisualStudio 2015 (Shel)` のインストールが行われるが、これは IDE に `VisualStudio Shell` を利用しているため。
     - VisualStudio の言語パックを導入することで一部日本語化も可能
 12. インストールが完了したら、`Close` を押す

## コンパイル方法

`uKernel-avr.cproj` を開いてビルドしてください。

## カーネルの設定の変更

`kernel.h` 内で定義されている以下のパラメータを変更してください。

```
#define TASK_NUM			(6)
#define TASK_STACK_SIZE		(64)
#define KERNEL_STACK_SIZE	(64)
#define MESSAGE_NUM			(16)
#define SEMAPHORE_NUM		(16)
#define USE_SIMULATOR		(1)
```

## その他

### ATmega328P 向けのプロジェクト作成方法
 1. Atmel Studio を起動
 2. `File` メニューから `New` を選び、サブメニューの `Project...` を選択
 3. 左側のペインから `Installed` を開き、中の `C/C++` を選択。
 4. 右側に表示された `Project` の中から `GCC C Executable Project` を選択。
 5. 下部の `Name` に プロジェクト名(ここでは `uKernel-avr` ) を入力。`Location` と `Solution Name` はデフォルトのままでいい。
 6. OK をクリックすると `Device Selection` (デバイス選択画面) が表示されるので、右側上部の検索ダイアログに `atmega328p` と入力。
 7. 左側リストに表示された候補中から `ATmega328P` を選択して OK をクリック。
 8. プロジェクト と `main.c` が生成されて表示される。

### デバッグ環境として Atmel Studio が提供するシミュレータデバッガを選択
 1. `Project` メニューから `uKernel-avr Properties...` を選択
 2. 左のリストから `Tool` をクリック
 3. 右側の `Selected debugger/programmer` のドロップダウンリストから `Simulator` を選択。
 4. `Debug` メニューから `Start Debugging and Break` を選択。
 5. ビルドが行われた後、画面がデバッグモードに切り替わり、main関数の先頭の括弧に黄色の矢印が表示される。
 6. `Debug` メニューの `Step Over` / `Step Out` を選びステップ実行ができることを確認。
 7. `Debug` メニューの `Stop Debugging` を選択してシミュレータデバッガを終了。

### スペルチェックを無効化

標準で導入されている `Visual Assist`  アドオンのスペルチェック機能が日本語に対応していないのでコメント文に日本語を書き込むと変な赤線が出る。以下の手順で、`Visual Assist` のスペルチェックを無効化する。

 1. `VAssistX` メニューから `Options` を選択
 2. 左側リストから `Underlining` を選択
 3. 右側の一番上にある `Underline spelling errors in comments and strings using` のチェックを外す。

