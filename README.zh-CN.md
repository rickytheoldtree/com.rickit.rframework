# RicKit.RFramework 使用说明

> [English Version](README.md)

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework/)

> ⚡ **灵感来源：[QFramework](https://github.com/liangxiegame/QFramework) —— RicKit.RFramework 的命令系统深受 QFramework 启发，并在此基础上实现了轻量级服务定位与消息机制。**

---

## 目录

- [简介](#简介)
- [ServiceLocator 介绍与生命周期](#servicelocator-介绍与生命周期)
- [依赖注入与服务注册](#依赖注入与服务注册)
- [Event 系统](#event-系统)
- [Command 系统](#command-系统)
- [示例](#示例)
- [最佳实践](#最佳实践)

---

## 简介

RicKit.RFramework 是一套轻量级服务定位器（Service Locator）和消息派发框架，支持依赖注入、事件总线（Event）、命令派发（Command）等，适用于 Unity 与 C# 工程开发。

- OpenUPM 页面：[https://openupm.com/packages/rickit.rframework/](https://openupm.com/packages/rickit.rframework/)
- 致敬 QFramework: [https://github.com/liangxiegame/QFramework](https://github.com/liangxiegame/QFramework)

---

## Command 系统

### 核心机制

命令系统采用“请求-处理器”（CQRS/Request-Handler）思想，设计风格与 [QFramework](https://github.com/liangxiegame/QFramework) 命令体系高度相似，但为独立实现。

- 命令以类为标识，支持多种签名：
  - 无参无返回值（`ICommand`, `AbstractCommand`）
  - 无参有返回值（`ICommand<TResult>`, `AbstractCommand<TResult>`）
  - 有参有返回值（`ICommand<TArgs, TResult>`, `AbstractCommand<TArgs, TResult>`）
  - 有参无返回值（`ICommandOnlyArgs<TArgs>`, `AbstractCommandOnlyArgs<TArgs>`）
- 所有命令实例均由 ServiceLocator 创建、缓存与复用，支持参数传递和自动依赖注入。每个命令首次执行前会自动调用 `Init()` 完成依赖注入。
- 命令的 `Execute()` 方法负责具体业务逻辑，可带参数和/或返回值。

### 命令接口与基类

```csharp
public interface ICommand : ICanGetLocator, ICanSetLocator
{
    void Init();
    void Execute();
}

public interface ICommand<out TResult> : ICommand
{
    new TResult Execute();
}
public interface ICommandOnlyArgs<in TArgs> : ICommand
{
    void Execute(TArgs args);
}
public interface ICommand<in TArgs, out TResult> : ICommand
{
    TResult Execute(TArgs args);
}
```

框架为每种命令类型都提供了抽象基类：

- `AbstractCommand`（无参无返回）
- `AbstractCommand<TResult>`（无参有返回）
- `AbstractCommand<TArgs, TResult>`（有参有返回）
- `AbstractCommandOnlyArgs<TArgs>`（有参无返回）

### 命令派发扩展方法

**请勿手动 new 命令实例，始终通过框架扩展方法派发命令，以保障依赖注入与缓存。**

```csharp
// 无参无返回
this.SendCommand<SomeCommand>();

// 无参有返回
var result = this.SendCommand<GetValueCommand, int>();

// 有参有返回
var result = this.SendCommand<CalcCommand, int, int>(input);

// 有参无返回
this.SendCommand<LogCommand, string>("日志信息");
```

> **注意：**  
> 带参数无返回值命令应使用 `this.SendCommand<命令类型, 参数类型>(参数)`，而不是 `SendCommandOnlyArgs`。  
> 例如：`this.SendCommand<LogEventCommand, string>("玩家死亡。");`

### 使用建议

- 命令类建议无状态，依赖通过重写 `Init()` 注入，避免在 `Execute()` 查找依赖。
- 命令用于封装单一业务处理逻辑，便于解耦和单元测试。
- 派发命令统一通过 `SendCommand`，不要手动 new 命令实例。
- 命令系统用法高度借鉴 QFramework，具体可参考 [QFramework Command System](https://github.com/liangxiegame/QFramework)。

---

## 示例

### 5. Command（命令）系统用法

#### 有参数有返回值命令

```csharp
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
```

#### 派发命令并获取返回值

```csharp
int killedId = this.SendCommand<KillPlayerCommand, int, int>(playerId);
```

#### 只有参数无返回值命令

```csharp
public class LogEventCommand : AbstractCommandOnlyArgs<string>
{
    public override void Init() {}

    public override void Execute(string message)
    {
        Debug.Log(message);
    }
}

// 派发命令
this.SendCommand<LogEventCommand, string>("玩家死亡。");
```

#### 无参数有返回值命令

```csharp
public class GetPlayerCountCommand : AbstractCommand<int>
{
    private IPlayerService playerService;

    public override void Init()
    {
        this.TryGetService(out playerService);
    }

    public override int Execute()
    {
        return playerService.GetPlayerCount();
    }
}

// 派发命令
int count = this.SendCommand<GetPlayerCountCommand, int>();
```

---

## 最佳实践

- **所有命令实例都不要手动 new，始终使用 SendCommand 派发，由框架负责依赖注入和缓存。**
- **命令为无状态单例，由 ServiceLocator 统一管理。**
- **命令系统用法高度借鉴 QFramework，具体可参考 [QFramework Command System](https://github.com/liangxiegame/QFramework)。**

---
