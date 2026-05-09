using System;
using System.Collections.Generic;


public class EventModule
{
    Dictionary<string, IEventInfo> eventInfoDic = new Dictionary<string, IEventInfo>();//事件不用序列化

    //事件信息接口，定义了销毁事件信息的方法
    interface IEventInfo
    {
        void Destory();
    }

    //无参事件信息，包含一个Action类型的字段来存储事件的回调函数，并实现了IEventInfo接口
    class EventInfo : IEventInfo
    {
        public Action action;

        public void Init(Action action)
        {
            this.action = action;
        }

        public void Destory()
        {
            action = null;
        }
    }

    //多参事件信息，使用泛型来支持不同类型的事件参数，包含一个TAction类型的字段来存储事件的回调函数，并实现了IEventInfo接口
    class MultiEventInfo<TAction> : IEventInfo where TAction : MulticastDelegate
    {
        public TAction action;

        public void Init(TAction action)
        {
            this.action = action;
        }

        public void Destory()
        {
            action = null;
        }
    };

    //添加事件监听，支持无参和有参两种事件类型，用泛型区分不同类型的事件参数，并将事件信息存储在字典中
    public void AddEventListener(string eventName, Action action)
    {
        // 有没有对应的事件可以监听
        if (eventInfoDic.ContainsKey(eventName))
        {
            (eventInfoDic[eventName] as EventInfo).action += action;
        }
        // 没有的话，需要新增 到字典中，并添加对应的Action
        else
        {
            EventInfo eventInfo = new EventInfo();
            if (eventInfo == null) eventInfo = new EventInfo();
            eventInfo.Init(action);
            eventInfoDic.Add(eventName, eventInfo);
        }
    }

    public void AddEventListener<TAction>(string eventName, TAction action) where TAction : MulticastDelegate
    {
        // 有没有对应的事件可以监听
        if (eventInfoDic.TryGetValue(eventName, out IEventInfo eventInfo))
        {
            MultiEventInfo<TAction> info = (MultiEventInfo<TAction>)eventInfo;
            info.action = (TAction)Delegate.Combine(info.action, action);
        }
        else AddMultipleParameterEventInfo(eventName, action);
    }

    void AddMultipleParameterEventInfo<TAction>(string eventName, TAction action)
        where TAction : MulticastDelegate
    {
        MultiEventInfo<TAction> newEventInfo = new MultiEventInfo<TAction>();
        if (newEventInfo == null) 
        {
            newEventInfo = new MultiEventInfo<TAction>();
        }
        newEventInfo.Init(action);
        eventInfoDic.Add(eventName, newEventInfo);
    }

    //触发事件的监听
    public void EventTrigger(string eventName)
    {
        if (eventInfoDic.ContainsKey(eventName))
        {
            ((EventInfo)eventInfoDic[eventName]).action?.Invoke();
        }
    }

    public void EventTrigger<T>(string eventName, T arg)
    {
        if (eventInfoDic.TryGetValue(eventName, out IEventInfo eventInfo))
            ((MultiEventInfo<Action<T>>)eventInfo).action?.Invoke(arg);
    }

    //取消事件的监听
    public void RemoveEventListener(string eventName, Action action)
    {
        if (eventInfoDic.TryGetValue(eventName, out IEventInfo eventInfo))
        {
            ((EventInfo)eventInfo).action -= action;
        }
    }

    public void RemoveEventListener<TAction>(string eventName, TAction action) where TAction : MulticastDelegate
    {
        if (eventInfoDic.TryGetValue(eventName, out IEventInfo eventInfo))
        {
            MultiEventInfo<TAction> info = (MultiEventInfo<TAction>)eventInfo;
            info.action = (TAction)Delegate.Remove(info.action, action);
        }
    }

    //取消事件
    public void RemoveEvent(string eventName)
    {
        if (eventInfoDic.Remove(eventName, out IEventInfo eventInfo))
        {
            eventInfo.Destory();
        }
    }

    public void Clear()
    {
        foreach (string eventName in eventInfoDic.Keys)
        {
            eventInfoDic[eventName].Destory();
        }

        eventInfoDic.Clear();
    }

}