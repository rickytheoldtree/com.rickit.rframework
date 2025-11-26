using System;
using System.Collections.Generic;

namespace RicKit.RFramework
{
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
        
        public void ForceNotify()
        {
            onValueChanged?.Invoke(value);
        }
        
        public void UnRegisterAll()
        {
            onValueChanged = null;
        }
    }

    public static class BindablePropertyExtension
    {
        public static void AddAndNotify<T, T2>(this BindableProperty<T> bindableProperty, T2 item)
            where T : class, ICollection<T2>
        {
            bindableProperty.Value.Add(item);
            bindableProperty.ForceNotify();
        }

        public static void RemoveAndNotify<T, T2>(this BindableProperty<T> bindableProperty, T2 item)
            where T : class, ICollection<T2>
        {
            bindableProperty.Value.Remove(item);
            bindableProperty.ForceNotify();
        }

        public static void ClearAndNotify<T, T2>(this BindableProperty<T> bindableProperty)
            where T : class, ICollection<T2>
        {
            bindableProperty.Value.Clear();
            bindableProperty.ForceNotify();
        }

        public static void UnionWithAndNotify<T, T2>(this BindableProperty<T> bindableProperty, IEnumerable<T2> items)
            where T : HashSet<T2>
        {
            bindableProperty.Value.UnionWith(items);
            bindableProperty.ForceNotify();
        }

        public static void AddRangeAndNotify<T, T2>(this BindableProperty<T> bindableProperty, IEnumerable<T2> items)
            where T : List<T2>
        {
            bindableProperty.Value.AddRange(items);
            bindableProperty.ForceNotify();
        }

        public static void InsertAndNotify<T, T2>(this BindableProperty<T> bindableProperty, int index, T2 item)
            where T : IList<T2>
        {
            bindableProperty.Value.Insert(index, item);
            bindableProperty.ForceNotify();
        }

        public static void RemoveAtAndNotify<T, T2>(this BindableProperty<T> bindableProperty, int index)
            where T : IList<T2>
        {
            bindableProperty.Value.RemoveAt(index);
            bindableProperty.ForceNotify();
        }
    }
}
