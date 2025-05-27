using System;
using System.Collections.Generic;

namespace RicKit.RFramework
{
    public static class EventExtension
    {
        public static void RegisterEvent<T>(this ICanGetLocator self, Action<T> action) =>
            self.GetLocator().RegisterEvent(action);

        public static void UnRegisterEvent<T>(this ICanGetLocator self, Action<T> action) =>
            self.GetLocator().UnRegisterEvent(action);

        public static void SendEvent<T>(this ICanGetLocator self, T arg = default) =>
            self.GetLocator().SendEvent(arg);

        public static void RegisterEvent<T>(this IServiceLocator self, Action<T> action)
        {
            var type = typeof(T);
            if (self.Events.TryGetValue(type, out var existing))
                self.Events[type] = Delegate.Combine(existing, action);
            else
                self.Events[type] = action;
        }

        public static void UnRegisterEvent<T>(this IServiceLocator self, Action<T> action)
        {
            var type = typeof(T);
            if (!self.Events.TryGetValue(type, out var existing)) return;

            var updated = Delegate.Remove(existing, action);
            if (updated == null)
                self.Events.Remove(type);
            else
                self.Events[type] = updated;
        }

        public static void SendEvent<T>(this IServiceLocator self, T arg = default)
        {
            var type = typeof(T);
            if (self.Events.TryGetValue(type, out var d))
                (d as Action<T>)?.Invoke(arg);
        }
    }
}