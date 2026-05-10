using System.Collections.Generic;
using System;
using UnityEngine;

//处理层，负责接受信息层传来的各种信息并加以处理，并选择效应器触发效果，是生物行为的决策中心
public class Brain : MonoBehaviour
{
    public List<Receptor> receptors = new List<Receptor>();//感受器列表
    public List<Effector> effectors = new List<Effector>();//效应器列表
    public List<PointConstraint> pointConstraints = new List<PointConstraint>();//点约束列表
    public List<LineController> lineControllers = new List<LineController>();//线控制器列表

    public List<Behavior> behaviors = new List<Behavior>();
    public Behavior curBehavior; // 决策中心的行为

    //存储生物状态的字典，可以根据需要添加各种状态信息
    SerializableDic<string, int> intState = new SerializableDic<string, int>();
    SerializableDic<string, float> floatState = new SerializableDic<string, float>();
    SerializableDic<string, string> stringState = new SerializableDic<string, string>();
    SerializableDic<string, bool> boolState = new SerializableDic<string, bool>();

    //编辑器视图中添加状态字典的可视化功能，可以添加int、float、string等类型的状态，并在运行时通过代码访问和修改这些状态
    [Serializable]
    public struct IntStateItem
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct FloatStateItem
    {
        public string key;
        public float value;
    }

    [Serializable]
    public struct StringStateItem
    {
        public string key;
        public string value;
    }

    [Serializable]
    public struct BoolStateItem
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public struct StateEntry
    {
        public List<IntStateItem> intStates;
        public List<FloatStateItem> floatStates;
        public List<StringStateItem> stringStates;
        public List<BoolStateItem> boolStates;
    }

    public StateEntry inspectorState;


    void Start()
    {
        ApplyInspectorState();

        //获取此transform上的点约束组件，分别从target和parents方向递归获取整个骨骼链的点约束、感受器和效应器组件并加入列表
        InitLists(transform);

        //注册行为，若curBehavior不为空则将其加入行为列表，否则将行为列表中的第一个行为设为当前行为
        if (curBehavior != null)
        {
            curBehavior.brain = this;
            if (!behaviors.Contains(curBehavior))
            {
                behaviors.Add(curBehavior);
            }
        }
        else
        {
            curBehavior = behaviors.Count > 0 ? behaviors[0] : null;
        }


    }

    void Update()//统一由决策中心调用更新
    {
        foreach (var receptor in receptors)
        {
            receptor.OnUpdate();
        }
        if (curBehavior != null)
        {
            curBehavior.OnUpdate();
        }
        foreach (var effector in effectors)
        {
            effector.OnUpdate();
        }
        foreach (var pointConstraint in pointConstraints)
        {
            pointConstraint.OnUpdate(true);
        }
        foreach (var lineController in lineControllers)
        {
            lineController.OnUpdate();
        }
    }

    public void ChangeBehavior(Behavior newBehavior)
    {
        if (newBehavior != null && newBehavior != curBehavior)
        {
            if (curBehavior != null)
            {
                curBehavior.OnExit();
            }
            curBehavior = newBehavior;
            curBehavior.OnEnter();
        }
    }

    public void ChangeBehavior(string behaviorName)
    {
        Behavior newBehavior = behaviors.Find(behavior => behavior.behaviorName == behaviorName);
        if (newBehavior != null && newBehavior != curBehavior)
        {
            if (curBehavior != null)
            {
                curBehavior.OnExit();
            }
            curBehavior = newBehavior;
            curBehavior.OnEnter();
        }
    }

    void ApplyInspectorState()
    {

        if (inspectorState.intStates != null)
        {
            foreach (var item in inspectorState.intStates)
            {
                if (string.IsNullOrEmpty(item.key)) continue;
                intState[item.key] = item.value;
            }
        }

        if (inspectorState.floatStates != null)
        {
            foreach (var item in inspectorState.floatStates)
            {
                if (string.IsNullOrEmpty(item.key)) continue;
                floatState[item.key] = item.value;
            }
        }

        if (inspectorState.stringStates != null)
        {
            foreach (var item in inspectorState.stringStates)
            {
                if (string.IsNullOrEmpty(item.key)) continue;
                stringState[item.key] = item.value;
            }
        }

        if (inspectorState.boolStates != null)
        {
            foreach (var item in inspectorState.boolStates)
            {
                if (string.IsNullOrEmpty(item.key)) continue;
                boolState[item.key] = item.value;
            }
        }
    }

