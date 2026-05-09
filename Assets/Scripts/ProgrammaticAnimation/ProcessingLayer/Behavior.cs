using UnityEngine;

//生物行为类，负责定义生物的各种行为，可以是条件反射（直接从感受器接收信息并触发效应器）或非条件反射（处理层根据感受器信息进行决策后触发效应器）
public class Behavior : MonoBehaviour
{
    public string behaviorName; //行为名称
    public EventModule eventModule = new EventModule();
    public Brain brain;

    void Awake()
    {
        eventModule = new EventModule();
        Init();
    }

    public virtual void Init()
    {
        //注册具体的事件监听和行为逻辑，在子类中实现
    }

    public virtual void OnEnter(){}

    public virtual void OnUpdate(){}

    public virtual void OnExit(){}
}
