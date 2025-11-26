using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RicKit.RFramework
{
    public interface IServiceLocator : ICanInit
    {
        Dictionary<Type, Delegate> Events { get; }
        Dictionary<Type, ICommand> Commands { get; }
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
        public Dictionary<Type, Delegate> Events { get; } = new();
        public Dictionary<Type, ICommand> Commands { get; } = new();

        public static T I
        {
            get
            {
                if (locator != null) return locator;
                throw new Exception($"{typeof(T)} is not initialized");
            }
        }

        protected class Cache : IEnumerable<IService>
        {
            private readonly Dictionary<Type, IService> map = new Dictionary<Type, IService>();
            private readonly List<IService> services = new List<IService>();

            public bool TryAdd<TService>(Type type, TService service) where TService : IService
            {
                if (!map.TryAdd(type, service)) return false;
                services.Add(service);
                return true;
            }

            public bool TryGetValue(Type type, out IService service)
            {
                return map.TryGetValue(type, out service);
            }

            public IEnumerator<IService> GetEnumerator() => services.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        
        protected readonly Cache cache = new Cache();

        public static void Initialize()
        {
            if (locator != null) return;
            locator = new T();
            locator.Init();
            foreach (var service in locator.cache)
            {
                service.Init();
            }
            foreach (var service in locator.cache)
            {
                service.Start();
                service.IsInitialized = true;
            }

            locator.IsInitialized = true;
        }

        public bool IsInitialized { get; set; }

        public virtual void Init()
        {
        }

        public virtual void DeInit()
        {
            foreach (var service in cache.Where(service => service.IsInitialized))
            {
                service.DeInit();
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

            if (!IsInitialized) return;
            service.Init();
            service.Start();
            service.IsInitialized = true;
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
    

    public static class ServiceExtension
    {
        public static T GetService<T>(this ICanGetLocator self) where T : IService, ICanGetLocator =>
            self.GetLocator().GetService<T>();

        public static bool TryGetService<T>(this ICanGetLocator self, out T service)
            where T : IService, ICanGetLocator =>
            self.GetLocator().TryGetService(out service);
    }
    
}