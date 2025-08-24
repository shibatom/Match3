# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code)へのガイダンスを提供します。

## プロジェクト概要

これはUnityベースのマッチ3ゲームテンプレートプロジェクトで、元々「Match 3 Kingdom Complete Game」アセットをベースとし、モバイルゲーム「猫とわたし」向けに改修中です。Royal Matchのゲームプレイメカニクスを模倣しています。

## Unity設定

- **Unityバージョン**: 6000.0.53f1（必須）
- **ターゲットプラットフォーム**: iOS 15+ / Android 9.0+  
- **フレームレート**: 60 FPS固定
- **画面向き**: ポートレートモード優先、ランドスケープ対応
- **会社名**: HarrisonStreet
- **プロダクト名**: Match3

## ビルドと開発コマンド

### Unityビルド
- Unity Hubを開き、Unity 6000.0.53f1がインストールされていることを確認
- `/mnt/e/Match3`ディレクトリからプロジェクトを開く
- iOSビルド: File → Build Settings → iOS → Build
- Androidビルド: File → Build Settings → Android → Build
- エディタで実行: UnityエディタのPlayボタンを押す

### レベルテスト
- UnityエディタでMainManagerの`currentLevel`を変更して特定レベルをテスト
- Level Editorシーンを使用してレベルを作成/修正
- レベルデータは`Assets/BaxterMatch3/Levels/`にScriptableObjectsとして保存

## コアアーキテクチャ

### ゲーム状態管理
ゲームは`MainManager.cs`によって管理される状態マシンパターンを使用：
- **GameState列挙型**: 全体的なゲームフロー制御（Map、PrepareGame、Playing、GameOver、Win等）
- **MainManager.Instance**: グローバルゲーム制御のためのシングルトンパターン
- 状態遷移はアニメーション、入力ブロック、ゲームフローを処理

### フィールドとアイテムシステム
- **FieldBoard.cs**: ゲームボードグリッド管理（デフォルト9x9）
  - Rectangle（スクエア）オブジェクトの作成と管理
  - アイテム生成と落下メカニクスの処理
  - CombinationManagerを通じたマッチ検出制御
  
- **Item.cs**: 全マッチ3アイテムの基底クラス
  - ItemsTypes列挙型が特殊アイテムを定義（Bomb、Rocket、DiscoBall等）
  - ドラッグ、切り替え、破壊アニメーションを処理
  - パフォーマンスのためプーリングを実装

### マッチ検出システム
- **CombinationManager.cs**: コアマッチングアルゴリズム
  - 水平・垂直マッチを検出
  - ボーナスアイテム生成を決定（4マッチ→Rocket、5マッチ→DiscoBall等）
  - 特殊アイテムの組み合わせを処理（Rocket+Bomb等）
  
### ターゲットシステム
- **Target.cs**（抽象）: レベル目標のベース
  - サブクラスが特定ターゲットを定義（アイテム収集、ブロック破壊等）
  - サブレベル間の進捗を追跡
  - TargetGUIGroupを通じてUIを更新

### レベルデータ構造
- **LevelData.cs**: 全レベル設定を含む
  - ターゲット目標とカウント
  - 移動/時間制限
  - フィールドレイアウトと障害物
  - サブレベル設定
  
- **ScriptableObjectLevel.cs**: 永続的レベルストレージ
  - Unityアセットとしてシリアライズ
  - カスタムLevel Editorで編集

### アイテムタイプと動作
通常アイテムは`Item.cs`から継承：
- **SimpleItem**: 基本的な色付きジェム
- **BombItem**: 3x3エリアで爆発
- **RocketItem**: 行または列をクリア  
- **DiscoBallItem**: 1色の全アイテムを削除
- **ChopperItem**: 特殊な飛行アイテム

障害物/ブロック：
- **LayeredBlock**: 複数回ヒットが必要な障害物
- **HoneyBlock**: 落下を防ぐ粘着ブロック
- **BreakableBox**: 破壊可能なコンテナ

### GUIとメニューシステム
- **ゲーム前**: MenuPlayControllerが目標を表示
- **ゲーム中**: ターゲットカウンター、移動カウンター、スコア表示
- **ゲーム後**: MenuCompleteController（勝利）/ MenuFailController（敗北）
- **ブースター**: BoostInventoryがパワーアップを管理

### アニメーションとエフェクト
- スムーズなアニメーションにDOTweenを使用
- 爆発とマッチのパーティクルエフェクト
- アイテム移動のトレイルレンダラー
- UIアニメーション用のカスタムAnimationUIシステム

## 主要な名前空間

- `Internal.Scripts`: コアゲームロジック
- `Internal.Scripts.Items`: アイテムクラスとインターフェース
- `Internal.Scripts.Level`: フィールドとレベル管理
- `Internal.Scripts.System.Combiner`: マッチ検出
- `Internal.Scripts.TargetScripts.TargetSystem`: 目標システム
- `Internal.Scripts.GUI`: UIコントローラー
- `Internal.Scripts.Blocks`: 障害物実装

## 現在の課題（READMEより）

1. ゲームプレイの安定性に影響する複数のバグ
2. Royal Matchと比較して劣るUX（洗練度不足）
3. あと118レベル必要（182/300完了）
4. 猫テーマのビジュアルリデザインが必要

## 開発優先順位

1. ゲーム進行を妨げるバグを最優先で修正
2. リリースに必要な最小限の機能を実装
3. 300レベル達成のための追加レベル作成
4. ゲームフィールとアニメーションの改善
5. 猫テーマのアセットでスプライトを置換
6. メインの「猫とわたし」ゲームとの統合

## 重要なファイル

- `/Assets/BaxterMatch3/Scripts/Directory/MainManager.cs` - コアゲームコントローラー
- `/Assets/BaxterMatch3/Scripts/Directory/Level/FieldBoard.cs` - ボード管理
- `/Assets/BaxterMatch3/Scripts/Directory/Items/Item.cs` - ベースアイテムクラス
- `/Assets/BaxterMatch3/Scripts/Directory/System/Combiner/CombinationManager.cs` - マッチ検出
- `/Assets/BaxterMatch3/Levels/` - レベルデータストレージ
- `/Assets/BaxterMatch3/Prefabs/` - ゲームオブジェクトプレハブ

## デバッグ機能

- `MainManager.logEnabled`: デバッグログのトグル
- `DebugSettings.cs`: 各種デバッグフラグ
- `DisplayFPSClass`でFPS表示可能
- `DebuggingLevelTool`でレベルデバッグ

## Scratchpad & Checklist Organization System

### Directory Structure

```
.claude/
├── work/                    # Active work files
│   ├── checklists/         # Task tracking files
│   ├── active/             # Currently being worked on
│   └── archive/            # Completed (kept for reference)
├── scratchpads/            # Temporary working files
│   ├── findings/           # Research and analysis results
│   ├── plans/              # Implementation plans
│   └── temp/               # Very temporary files (auto-cleanup)
├── agents/                 # Inter-agent communication
│   ├── shared/             # Shared findings between agents
│   └── handoffs/           # Task handoff files
├── templates/              # Reusable templates
└── logs/                   # Historical records
```

## Conversation Guidelines

- 常に日本語で会話する
- コメントは日本語で記載する