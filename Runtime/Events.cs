using System;
using System.Collections.Generic;

namespace RicKit.RFramework
{
    public class Events
    {
        private readonly Dictionary<Type, Delegate> events = new();

        public void Register<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (events.TryGetValue(type, out var existing))
                events[type] = Delegate.Combine(existing, callback);
            else
                events[type] = callback;
        }

        public void UnRegister<T>(Action<T> callback)
        {
            var type = typeof(T);
            if (!events.TryGetValue(type, out var existing)) return;

            var updated = Delegate.Remove(existing, callback);
            if (updated == null)
                events.Remove(type);
            else
                events[type] = updated;
        }

        public void Send<T>(T arg = default)
        {
            var type = typeof(T);
            if (events.TryGetValue(type, out var d))
                (d as Action<T>)?.Invoke(arg);
        }
    }

    public static class EventExtension
    {
        public static void RegisterEvent<T>(this ICanGetLocator self, Action<T> action) =>
            self.GetLocator().Events.Register(action);

        public static void UnRegisterEvent<T>(this ICanGetLocator self, Action<T> action) =>
            self.GetLocator().Events.UnRegister(action);

        public static void SendEvent<T>(this ICanGetLocator self, T arg = default) =>
            self.GetLocator().Events.Send(arg);

        public static void RegisterEvent<T>(this IServiceLocator self, Action<T> action) =>
            self.Events.Register(action);

        public static void UnRegisterEvent<T>(this IServiceLocator self, Action<T> action) =>
            self.Events.UnRegister(action);

        public static void SendEvent<T>(this IServiceLocator self, T arg = default) =>
            self.Events.Send(arg);
    }
}