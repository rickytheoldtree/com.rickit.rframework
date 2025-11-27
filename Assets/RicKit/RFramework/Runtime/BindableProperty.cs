using System;
using System.Collections;
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

    public class BindableCollection<T, T2> : BindableProperty<T>, ICollection<T2> where T : ICollection<T2>, new()
    {
        public BindableCollection() => Value = new T();

        public IEnumerator<T2> GetEnumerator() => Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T2 item)
        {
            Value.Add(item);
            ForceNotify();
        }

        public void Clear()
        {
            Value.Clear();
            ForceNotify();
        }

        public bool Contains(T2 item) => Value.Contains(item);

        public void CopyTo(T2[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);

        public bool Remove(T2 item)
        {
            var result = Value.Remove(item);
            ForceNotify();
            return result;
        }

        public int Count => Value.Count;
        public bool IsReadOnly => Value.IsReadOnly;
    }

    public class BindableDictionary<T, TKey, TValue> : BindableCollection<T, KeyValuePair<TKey, TValue>>,
        IDictionary<TKey, TValue> where T : class, IDictionary<TKey, TValue>, new()
    {
        public void Add(TKey key, TValue value)
        {
            Value.Add(key, value);
            ForceNotify();
        }

        public bool ContainsKey(TKey key) => Value.ContainsKey(key);

        public bool Remove(TKey key)
        {
            var result = Value.Remove(key);
            ForceNotify();
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value) => Value.TryGetValue(key, out value);

        public TValue this[TKey key]
        {
            get => Value[key];
            set
            {
                Value[key] = value;
                ForceNotify();
            }
        }

        public ICollection<TKey> Keys => Value.Keys;
        public ICollection<TValue> Values => Value.Values;
    }

    public class BindableList<T, T2> : BindableCollection<T, T2>, IList<T2> where T : class, IList<T2>, new()
    {
        public int IndexOf(T2 item) => Value.IndexOf(item);

        public void Insert(int index, T2 item)
        {
            Value.Insert(index, item);
            ForceNotify();
        }

        public void RemoveAt(int index)
        {
            Value.RemoveAt(index);
            ForceNotify();
        }

        public T2 this[int index]
        {
            get => Value[index];
            set
            {
                Value[index] = value;
                ForceNotify();
            }
        }
    }

    public static class BindablePropertyExtensions
    {
        public static void UnionWith<T, T2>(this BindableCollection<T, T2> bindableSet, IEnumerable<T2> other)
            where T : ISet<T2>, new()
        {
            bindableSet.Value.UnionWith(other);
            bindableSet.ForceNotify();
        }

        public static void AddRange<T, T2>(this BindableCollection<T, T2> bindableCollection, IEnumerable<T2> items)
            where T : List<T2>, new()
        {
            bindableCollection.Value.AddRange(items);
            bindableCollection.ForceNotify();
        }
    }
}