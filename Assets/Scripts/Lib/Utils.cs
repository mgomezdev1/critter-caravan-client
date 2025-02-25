using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class GenericUtils
    {
        public static T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) <= 0 ? val1 : val2;
        }
        public static T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            return val1.CompareTo(val2) >= 0 ? val1 : val2;
        }

        public static K KvpKey<K, V>(KeyValuePair<K, V> kvp) { return kvp.Key; }
        public static V KvpValue<K, V>(KeyValuePair<K, V> kvp) { return kvp.Value; }

        public static K KvpKey<K, V>(SerializableKeyValuePair<K, V> kvp) { return kvp.Key; }
        public static V KvpValue<K, V>(SerializableKeyValuePair<K, V> kvp) { return kvp.Value; }

        public static F First<F, S>(Tuple<F, S> tup) { return tup.Item1; }
        public static S Second<F, S>(Tuple<F, S> tup) { return tup.Item2; }

        public static F First<F, S, T>(Tuple<F, S, T> tup) { return tup.Item1; }
        public static S Second<F, S, T>(Tuple<F, S, T> tup) { return tup.Item2; }
        public static T Third<F, S, T>(Tuple<F, S, T> tup) { return tup.Item3; }
    }

    [Serializable]
    public class SerializableKeyValuePair<K, V>
    {
        [SerializeField] private K key;
        [SerializeField] private V value;
        public SerializableKeyValuePair(K key, V value)
        {
            this.Key = key;
            this.Value = value;
        }

        public V Value { get => value; set => this.value = value; }
        public K Key { get => key; set => key = value; }
    }
}