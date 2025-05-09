﻿#if USING_UNITASK
using System;
using Cysharp.Threading.Tasks;

namespace RicKit.RFramework
{
    public interface ICanInitAsync
    {
        UniTask InitAsync(IProgress<float> progress);
    }

    public interface ICanStartAsync
    {
        UniTask StartAsync(IProgress<float> progress);
    }

    public abstract class AsyncServiceLocator<T> : ServiceLocator<T> where T : AsyncServiceLocator<T>, ICanInitAsync, new()
    {
        public static async UniTask InitializeAsync(IProgress<float> progress)
        {
            if (locator != null) return;
            locator = new T();
            await locator.InitAsync(progress);
            foreach (var service in locator.cache)
            {
                if (service is ICanStartAsync startAsync)
                    await startAsync.StartAsync(progress);
                else
                    service.Start();
            }
            locator.IsInitialized = true;
        }
        
        public async UniTask RegisterServiceAsync<TService>(TService service, IProgress<float> progress) where TService : IService, ICanInitAsync
        {
            service.SetLocator(this);
            var type = typeof(TService);
            if (!cache.TryAdd(type, service))
                throw new ServiceAlreadyExistsException(type);
            if (service is ICanInitAsync initAsync)
                await initAsync.InitAsync(progress);
            service.IsInitialized = true;
            if (!IsInitialized) return;
            if (service is ICanStartAsync startAsync)
                await startAsync.StartAsync(progress);
            else
                service.Start();
        }
    }
}
#endif