# NPC 信息提示

这是一个 SMAPI C# Mod：在 NPC 头顶显示当前客户端玩家自己的婚姻资格、今天是否送礼、是否已对话，以及好感度。

## 构建

在包含 `Stardew Valley.dll` 和 `StardewModdingAPI.dll` 的目录执行：

```powershell
dotnet build .\NpcInfoOverlay.csproj -c Release -p:GamePath="C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
```

可选设置 `GameModsPath` 自动复制到 Mods 目录。多人游戏中信息读取 `Game1.player`，因此非主机玩家看到的是自己的数据。
