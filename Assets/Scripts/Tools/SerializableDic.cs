using System;
using System.Collections.Generic;
using UnityEngine;
//可序列化的字典类，方便在编辑器中使用
[System.Serializable]
public class SerializableDic<Tkey, TValue>: Dictionary<Tkey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    List<Tkey> keys = new List<Tkey>();

    [SerializeField]
    List<TValue> values = new List<TValue>();

    //保存数据到序列化列表中
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    //从序列化列表中恢复数据到字典中
    public void OnAfterDeserialize()
    {
        this.Clear();
        for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
        {
            this[keys[i]] = values[i];
        }
        keys.Clear();
        values.Clear();
    }
}

