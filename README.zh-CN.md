# RicKit RFramework

[![openupm](https://img.shields.io/npm/v/com.rickit.rframework?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rframework/)

## 概述

这是一个轻量级的服务定位器框架，用于在 C# 应用中管理服务的生命周期。它支持服务的初始化、启动、反初始化，以及可选的服务依赖管理，既适用于 Unity，也可用于通用 C# 项目。

---

## 最佳实践：用接口注册服务

**强烈建议用接口类型注册服务，而不是直接注册实现类。**  
这样可以提升解耦性，支持依赖反转，并便于单元测试。

**示例：**
```csharp
// 定义接口
public interface IGameService : IService
{
    void DoSomething();
}

// 实现接口
public class GameService : AbstractService, IGameService
{
    public void DoSomething() { /* ... */ }
}

// 按接口注册
public class GameLocator : ServiceLocator<GameLocator>
{
    public override void Init()
    {
        base.Init();
        RegisterService<IGameService>(new GameService());
    }
}

// 按接口获取
var service = this.GetService<IGameService>();
```

---

## 核心接口与类

### `IServiceLocator`
- 表示服务定位器的接口。
- 提供以下能力：
  - 获取 (`GetService<T>()`) 或尝试获取 (`TryGetService<T>()`) 已注册的服务。
  - 访问全局事件 `Events`。

### `ICanInit`
- 基础生命周期接口：
  - `Init()` 初始化
  - `DeInit()` 反初始化
  - `IsInitialized` 初始化状态标记

### `ICanSetLocator`
- 表示服务支持被注入其所归属的 `IServiceLocator`。

### `ICanStart`
- 表示服务支持 `Start()` 启动阶段。

### `IService`
- 组合了 `ICanInit`、`ICanStart`、`ICanGetLocator` 和 `ICanSetLocator` 的接口，是服务的基础接口。

### `ICanGetLocator`
- 提供获取当前服务定位器的方法。

### `ICanGetLocator<T>`
- 默认实现 `ICanGetLocator`，返回 `ServiceLocator<T>.I`。

---

## 主类：`ServiceLocator<T>`

- 泛型单例基类，用于创建具体的服务定位器类型。

示例：
```csharp
public class MyGameLocator : ServiceLocator<MyGameLocator> {}
```

### 主要成员

- `static T I`：单例访问器。
- `Initialize()`：初始化定位器。
- `RegisterService<T>(TService service)`：
  - 设置 `Locator`
  - 初始化服务
  - 如果定位器已经初始化，则启动服务。
- `DeInit()`：反初始化所有服务并清除单例。

### 内部类 `Cache`

- 用于存储所有注册的服务。
- 基于 `Dictionary<Type, IService>` 和 `List<IService>`。

### 自定义初始化

你可以在具体定位器中重写 `Init()` 方法，实现自定义初始化逻辑：

```csharp
public class GameLocator : ServiceLocator<GameLocator>
{
    public override void Init()
    {
        base.Init();
        RegisterService<IGameService>(new GameService());
        // 可在此注册更多服务
    }
}
```

---

## 抽象服务类：`AbstractService`

- 实现 `IService`
- 提供默认的生命周期钩子（可重写）：
  - `Init()` 初始化
  - `Start()` 启动
  - `DeInit()` 反初始化

---

## 实用类：`BindableProperty<T>`

- 封装可绑定属性，支持监听值变化。
- 方法：
  - `Register(Action<T>)` 注册监听
  - `RegisterAndInvoke(Action<T>)` 注册并立即调用
  - `UnRegister(Action<T>)` 移除监听
  - `SetWithoutInvoke(T)` 设置值但不触发事件

---

## 扩展方法：`ServiceExtension`

- 为实现 `ICanGetLocator` 的对象提供简洁的服务访问方式：

```csharp
var myService = someComponent.GetService<IGameService>();
```

- 支持安全访问：

```csharp
if (someComponent.TryGetService(out IGameService service)) { ... }
```

### 获取服务的推荐方式

继承 `ICanGetLocator<GameLocator>` 接口的对象可以直接通过扩展方法访问服务：

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

## 异常类型

### `ServiceNotFoundException`
- 服务未注册时报错。
- 构造方法：
```csharp
new ServiceNotFoundException(typeof(IGameService))
```

### `ServiceAlreadyExistsException`
- 注册重复服务时报错。
- 构造方法：
```csharp
new ServiceAlreadyExistsException(typeof(IGameService))
```

---

## 使用示例

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

// 初始化定位器
GameLocator.Initialize();

// 从继承 ICanGetLocator<GameLocator> 的对象中获取服务
public class GameLogic : ICanGetLocator<GameLocator>
{
    public void Run()
    {
        var gameService = this.GetService<IGameService>();
    }
}
```

---

## 注意事项

- 使用服务前必须调用 `Initialize()` 初始化。
- 服务在注册时，如果定位器已初始化，会自动调用 `Start()`。
- 设计上适用于 Unity 架构，但也可用于通用 C# 应用。

---

## 推荐扩展

- 日志支持
- 服务依赖校验
- 异步生命周期支持
