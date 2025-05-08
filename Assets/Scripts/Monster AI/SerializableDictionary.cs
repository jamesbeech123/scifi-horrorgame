using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A dictionary that can be serialized by Unity
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary</typeparam>
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    /// <summary>
    /// Save the dictionary to lists
    /// </summary>
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    /// <summary>
    /// Load the dictionary from lists
    /// </summary>
    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count)
        {
            Debug.LogError($"Error deserializing SerializableDictionary. {keys.Count} keys and {values.Count} values found.");
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            this.Add(keys[i], values[i]);
        }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public SerializableDictionary() : base() { }

    /// <summary>
    /// Constructor that accepts a dictionary
    /// </summary>
    public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }

    /// <summary>
    /// Constructor that accepts a comparer
    /// </summary>
    public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

    /// <summary>
    /// Constructor that accepts a capacity
    /// </summary>
    public SerializableDictionary(int capacity) : base(capacity) { }

    /// <summary>
    /// Constructor that accepts a capacity and comparer
    /// </summary>
    public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
}