# RicKit.RFramework 文档

> [English version](README.md)

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=OpenUPM&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework)

> ⚡ **借鉴自 [QFramework](https://github.com/liangxiegame/QFramework)** – RicKit.RFramework 的命令系统和服务定位器模式深受 QFramework 启发，提供轻量、Unity 友好的实现。

---

## 目录

1. [简介](#简介)  
2. [主要特性](#主要特性)  
3. [架构概览](#架构概览)  
4. [安装](#安装)  
5. [快速上手](#快速上手)  
6. [ServiceLocator 生命周期 & 初始化](#servicelocator-生命周期--初始化)  
7. [依赖注入 & 服务注册](#依赖注入--服务注册)  
8. [命令与 MonoBehaviour 中的依赖注入](#命令与-monobehaviour-中的依赖注入)  
9. [事件系统](#事件系统)  
10. [命令系统](#命令系统)  
11. [使用示例](#使用示例)  
12. [最佳实践](#最佳实践)  
13. [高级主题](#高级主题)  
14. [贡献指南](#贡献指南)  
15. [许可](#许可)  

---

## 简介

RicKit.RFramework 是一个轻量级 **服务定位器** 与 **消息传递** 框架，适用于 Unity 与 C# 项目，支持：

- 全局 ServiceLocator 实现 **依赖注入**  
- **事件总线**：类型安全的发布/订阅  
- **命令分发**：CQRS 风格的 Request-Handler  

---

## 主要特性

- 零配置：运行时注册服务和命令  
- 自动管理服务的初始化、启动和反初始化  
- **事件系统**：类型安全的注册/注销/发送  
- **命令系统**：
  - 无参无返回  
  - 无参有返回  
  - 仅参数无返回  
  - 参数与返回  
- **依赖注入** 全面支持，通过 `this.TryGetService<T>(out T)` 获取服务  
- 基于扩展方法的 API，任何实现定位接口的类均可发送命令或事件  
- 源自 QFramework，但独立实现，专注性能与简洁  

---

## 架构概览

1. **ServiceLocator** (`ServiceLocator<T>`)  
2. **IService** / **AbstractService**  
3. **ICanGetLocator<T>** / **ICanSetLocator**  
4. **EventExtension**  
5. **CommandExtension**  

---

## 安装

通过 **OpenUPM**：

```bash
npm install com.rickit.rframework --registry https://package.openupm.com
```

或在 UPM 界面添加：`com.rickit.rframework`

---

## 快速上手

1. 调用 `MyGameLocator.Initialize()` 在游戏入口初始化定位器。  
2. 在自定义子类 `Init()` 中注册服务：  
   ```csharp
   public class MyGameLocator : ServiceLocator<MyGameLocator>
   {
       protected override void Init()
       {
           RegisterService<IAnalyticsService>(new AnalyticsService());
           RegisterService<IDataService>(new DataService());
       }
   }
   ```  
3. 在任意类实现 `ICanGetLocator<MyGameLocator>`，通过扩展方法获取服务/命令：

   ```csharp
   public class PlayerController : MonoBehaviour,
       ICanGetLocator<MyGameLocator>
   {
       private IAnalyticsService analytics;

       void Awake()
       {
           this.TryGetService(out analytics);
           analytics.TrackEvent("game_start");
       }

       void Start()
       {
           var data = this.GetService<IDataService>();
           data.SaveGame();
       }
   }
   ```

---

## ServiceLocator 生命周期 & 初始化

- **Initialize**  
  1. 创建定位器  
  2. 调用子类 `Init()` 注册服务  
  3. 服务 `Init()` → `Start()` → 标记已初始化  
  4. 定位器标记已初始化  
- **DeInit**  
  - 服务 `DeInit()`  
  - 清理定位器实例  

---

## 依赖注入 & 服务注册

```csharp
public interface IAnalyticsService : IService
{
    void TrackEvent(string name);
}
public class AnalyticsService : AbstractService, IAnalyticsService
{
    public override void Init() { /* 初始化 SDK */ }
    public void TrackEvent(string name) { /* 上报 */ }
}
// 在 MyGameLocator.Init():
RegisterService<IAnalyticsService>(new AnalyticsService());
```

---

## 命令与 MonoBehaviour 中的依赖注入

在命令或 MonoBehaviour 的 `Init()` / `Awake()` 中使用 `this.TryGetService<T>(out T)` 自动注入：

```csharp
// 命令示例
public class KillPlayerCommand : AbstractCommand<int, int>
{
    private IPlayerService playerService;
    public override void Init()
    {
        this.TryGetService(out playerService);
    }
    public override int Execute(int playerId)
    {
        playerService.Kill(playerId);
        return playerId;
    }
}

// MonoBehaviour 示例
public class GameStarter : MonoBehaviour,
    ICanGetLocator<MyGameLocator>
{
    private IDataService dataService;
    void Awake()
    {
        this.TryGetService(out dataService);
        dataService.LoadGame();
    }
}
```

---

## 事件系统

```csharp
this.RegisterEvent<PlayerDamagedEvent>(evt => { /* 处理 */ });
this.UnRegisterEvent<PlayerDamagedEvent>(handler);
this.SendEvent(new PlayerDamagedEvent { Damage = 10 });
```

---

## 命令系统

```csharp
this.SendCommand<ResetGameCommand>();
int count = this.SendCommand<GetPlayerCountCommand, int>();
this.SendCommand<LogEventCommand, string>("玩家死亡");
int killedId = this.SendCommand<KillPlayerCommand, int, int>(playerId);
```

---

## 使用示例

1. **服务示例**  
   ```csharp
   var data = this.GetService<IDataService>();
   data.SaveGame();
   ```
2. **Struct 注入命令**  
   ```csharp
   public struct PlayerInfo { public int Id; public string Name; }
   this.SendCommand<LogPlayerInfoCommand, PlayerInfo>(
       new PlayerInfo { Id = 1, Name = "Alice" });
   ```
3. **Tuple 注入命令**  
   ```csharp
   bool result = this.SendCommand<
       ProcessScoresCommand,
       (int Level, int Score),
       bool>((2, 1200));
   ```

---

## 最佳实践

- 实现 `ICanGetLocator<MyGameLocator>` 并用 `this.GetService<T>()` / `this.TryGetService(out T)`。  
- 在命令 `Init()` 或 MonoBehaviour `Awake()` 中注入。  
- 服务统一在定位器 `Init()` 注册。  
- 启动时调用 `MyGameLocator.Initialize()`，关闭时 `MyGameLocator.I.DeInit()`。

---

## 高级主题

- 场景/模块级定位器(`Scoped Locator`)。  
- 与 Zenject/VContainer 等 DI 框架集成。

---

## 贡献指南

1. Fork → 创建分支 → PR  
2. 添加测试/示例

---

## 许可

[MIT 协议](LICENSE)
