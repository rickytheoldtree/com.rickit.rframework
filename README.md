# RicKit.RFramework Documentation

> [中文版](README.zh-CN.md)

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework)

> ⚡ **Inspired by [QFramework](https://github.com/liangxiegame/QFramework)** – RicKit.RFramework’s command system and service locator pattern borrow heavily from QFramework, while delivering a lightweight, Unity-friendly implementation.

---

## Table of Contents

1. [Introduction](#introduction)  
2. [Features](#features)  
3. [Architecture Overview](#architecture-overview)  
4. [Installation](#installation)  
5. [Getting Started](#getting-started)  
6. [Service Locator Lifecycle & Initialization](#service-locator-lifecycle--initialization)  
7. [Dependency Injection & Service Registration](#dependency-injection--service-registration)  
8. [Dependency Injection in Commands & MonoBehaviours](#dependency-injection-in-commands--monobehaviours)  
9. [Event System](#event-system)  
10. [Command System](#command-system)  
11. [Usage Examples](#usage-examples)  
12. [Best Practices](#best-practices)  
13. [Advanced Topics](#advanced-topics)  
14. [Contributing](#contributing)  
15. [License](#license)  

---

## Introduction

RicKit.RFramework is a lightweight **service locator** and **messaging** framework for Unity and C# projects. It supports:

- **Dependency Injection** via a global ServiceLocator  
- **Event Bus** for publish/subscribe patterns  
- **Command Dispatch** (CQRS-style Request-Handler)  

By relying on a **single central locator**, you avoid fragile static references while still enjoying fast lookup and automatic lifecycle management.

---

## Features

- Zero-configuration: register services and commands at runtime.  
- Automatic initialization, startup, and deinitialization of services.  
- **Event System**: strongly-typed events with register/unregister/send.  
- **Command System**: support for:
  - No-arg no-return commands  
  - No-arg with-return commands  
  - Arg-only, no-return commands  
  - Arg-and-return commands  
- Full **Dependency Injection** support via `TryGetService<T>()`.  
- Fully **extension-method-based** API: dispatch commands or events from any object implementing locator interfaces.  
- Inspired by QFramework’s patterns but implemented independently and streamlined for performance.

---

## Architecture Overview

1. **ServiceLocator** (`ServiceLocator<T>`)  
   - Generic singleton locator  
   - Manages a **Cache** of `IService` implementations  
   - Calls `Init()`, `Start()`, `DeInit()` in proper order  
2. **IService** / **AbstractService**  
   - Base interface for all services  
   - Lifecycle: `Init()`, `Start()`, `DeInit()`  
3. **ICanGetLocator<T>** / **ICanSetLocator**  
   - Enables any class to retrieve the locator instance via `this.GetLocator()`  
4. **EventExtension**  
   - `RegisterEvent<T>(Action<T>)`  
   - `UnRegisterEvent<T>(Action<T>)`  
   - `SendEvent<T>(T arg)`  
5. **CommandExtension**  
   - `SendCommand<TCommand>()`  
   - `SendCommand<TCommand, TResult>()`  
   - `SendCommand<TCommand, TArgs, TResult>(TArgs args)`  
   - `SendCommand<TCommand, TArgs>(TArgs args)`  

---

## Installation

Install via **OpenUPM**:

```bash
npm install com.rickit.rframework --registry https://package.openupm.com
```

_or via Unity Package Manager (UPM) UI:_

1. Open `Window → Package Manager`.  
2. Click “+” → “Add package by name…”.  
3. Enter `com.rickit.rframework`.  

---

## Getting Started

1. **Initialize** your custom locator in bootstrap code (e.g., on game launch):

   ```csharp
   MyGameLocator.Initialize();
   ```

2. **Create** a subclass of `ServiceLocator<T>` and **register services** in its `Init()` override:

   ```csharp
   public class MyGameLocator : ServiceLocator<MyGameLocator>
   {
       protected override void Init()
       {
           // Register services by interface
           RegisterService<IAnalyticsService>(new AnalyticsService());
           RegisterService<IDataService>(new DataService());
       }
   }
   ```

3. **Retrieve** services in any object by implementing `ICanGetLocator<MyGameLocator>`:

   ```csharp
   public class PlayerController : MonoBehaviour,
       ICanGetLocator<MyGameLocator>
   {
       private IAnalyticsService analytics;

       void Awake()
       {
           // Inject via TryGetService in Awake
           this.TryGetService(out analytics);
           analytics.TrackEvent("game_start");
       }

       void Start()
       {
           // You can also get services directly
           var data = this.GetService<IDataService>();
           data.SaveGame();
       }
   }
   ```

---

## Service Locator Lifecycle & Initialization

- **Static Initialize**  
  1. Creates locator instance  
  2. Calls your `Init()` override to register services  
  3. Calls each registered service’s `Init()`  
  4. Calls each service’s `Start()`, sets `IsInitialized = true`  
  5. Sets locator’s `IsInitialized = true`  

- **DeInit**  
  - Calls `DeInit()` on all initialized services  
  - Clears the static locator reference  

---

## Dependency Injection & Service Registration

Register all services in your locator’s `Init()`:

```csharp
public interface IAnalyticsService : IService
{
    void TrackEvent(string name);
}

public class AnalyticsService : AbstractService, IAnalyticsService
{
    public override void Init() { /* attach SDK */ }
    public void TrackEvent(string name) { /* ... */ }
}

// In MyGameLocator.Init():
RegisterService<IAnalyticsService>(new AnalyticsService());
```

- Services must implement `IService`.  
- Locator will inject itself into each service before initialization.  
- Use `this.GetService<Interface>()` or `this.TryGetService(out T)` to obtain dependencies.

---

## Dependency Injection in Commands & MonoBehaviours

You can perform injection in both commands and MonoBehaviours using `TryGetService<T>()`:

```csharp
// Command Example
public class KillPlayerCommand
    : AbstractCommand<int, int>
{
    private IPlayerService playerService;

    public override void Init()
    {
        // Inject dependency in Init
        this.TryGetService(out playerService);
    }

    public override int Execute(int playerId)
    {
        playerService.Kill(playerId);
        return playerId;
    }
}

// MonoBehaviour Example
public class GameStarter : MonoBehaviour,
    ICanGetLocator<MyGameLocator>
{
    private IDataService dataService;

    void Awake()
    {
        // Inject in Awake
        this.TryGetService(out dataService);
        dataService.LoadGame();
    }
}
```

---

## Event System

```csharp
// Any class implementing ICanGetLocator<MyGameLocator>:
this.RegisterEvent<PlayerDamagedEvent>(evt => { /* handle damage */ });
this.UnRegisterEvent<PlayerDamagedEvent>(handler);
this.SendEvent(new PlayerDamagedEvent { Damage = 10 });
```

- Handlers stored in a `Dictionary<Type, Delegate>` on the locator.  
- Supports multiple subscribers per event type.

---

## Command System

> **Do not instantiate command classes manually.** Always dispatch via extension methods on `ICanGetLocator<T>`.

```csharp
// No-arg, no-return
this.SendCommand<ResetGameCommand>();

// No-arg, return
int count = this.SendCommand<GetPlayerCountCommand, int>();

// Arg-only, no-return
this.SendCommand<LogEventCommand, string>("Player died.");

// Arg-and-return
int killedId = this.SendCommand<KillPlayerCommand, int, int>(playerId);
```

- Commands are cached in the locator and initialized once before first use.

---

## Usage Examples

1. **Service Example**

   ```csharp
   public class DataService : AbstractService, IDataService
   {
       public override void Init() { /* load DB */ }
       public void SaveGame() { /* ... */ }
   }

   // Registered in MyGameLocator.Init()
   // Retrieved anywhere in ICanGetLocator:
   var data = this.GetService<IDataService>();
   data.SaveGame();
   ```

2. **Command with Custom Struct**

   ```csharp
   public struct PlayerInfo { public int Id; public string Name; }

   public class LogPlayerInfoCommand
       : AbstractCommandOnlyArgs<PlayerInfo>
   {
       public override void Init() { this.TryGetService(out var logger); }
       public override void Execute(PlayerInfo info)
       {
           Debug.Log($"Player {info.Id}: {info.Name}");
       }
   }

   this.SendCommand<LogPlayerInfoCommand, PlayerInfo>(
       new PlayerInfo { Id = 1, Name = "Alice" });
   ```

3. **Command with Tuple**

   ```csharp
   public class ProcessScoresCommand
       : AbstractCommand<(int Level, int Score), bool>
   {
       public override void Init() { }
       public override bool Execute((int Level, int Score) args)
       {
           return args.Score > 1000;
       }
   }

   bool passed = this.SendCommand<
       ProcessScoresCommand,
       (int Level, int Score),
       bool>((2, 1200));
   ```

---

## Best Practices

- Always implement `ICanGetLocator<MyGameLocator>` and use `this.GetService<T>()` or `this.TryGetService(out T)` for dependencies.  
- Perform dependency injection in `Init()` for commands or `Awake()` for MonoBehaviours.  
- Register all services in your locator’s `Init()`.  
- Use `SendCommand` / `SendEvent` extension methods only.  
- Call `MyGameLocator.Initialize()` once on startup and `MyGameLocator.I.DeInit()` on shutdown.

---

## Advanced Topics

- Scoped locators for scene-specific modules.  
- Integration with Zenject, VContainer, or other DI frameworks.  

---

## Contributing

1. Fork the repo.  
2. Create a feature branch.  
3. Open a PR against `main`.  
4. Add tests or example coverage.  

---

## License

[MIT License](LICENSE)
