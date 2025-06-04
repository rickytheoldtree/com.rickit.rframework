# RicKit RFramework

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework/)

🌏 [中文文档 (Chinese README)](README.zh-CN.md)

## Overview

RicKit RFramework is a lightweight service locator framework for managing service lifecycles in C# applications. It supports service initialization, startup, de-initialization, and optional dependency management, designed to be both Unity-friendly and usable in generic C# projects.

---

## Best Practice: Register Services by Interface

**It is recommended to register services using their interfaces, not concrete classes.**  
This approach improves decoupling, supports dependency inversion, and makes testing easier.

**Example:**
```csharp
// Define an interface
public interface IGameService : IService
{
    void DoSomething();
}

// Implement the interface
public class GameService : AbstractService, IGameService
{
    public void DoSomething() { /* ... */ }
}

// Register by interface
public class GameLocator : ServiceLocator<GameLocator>
{
    public override void Init()
    {
        base.Init();
        RegisterService<IGameService>(new GameService());
    }
}

// Retrieve by interface
var service = this.GetService<IGameService>();
```

---

## Core Interfaces and Classes

### `IServiceLocator`
- The main service locator interface.
- Provides:
  - `GetService<T>()` and `TryGetService<T>()` to retrieve registered services.
  - Access to global events via `Events`.

### `ICanInit`
- Base lifecycle interface:
  - `Init()` for initialization
  - `DeInit()` for de-initialization
  - `IsInitialized` status flag

### `ICanSetLocator`
- Indicates that a service can have its owning `IServiceLocator` injected.

### `ICanStart`
- Indicates that a service supports a `Start()` phase.

### `IService`
- Combines `ICanInit`, `ICanStart`, `ICanGetLocator`, and `ICanSetLocator` as the base service interface.

### `ICanGetLocator`
- Provides a method to get the current service locator.

### `ICanGetLocator<T>`
- Default implementation of `ICanGetLocator`, returns `ServiceLocator<T>.I`.

---

## Main Class: `ServiceLocator<T>`

- Generic singleton base for creating concrete service locator types.

Example:
```csharp
public class MyGameLocator : ServiceLocator<MyGameLocator> {}
```

### Main Members

- `static T I`: Singleton accessor.
- `Initialize()`: Initialize the locator.
- `RegisterService<T>(TService service)`:
  - Sets the `Locator`
  - Initializes the service
  - If the locator is already initialized, starts the service.
- `DeInit()`: De-initializes all services and clears the singleton.

### Internal Class: `Cache`

- Stores all registered services.
- Based on `Dictionary<Type, IService>` and `List<IService>`.

### Custom Initialization

Override the `Init()` method in your locator for custom logic:

```csharp
public class GameLocator : ServiceLocator<GameLocator>
{
    public override void Init()
    {
        base.Init();
        RegisterService<IGameService>(new GameService());
        // Register more services here
    }
}
```

---

## Abstract Service Class: `AbstractService`

- Implements `IService`
- Provides lifecycle hooks (overridable):
  - `Init()` initialization
  - `Start()` startup
  - `DeInit()` de-initialization

---

## Utility: `BindableProperty<T>`

- Encapsulates a bindable property, supporting value change listeners.
- Methods:
  - `Register(Action<T>)`: Register a listener
  - `RegisterAndInvoke(Action<T>)`: Register and invoke immediately
  - `UnRegister(Action<T>)`: Remove a listener
  - `SetWithoutInvoke(T)`: Set value without triggering event

---

## Extension Methods: `ServiceExtension`

- Provides concise service access for objects implementing `ICanGetLocator`:

```csharp
var myService = someComponent.GetService<IGameService>();
```

- Supports safe access:

```csharp
if (someComponent.TryGetService(out IGameService service)) { ... }
```

### Recommended Service Access

Objects implementing `ICanGetLocator<GameLocator>` can access services directly:

```csharp
public class GameLogic : ICanGetLocator<GameLocator>
{
    public void DoSomething()
    {
        var service = this.GetService<IGameService>();
    }
}
```

---

## Exception Types

### `ServiceNotFoundException`
- Thrown when a service is not registered.
- Constructor:
```csharp
new ServiceNotFoundException(typeof(IGameService))
```

### `ServiceAlreadyExistsException`
- Thrown when a duplicate service is registered.
- Constructor:
```csharp
new ServiceAlreadyExistsException(typeof(IGameService))
```

---

## Usage Example

```csharp
public interface IGameService : IService
{
    void DoSomething();
}

public class GameService : AbstractService, IGameService
{
    public void DoSomething() { /* ... */ }
}

public class GameLocator : ServiceLocator<GameLocator>
{
    public override void Init()
    {
        base.Init();
        RegisterService<IGameService>(new GameService());
    }
}

// Initialize the locator
GameLocator.Initialize();

// Accessing services from an object implementing ICanGetLocator<GameLocator>
public class GameLogic : ICanGetLocator<GameLocator>
{
    public void Run()
    {
        var gameService = this.GetService<IGameService>();
    }
}
```

---

## Notes

- Always call `Initialize()` before using services.
- When a locator is already initialized, registering a service will automatically call `Start()`.
- Designed for Unity, but also suitable for general C# applications.

---

## Recommended Extensions

- Logging support
- Service dependency validation
- Async lifecycle support
