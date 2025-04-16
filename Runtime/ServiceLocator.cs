using System;
using System.Collections.Generic;
using System.Linq;

namespace RicKit.RFramework
{
    public interface IServiceLocator : ICanInit
    {
        Events Events { get; }
        T GetService<T>() where T : IService;
        bool TryGetService<T>(out T service) where T : IService;
    }
    
    public interface ICanSetLocator
    {
        void SetLocator(IServiceLocator locator);
    }

    public interface ICanInit
    {
        bool IsInitialized { get; set; }
        void Init();
        void DeInit();
    }

    public interface ICanStart
    {
        void Start();
    }

    public abstract class ServiceLocator<T> : IServiceLocator where T : ServiceLocator<T>, new()
    {
        protected static T locator;
        public Events Events { get; } = new Events();

        public static T I
        {
            get
            {
                if (locator != null) return locator;
                throw new Exception($"{typeof(T)} is not initialized");
            }
        }

        protected readonly Dictionary<Type, IService> cache = new Dictionary<Type, IService>();
        protected readonly List<IService> services = new List<IService>();

        public static void Initialize()
        {
            if (locator != null) return;
            locator = new T();
            locator.Init();
            foreach (var service in locator.services)
            {
                service.Start();
            }

            locator.IsInitialized = true;
        }

        public bool IsInitialized { get; set; }

        public virtual void Init()
        {
        }

        public virtual void DeInit()
        {
            foreach (var kvp in cache.Where(kvp => kvp.Value.IsInitialized))
            {
                kvp.Value.DeInit();
            }

            locator = null;
        }

        public void RegisterService<TService>(TService service) where TService : IService
        {
            service.SetLocator(this);
            var type = typeof(TService);
            if (!cache.TryAdd(type, service))
            {
                throw new ServiceAlreadyExistsException(type);
            }

            services.Add(service);
            service.Init();
            service.IsInitialized = true;
            if (!IsInitialized) return;
            service.Start();
        }

        public TService GetService<TService>() where TService : IService
        {
            var type = typeof(TService);
            if (!cache.TryGetValue(type, out var service))
                throw new ServiceNotFoundException(type);
            return (TService)service;
        }

        public bool TryGetService<TService>(out TService service) where TService : IService
        {
            var type = typeof(TService);
            if (!cache.TryGetValue(type, out var s))
                throw new ServiceNotFoundException(type);
            service = (TService)s;
            return true;
        }
    }

    public interface IService : ICanInit, ICanGetLocator, ICanStart, ICanSetLocator
    {
    }

    public interface ICanGetLocator
    {
        IServiceLocator GetLocator();
    }

    public interface ICanGetLocator<T> : ICanGetLocator where T : ServiceLocator<T>, new()
    {
        IServiceLocator ICanGetLocator.GetLocator() => ServiceLocator<T>.I;
    }

    public abstract class AbstractService : IService
    {
        private IServiceLocator locator;
        public bool IsInitialized { get; set; }
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;

        public virtual void Init()
        {
        }

        public virtual void Start()
        {
        }

        public virtual void DeInit()
        {
        }
    }
    
    public class BindableProperty<T>
    {
        private Action<T> onValueChanged;
        private T value;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                onValueChanged?.Invoke(value);
            }
        }

        public BindableProperty(T value = default)
        {
            this.value = value;
        }

        public BindableProperty<T> Register(Action<T> onValueChanged)
        {
            this.onValueChanged += onValueChanged;
            return this;
        }

        public BindableProperty<T> RegisterAndInvoke(Action<T> onValueChanged)
        {
            this.onValueChanged += onValueChanged;
            onValueChanged(value);
            return this;
        }

        public BindableProperty<T> UnRegister(Action<T> onValueChanged)
        {
            this.onValueChanged -= onValueChanged;
            return this;
        }

        public void SetWithoutInvoke(T value)
        {
            this.value = value;
        }
    }

    public static class ServiceExtension
    {
        public static T GetService<T>(this ICanGetLocator self) where T : IService, ICanGetLocator =>
            self.GetLocator().GetService<T>();

        public static bool TryGetService<T>(this ICanGetLocator self, out T service)
            where T : IService, ICanGetLocator =>
            self.GetLocator().TryGetService(out service);
    }
}