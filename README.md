# RicKit.RFramework Documentation

> [中文版](README.zh-CN.md)

## Table of Contents

- [Introduction](#introduction)
- [ServiceLocator Lifecycle & Initialization](#servicelocator-lifecycle--initialization)
- [Dependency Injection & Service Registration](#dependency-injection--service-registration)
- [Event System](#event-system)
- [Command System](#command-system)
- [Examples](#examples)
- [Best Practices](#best-practices)

---

## Introduction

RicKit.RFramework is a lightweight service locator and messaging framework supporting dependency injection, event bus (Event), and command dispatch (Command), suitable for Unity and C# projects.

---

## ServiceLocator Lifecycle & Initialization

**ServiceLocator** is the core of RicKit.RFramework, managing registration, access, and lifecycle of all services.

### Initialization Sequence

- `ServiceLocator<T>.Initialize()` is the framework entry point and should be called **once**.
- The lifecycle is:

  1. **Create and initialize the locator instance**, calling its `Init()` (typically for registering all services).
  2. **Iterate all registered services and call their `Init()`** (for dependency resolution, event registration, etc.).
  3. **Iterate all services again and call their `Start()`** (now all services are safe to use each other).
  4. Each service is marked `IsInitialized = true`; finally, the locator itself is `IsInitialized = true`.

- **If you register a new service after locator is initialized, its Init and Start are called immediately.**

#### Recommended Practice

- **Register all service instances in the locator's `Init()` method.**
- The framework guarantees the correct order of Init/Start calls.
- Business code (MonoBehaviour, commands, etc.) should use TryGetService/GetService in Awake/Init to get service dependencies.

#### DeInit

- Calling locator's `DeInit()` will call DeInit on all initialized services and release the locator instance.

---

## Dependency Injection & Service Registration

- **All service registration should be centralized in the ServiceLocator's Init method.**
- Services should inherit from `AbstractService` and implement Init, Start, and DeInit.
- Business code should use `TryGetService` or `GetService` for dependency injection, never directly depend on the locator.

---

## Event System

### Core Mechanism

- Events are distinguished by generic parameter `T`, essentially registering and dispatching `Action<T>`.
- Registration, unregistration, and dispatch are implemented via extension methods on `IServiceLocator`, with convenient access for `ICanGetLocator<T>` objects.
- All event handlers are stored in a type-safe `Dictionary<Type, Delegate>`.

### Key Interfaces

- `RegisterEvent<T>(Action<T> action)`: Register an event listener.
- `UnRegisterEvent<T>(Action<T> action)`: Unregister an event listener.
- `SendEvent<T>(T arg = default)`: Dispatch an event.

### Usage Advice

- Register/unregister events in the early lifecycle (`Awake`/`Init`/`Start`) and on destruction (`OnDestroy`).

---

## Command System

### Core Mechanism

- Commands are identified by class name and support both void (`ICommand`) and return-value (`ICommand<TResult>`) flavors.
- Commands are created, cached, and reused via the ServiceLocator, supporting parameter passing and dependency injection.
- Each command's Init() is automatically called before first execution for dependency injection.

### Key Interfaces

- `ICommand`: Base command interface, including `Init()` and `Execute(params object[] args)`.
- `ICommand<TResult>`: Command interface with return value, `Execute` returns `TResult`.
- `AbstractCommand` / `AbstractCommand<TResult>`: Recommended abstract base classes.
- `SendCommand<TCommand>(...)` / `SendCommand<TCommand, TResult>(...)`: Command dispatch methods.

### Usage Advice

- Override `Init` in command classes for dependency injection; all dependencies will be injected before command execution.
- Commands should be stateless or short-lived; persistent state belongs in the Service layer.

---

## Examples

Below are usage examples covering service implementation, registration, dependency injection, event, and command features:

### 1. Service Interface & Implementation

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

### 2. Register Services in ServiceLocator

```csharp
public class Entity : ServiceLocator<Entity>
{
    public override void Init()
    {
        RegisterService<IVibrateService>(new VibrateService());
        // ...register other services
    }
}
```

### 3. Business Layer: Get and Use the Service

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

### 4. Event System Usage

#### Event Declaration

```csharp
public struct PlayerDiedEvent
{
    public int PlayerId;
}
```

#### Event Subscription and Unsubscription (Unity MonoBehaviour Example)

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
        // Respond to player death, e.g., show UI
    }

    void OnDestroy()
    {
        this.UnRegisterEvent<PlayerDiedEvent>(OnPlayerDied);
    }
}
```

#### Event Dispatch

```csharp
// Dispatch event somewhere in code
this.SendEvent(new PlayerDiedEvent { PlayerId = 1 });
```

### 5. Command System Usage

#### Command Definition

```csharp
public class KillPlayerCommand : AbstractCommand<int>
{
    private IPlayerService playerService;

    public override void Init()
    {
        this.TryGetService(out playerService);
    }

    public override int Execute(params object[] args)
    {
        int playerId = (int)args[0];
        playerService.Kill(playerId);
        return playerId;
    }
}
```

#### Command Dispatch and Return Value

```csharp
// Dispatch command and get result
int killedId = this.SendCommand<KillPlayerCommand, int>(playerId);
```

### Notes

- Service registration is centralized in ServiceLocator's Init method; lifecycle is auto-managed.
- All services are recommended to inject dependencies via TryGetService inside Init.
- Register/unregister events in Init/Awake and OnDestroy; event types are recommended as structs.
- Commands should be stateless, dependencies injected via Init, executed via SendCommand.

---

## Best Practices

- **Centralize all service registration in the global ServiceLocator. Do not self-register services inside their Init.**
- **Business code obtains service dependencies using TryGetService for loose coupling.**
- **Service Init/Start is managed by the framework for correct order and availability.**
- **Use DeInit for teardown; all services will be de-initialized in order.**
- **ServiceLocator should only be referenced for startup/global registration—business code should use service interfaces.**

---
For the Chinese documentation, please click [中文版](README.zh-CN.md).