    void InitLists(Transform tr)
    {
        if (tr == null) return;
        PointConstraint pointConstraint = tr.GetComponent<PointConstraint>();
        Receptor[] receptor = tr.GetComponents<Receptor>();
        Effector[] effector = tr.GetComponents<Effector>();
        LineController[] lineController = tr.GetComponents<LineController>();
        if (receptor != null){
            foreach (var r in receptor)
            {
                if (!receptors.Contains(r))
                {
                    receptors.Add(r);
                    r.brain = this;
                    r.autoUpdate = false;
                }
            }
        }
        if (effector != null){
            foreach (var e in effector)
            {
                if (!effectors.Contains(e))
                {
                    effectors.Add(e);
                    e.brain = this;
                    e.autoUpdate = false;
                }
            }
        }
        if (lineController != null){
            foreach (var lc in lineController)
            {
                if (!lineControllers.Contains(lc))
                {
                    lineControllers.Add(lc);
                    lc.brain = this;
                    lc.autoUpdate = false;
                }
            }
        }
        if (pointConstraint != null)
        {
            if (!pointConstraints.Contains(pointConstraint))
                pointConstraints.Add(pointConstraint);
                pointConstraint.autoUpdate = false;
                
                foreach (var constraint in pointConstraint.constraints)
                {
                    if (pointConstraints.Contains(constraint.target)) continue; //避免重复添加已存在的约束
                    if (constraint.target != null)
                        InitLists(constraint.target.transform);
                }

                foreach (var parent in pointConstraint.parents)
                {
                    if (pointConstraints.Contains(parent)) continue;
                    if (parent != null)
                        InitLists(parent.transform);
                }
        }
    }

    //注册事件及监听
    public void AddEventListener(string eventName, Action action)
    {
        curBehavior.eventModule.AddEventListener(eventName, action);
    }
    public void AddEventListener<T>(string eventName, Action<T> action)
    {
        curBehavior.eventModule.AddEventListener<Action<T>>(eventName, action);
    }

    //触发事件的监听
    public void EventTrigger(string eventName)
    {
        curBehavior.eventModule.EventTrigger(eventName);
    }
    public void EventTrigger<T>(string eventName, T arg)
    {
        curBehavior.eventModule.EventTrigger<T>(eventName, arg);
    }

    //取消事件的监听
    public void RemoveEventListener(string eventName, Action action)
    {
        curBehavior.eventModule.RemoveEventListener(eventName, action);
    }
    public void RemoveEventListener<T>(string eventName, Action<T> action)
    {
        curBehavior.eventModule.RemoveEventListener(eventName, action);
    }

    //取消事件
    public void RemoveEvent(string eventName)
    {
        curBehavior.eventModule.RemoveEvent(eventName);
    }

    public void Clear()
    {
        curBehavior.eventModule.Clear();
    }

    public Receptor GetReceptor(string receptorName)
    {
        return receptors.Find(receptor => receptor.receptorName == receptorName);
    }

    public Effector GetEffector(string effectorName)
    {
        return effectors.Find(effector => effector.effectorName == effectorName);
    }

    public LineController GetLineController(string lineControllerName)
    {
        return lineControllers.Find(lineController => lineController.controllerName == lineControllerName);
    }

    public void TriggerEffector(string effectorName)
    {
        Effector effector = GetEffector(effectorName);
        if (effector != null)
        {
            effector.TriggerEffect();
        }
    }

    public bool HasKey(string key)
    {
        return intState.ContainsKey(key) || floatState.ContainsKey(key) || stringState.ContainsKey(key) || boolState.ContainsKey(key);
    }

    public int GetIntState(string key)
    {
        if (intState.TryGetValue(key, out int intValue))
        {
            return intValue;
        }
        return default;
    }

    public float GetFloatState(string key)
    {
        if (floatState.TryGetValue(key, out float floatValue))
        {
            return floatValue;
        }
        return default;
    }

    public string GetStringState(string key)
    {
        if (stringState.TryGetValue(key, out string stringValue))
        {
            return stringValue;
        }
        return default;
    }

    public bool GetBoolState(string key)
    {
        if (boolState.TryGetValue(key, out bool boolValue))
        {
            return boolValue;
        }
        return default;
    }

    public void SetIntState(string key, int value)
    {
        if (intState.ContainsKey(key))
        {
            intState[key] = value;
        }
        else
        {
            intState.Add(key, value);
        }
    }

    public void SetFloatState(string key, float value)
    {
        if (floatState.ContainsKey(key))
        {
            floatState[key] = value;
        }
        else
        {
            floatState.Add(key, value);
        }
    }

    public void SetStringState(string key, string value)
    {
        if (stringState.ContainsKey(key))
        {
            stringState[key] = value;
        }
        else
        {
            stringState.Add(key, value);
        }
    }

    public void SetBoolState(string key, bool value)
    {
        if (boolState.ContainsKey(key))
        {
            boolState[key] = value;
        }
        else
        {
            boolState.Add(key, value);
        }
    }
}
