﻿# NextorPatch For HB-11 version 0.0.1

## 概要

### 本パッチの概要

SONY最後のMSX1であるHB-11(HiTBiT-U)では、スロット3-0にある内蔵メニューの影響で、NEXTORがDOS1モードでしか起動できません。

フラッシュメモリなどにより容易に修正可能な場合が多いNEXTOR側を改造することにより内蔵メニューと共存可能にします。

本パッチを適用したNEXTORをHB-11以外の機種で起動した場合、初期化が数十クロック遅くなります。

HB-101など他のSONYのMSX1後期機種もアドレスを適切に変更すれば、同様の方法で対応可能と推測されますが、所持していないため対応できません。

### 実行環境

.NET6 もしくは .NET Framework 4.5以降でNEXTORのイメージファイルに対してコマンドラインでパッチを実行します。
.NET Framework用バイナリはgithubのリリース([https://github.com/uniabis/nextor_patch_for_hb11/releases](https://github.com/uniabis/nextor_patch_for_hb11/releases))からダウンロード可能です。

### コマンドライン引数

```
-i 入力ファイルパス(必須)
-o 出力ファイルパス(省略可能)
```

### 対応NEXTORバージョン

```
2.1.0 beta1
2.1.0 beta2
2.1.0 rc1
2.1.0
2.1.1 alpha1
2.1.1 alpha2
2.1.1 beta1
2.1.1 beta2
```

### 動作確認デバイス

```
Fractal2000 SD Mapper/Megaram 512KB
似非RAM Maximum
```

Fractal2000 SD Mapper/Megaram 512KBについてはドライバーがNextor 2.1.xに対応していなかったため、修正後、動作確認しました。DEV_CMDとDEV_FORMATを実装しただけです。

似非RAM MaximumについてはNextorPatcherがNextor 2.1.1 beta2に対応していなかったため、xml変更後、動作確認しました。ハッシュ値を追加してturboパッチのアドレスを7500hから7600hに移動しただけです。新機能を試したい場合以外は2.1.0で問題ないものと思います。

## 詳細

### HB-11の内蔵メニューがDOS2以降未対応のカートリッジであると誤判定される問題

HB-11のスロット3-0にある内蔵メニューでは、初期化時にまずH.STKE(0FEDAh)が他のカートリッジによってフック済みでなければフックして戻ります。実際のメニューは全スロットの初期化完了後にH.STKE(0FEDAh)から起動されます。これは一般的なカートリッジの初期化手順と同一です。

DOS2やNEXTORではH.RUNC(0FECBh)内の処理で、H.STKE(0FEDAh)がフックされているか確認し、さらにUSRTAB(0F39AH)にFCERR(475AH)が格納されている場合は、DOS2以降未対応のカートリッジが存在していると判断し、DOS1モードとなります。

DOS2以降対応カートリッジではUSRTAB(0F39AH)に、FCERR(475AH)以外の値、基本的には6474Hをセットする事とされています。

HB-11の内蔵メニューでは、前述のように、初期化時にH.STKE(0FEDAh)をセットしますが、USRTAB(0F39AH)は変更せず、初期値のFCERR(475AH)のままであるため、DOS1モードになってしまします。

本パッチでは、NEXTORのH.RUNCの処理の直前に、H.STKE(0FEDAh)の内容がHB-11の内蔵メニューの初期化時に設定される状態と一致している場合に、H.STKE(0FEDAh)をクリアする処理を追加します。
H.STKE(0FEDAh)の内容が偶然一致する可能性は低いのですが、念のためスロット3-0の5DB2hにHITBIT文字列が格納されていることも確認します。

これによりNEXTOR起動時はHB-11の内蔵メニューが自動的には起動しなくなり、BASICから```CALL HITBIT```を実行した時のみ起動するようになります。

単にHB-11の内蔵メニューのH.STKE(0FEDAh)を無効化した場合、```CALL FONT```やワードランド文IIの起動が動作しなくなります。
これは両処理がH.INIP(0FDC7h)のフックを前提としている事が原因であるため、H.INIP(0FDC7h)の内容をHB-11の内蔵メニューで設定される状態にします。

### HB-11の内蔵メニューでBASICを選択した場合の問題

HB-11の内蔵メニューはBASIC選択時に、次のフックのうち2つ以上がフックされていればDISK-BASICが有効であると判定します。

```
H.DSKO equ 0FDEFh
H.SETS equ 0FDF4h
H.NAME equ 0FDF9h
H.KILL equ 0FDFEh
```

DISK-BASICが無効と判定された場合、スロット0-0 の 7D20H に ```CALSLT``` しROM BASICを開始します。

DISK-BASICが有効と判定された場合、SP に 0C206h をセットし、マスターDOSカーネルのスロット(0F348H:MASTERS)の固定アドレス 59DBH に ```CALSLT``` します。

このアドレス(59DBH)は、DOS1ではDISK BASICの初期化処理となっています。turboRのDOS2やNEXTORのDOS1カーネルではDOS1と同等ですが、DOS2カーネルは該当アドレスの処理は不定です。DOS2はMSX1に対応しないため考慮する必要はありません。NEXTORではinit.macの次の処理が本来 CALSLT して欲しい場所と推測されます。

```
SRCBAS	equ	7DEEh
BASROM	equ	0FBB1h

	ld	a,(BASROM)
	or	a
	ld	ix,SRCBAS-5	;Because BASIC cartridge is no more enabled.
	jr	nz,GO_BASIC
```

この処理の存在するアドレスはNextorのバージョンによって異なるため、bank0から検索する必要があります。
本パッチでは、59DBHから発見したアドレスに分岐するように書き換えます。また、この書き換えで破壊される59DBHの前後の処理は他の場所に退避します。

SP に 0C206h をセットしているのは、拡張されていないスロットに対する CALSLT により、スタックが6バイト消費され、59DBH に到達時に SPが 0C200h となっている事を期待しているものと思われます。マスターDOSカーネルのスロットが拡張されている場合、スタック消費量が想定より8バイトほど増えます。ディスク版ソフトのブートセクタがこのスタックを使うため、スタック不足により問題が発生する可能性は否定できません。スタック位置を修正する処理を追加すれば互換性がさらに向上するものと思われますが、今回は対応しません。

