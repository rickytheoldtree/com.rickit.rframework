# RicKit.RFramework Documentation

> [中文版](README.zh-CN.md)

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework/)

> ⚡ **Inspired by [QFramework](https://github.com/liangxiegame/QFramework) – RicKit.RFramework’s command system is heavily inspired by QFramework and implements a lightweight service locator and messaging system.**

---

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

- OpenUPM page: [https://openupm.com/packages/rickit.rframework/](https://openupm.com/packages/rickit.rframework/)
- Inspired by QFramework: [https://github.com/liangxiegame/QFramework](https://github.com/liangxiegame/QFramework)

---

## Command System

### Core Mechanism

The command system adopts a "Request-Handler" (CQRS/Request-Handler) pattern, similar to [QFramework](https://github.com/liangxiegame/QFramework), but is implemented independently.

- Commands are identified by their class type and support a variety of signatures:
  - No argument, no return value (`ICommand`, `AbstractCommand`)
  - No argument, with return value (`ICommand<TResult>`, `AbstractCommand<TResult>`)
  - With argument, with return value (`ICommand<TArgs, TResult>`, `AbstractCommand<TArgs, TResult>`)
  - With argument, no return value (`ICommandOnlyArgs<TArgs>`, `AbstractCommandOnlyArgs<TArgs>`)
- All command instances are created, cached, and reused by the `ServiceLocator`, supporting parameter passing and automatic dependency injection. Each command's `Init()` is called once before first execution for dependency injection.
- The command's `Execute()` method contains the business logic and may accept arguments and/or return a result.

### Command interface and base class

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

The framework provides abstract base classes for each command type:

- `AbstractCommand` (no args, no return)
- `AbstractCommand<TResult>` (no args, return)
- `AbstractCommand<TArgs, TResult>` (args, return)
- `AbstractCommandOnlyArgs<TArgs>` (args, no return)

### Command dispatch extension methods

You should **never instantiate commands manually**. Instead, always dispatch via the framework's extension methods, e.g.:

```csharp
// For no-arg, no-return command
this.SendCommand<SomeCommand>();

// For no-arg, with-return command
var result = this.SendCommand<GetValueCommand, int>();

// For arg, with-return command
var result = this.SendCommand<CalcCommand, int, int>(input);

// For arg, no-return command
this.SendCommand<LogCommand, string>("Log Message");
```

> **Note:** The correct method for "argument, no return" commands is `this.SendCommand<CommandType, ArgType>(arg)`—not `SendCommandOnlyArgs`!  
> Example: `this.SendCommand<LogEventCommand, string>("Player died.");`

### Usage Advice

- Override `Init()` in command classes for dependency injection; all dependencies will be injected before command execution.
- Commands should be **stateless** or short-lived; persistent state belongs in the Service layer.
- Use the corresponding `SendCommand` extension methods to dispatch commands and get results.
- Do not instantiate command classes directly; always dispatch through the framework so that dependency injection and caching work correctly.

---

## Examples

### 5. Command System Usage

#### Command with argument and return value

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

#### Dispatch command and get result

```csharp
int killedId = this.SendCommand<KillPlayerCommand, int, int>(playerId);
```

#### Command with only argument (no return value)

```csharp
public class LogEventCommand : AbstractCommandOnlyArgs<string>
{
    public override void Init() {}

    public override void Execute(string message)
    {
        Debug.Log(message);
    }
}

// Dispatch command
this.SendCommand<LogEventCommand, string>("Player died.");
```

#### Command with no argument and return value

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

// Dispatch command
int count = this.SendCommand<GetPlayerCountCommand, int>();
```

---

## Best Practices

- **Do not instantiate command classes directly. Always use SendCommand extensions so that dependency injection and caching are correctly handled by the framework.**
- **Commands are stateless singletons managed by the ServiceLocator.**
- **The command system design and usage are inspired by QFramework. For more details, see [QFramework Command System](https://github.com/liangxiegame/QFramework).**

---
