# RicKit.RFramework 使用说明

> [English Version](README.md)

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework/)

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

RicKit.RFramework 是一套轻量级服务定位器（Service Locator）和消息派发框架，支持依赖注入、事件总线（Event）、命令派发（Command）等，适用于 Unity 与 C# 工程。

- OpenUPM 页面：[https://openupm.com/packages/rickit.rframework/](https://openupm.com/packages/rickit.rframework/)

---

## ServiceLocator 介绍与生命周期

**ServiceLocator** 是 RicKit.RFramework 的核心，负责所有服务的注册、管理和全局访问。

### 生命周期与初始化流程

- `ServiceLocator<T>.Initialize()` 为整个框架的启动入口，推荐仅调用一次。
- 典型流程如下：

  1. **创建并初始化 locator 实例**，调用 locator 的 `Init()`（在此注册所有服务实例）。
  2. **遍历所有已注册服务，依次调用各服务的 `Init()` 方法**（服务初始化，可进行依赖查找、事件注册等）。
  3. **再次遍历所有服务，依次调用 `Start()`**（服务启动，业务逻辑可安全使用依赖）。
  4. 每个服务 `IsInitialized = true`，最后 locator 自身也置为 `IsInitialized = true`。

- **若在 locator 初始化后新注册服务，则立即调用该服务的 Init、Start，并设置 IsInitialized = true。**

#### 推荐注册与初始化时机

- 所有服务实例应在 locator 的 `Init()` 方法中注册（即 RegisterService 调用）。
- 框架自动管理服务的 Init/Start 调用顺序，无需业务手动干预。
- 业务层（如 MonoBehaviour、命令等）应在 Awake/Init 阶段通过 TryGetService/GetService 获取服务依赖。

#### DeInit（反初始化）

- 调用 locator 的 `DeInit()`，会对所有已初始化服务调用 DeInit，并释放 locator 实例。

---

## 依赖注入与服务注册

- 服务注册应集中写在 ServiceLocator 的 Init 方法中。
- 服务实现应继承自 `AbstractService` 并实现 Init、Start、DeInit。
- 推荐业务层通过 `TryGetService` 或 `GetService` 获取依赖服务，避免直接依赖 Locator 实例。

---

## Event 系统

### 核心机制

- 事件以泛型参数 `T` 作为类型区分，本质是对 `Action<T>` 的注册与派发。
- 通过 `IServiceLocator` 扩展方法实现注册、注销、派发，并支持 `ICanGetLocator<T>` 对象的快捷调用。
- 所有事件监听器存储于 `Dictionary<Type, Delegate>` 中，类型安全。

### 关键接口

- `RegisterEvent<T>(Action<T> action)`：注册事件监听。
- `UnRegisterEvent<T>(Action<T> action)`：注销事件监听。
- `SendEvent<T>(T arg = default)`：派发事件。

### 使用建议

- 事件注册与注销建议在对象生命周期早期（如 `Awake`/`Init`/`Start`）与销毁（`OnDestroy`）时进行。

---

## Command 系统

### 核心机制

- 命令系统采用“请求-处理器”（CQRS/Request-Handler）思想，不仅是传统命令模式。
- 命令以类为标识，支持多种签名：无参/有参、有无返回值。
- 所有命令实例均由 ServiceLocator 创建、缓存与复用，支持参数传递和自动依赖注入。
- 每个命令首次执行前会自动调用 `Init()` 完成依赖注入。
- 命令的 `Execute()` 方法负责具体业务逻辑，可带参数和/或返回值。

### 关键接口

- `ICommand`：基础命令接口，包含 `Init()` 和 `Execute()`。
- `ICommand<TResult>`：带返回值的命令接口，`Execute()` 返回 `TResult`。
- `ICommand<TArgs, TResult>`：带参数和返回值的命令接口。
- `ICommandOnlyArgs<TArgs>`：带参数无返回值的命令接口。
- `AbstractCommand` / `AbstractCommand<TResult>` / `AbstractCommand<TArgs, TResult>` / `AbstractCommandOnlyArgs<TArgs>`：推荐继承的抽象基类。
- `SendCommand<TCommand>(...)` / `SendCommand<TCommand, TResult>(...)` / `SendCommand<TCommand, TArgs, TResult>(TArgs args)` / `SendCommandOnlyArgs<TCommand, TArgs>(TArgs args)`：通过 ServiceLocator 或 ICanGetLocator 扩展方法派发命令。

### 使用建议

- 命令类建议无状态，依赖通过重写 `Init()` 注入，避免在 `Execute()` 查找依赖。
- 命令用于封装单一业务处理逻辑，便于解耦和单元测试。
- 派发命令统一通过 `SendCommand` 或 `SendCommandOnlyArgs`，不要手动 new 命令实例。

---

## 示例

以下示例结合实际推荐用法，展示服务实现、注册、依赖注入、事件与命令的使用：

### 1. 服务接口与实现

```csharp
public interface IVibrateService : IService
{
    void Vibrate(int milliseconds = 2);
}

public class VibrateService : AbstractService, IVibrateService
{
    private ISettingsDataService settingsDataService;

    public override void Init()
    {
        this.TryGetService(out settingsDataService);
    }

    public void Vibrate(int milliseconds = 2)
    {
        if (!settingsDataService.Vibrate.Value) return;
        PlatformUtils.Vibrate(milliseconds);
    }
}
```

### 2. 服务注册（在 ServiceLocator 的 Init 中集中注册）

```csharp
public class Entity : ServiceLocator<Entity>
{
    public override void Init()
    {
        RegisterService<IVibrateService>(new VibrateService());
        // ...注册其他服务
    }
}
```

### 3. 业务层获取服务并使用

```csharp
public class SomeGameLogic : ICanGetLocator<Entity>
{
    private IVibrateService vibrateService;

    public void Init()
    {
        this.TryGetService(out vibrateService);
    }

    public void OnSpecialEvent()
    {
        vibrateService?.Vibrate(10);
    }
}
```

### 4. Event（事件）系统用法

#### 事件声明

```csharp
public struct PlayerDiedEvent
{
    public int PlayerId;
}
```

#### 事件监听与注销（Unity MonoBehaviour 示例）

```csharp
using UnityEngine;

public class PlayerUI : MonoBehaviour, ICanGetLocator<Entity>
{
    void Awake()
    {
        this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent evt)
    {
        // 响应玩家死亡，例如显示UI
    }

    void OnDestroy()
    {
        this.UnRegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
}
```

#### 事件派发

```csharp
// 某处触发事件
this.SendEvent(new PlayerDiedEvent { PlayerId = 1 });
```

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
// 派发命令并获取返回值
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
this.SendCommandOnlyArgs<LogEventCommand, string>("玩家死亡。");
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

- **所有服务注册应集中于 ServiceLocator 的 Init 方法，不推荐在服务自身 Init 主动注册自己。**
- **业务代码通过 TryGetService 获取依赖服务，实现解耦。**
- **服务 Init/Start 由框架统一调度，保障依赖顺序和可用性。**
- **如需反初始化，调用 DeInit 即可自动依次反初始化所有服务。**
- **ServiceLocator 只在启动与服务注册等全局场景用，业务层请用服务接口。**
