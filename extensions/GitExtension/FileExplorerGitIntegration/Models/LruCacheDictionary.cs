// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace FileExplorerGitIntegration.Models;

// A simple LRU cache dictionary implementation.
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "File names for generics are ugly.")]
internal sealed class LruCacheDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private const int DefaultCapacity = 512;
    private readonly object _lock = new();
    private readonly Dictionary<TKey, LinkedListNode<(TKey, TValue)>> _dict;
    private readonly LinkedList<(TKey, TValue)> _lruList = new();

    public LruCacheDictionary(int capacity = DefaultCapacity)
    {
        _capacity = capacity;
        _dict = [];
    }

    public LruCacheDictionary(IEqualityComparer<TKey> comparer, int capacity = DefaultCapacity)
    {
        _capacity = capacity;
        _dict = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(comparer);
    }

    public int Count => _dict.Count;

    public bool IsEmpty => Count == 0;

    public IEqualityComparer<TKey> Comparer => _dict.Comparer;

    public void Clear()
    {
        lock (_lock)
        {
            _dict.Clear();
            _lruList.Clear();
        }
    }

    public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                return updateValueFactory(key, node.Value.Item2);
            }

            var value = addValueFactory(key);
            AddAndTrimNoLock(key, value);
            return value;
        }
    }

    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                return updateValueFactory(key, node.Value.Item2);
            }

            AddAndTrimNoLock(key, addValue);
            return addValue;
        }
    }

    public bool ContainsKey(TKey key)
    {
        lock (_lock)
        {
            return _dict.ContainsKey(key);
        }
    }

    public bool TryAdd(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                return false;
            }

            AddAndTrimNoLock(key, value);
            return true;
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                value = node.Value.Item2;
                return true;
            }

            value = default!;
            return false;
        }
    }

    public TValue GetOrAdd(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                return node.Value.Item2;
            }

            AddAndTrimNoLock(key, value);
            return value;
        }
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                return node.Value.Item2;
            }

            var value = valueFactory(key);
            AddAndTrimNoLock(key, value);
            return value;
        }
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _dict.Remove(key);
                value = node.Value.Item2;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private void AddAndTrimNoLock(TKey key, TValue value)
    {
        var newNode = new LinkedListNode<(TKey, TValue)>((key, value));
        AddNewNodeNoLock(newNode);
        TrimToCapacityNoLock();
    }

    private void RenewExistingNodeNoLock(LinkedListNode<(TKey, TValue)> node)
    {
        Debug.Assert(node.List == _lruList, "Node is not in the list");
        _lruList.Remove(node);
        _lruList.AddLast(node);
    }

    private void AddNewNodeNoLock(LinkedListNode<(TKey, TValue)> node)
    {
        Debug.Assert(node.List == null, "Node is already in the list");
        _lruList.AddLast(node);
        _dict.Add(node.Value.Item1, node);
    }

    private void TrimToCapacityNoLock()
    {
        if (_lruList.Count > _capacity)
        {
            RemoveFirstNoLock();
        }
    }

    private void RemoveFirstNoLock()
    {
        var node = _lruList.First;
        if (node != null)
        {
            _lruList.RemoveFirst();
            _dict.Remove(node.Value.Item1);
        }
    }
}
