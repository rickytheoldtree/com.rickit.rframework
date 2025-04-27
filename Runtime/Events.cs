using System;
using System.Collections.Generic;

namespace RicKit.RFramework
{
    public class Events
    {
        private readonly Dictionary<string, object> events = new Dictionary<string, object>();

        public void Register<T>(Action<T> action)
        {
            var key = typeof(T).Name;
            if (!events.ContainsKey(key))
            {
                events[key] = new Event<T>();
            }

            ((Event<T>)events[key]).Register(action);
        }

        public void UnRegister<T>(Action<T> action)
        {
            var key = typeof(T).Name;
            if (!events.TryGetValue(key, out var e)) return;
            ((Event<T>)e).UnRegister(action);
        }

        public void Send<T>(T arg = default)
        {
            var key = typeof(T).Name;
            if (!events.TryGetValue(key, out var e)) return;
            ((Event<T>)e).Invoke(arg);
        }
    }

    public class Event<T>
    {
        private Action<T> action = delegate { };

        public void Register(Action<T> action)
        {
            this.action += action;
        }

        public void UnRegister(Action<T> action)
        {
            this.action -= action;
        }

        public void Invoke(T arg)
        {
            action(arg);
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