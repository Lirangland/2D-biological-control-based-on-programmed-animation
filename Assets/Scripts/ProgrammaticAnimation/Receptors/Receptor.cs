using UnityEngine;

//感受器基类，负责接收环境传来的各种信息并加以处理，发送给决策中心（条件反射）或效应器（非条件反射），可以是检测环境、感知其他生物、监测自身状态等，绑定在任意节点上
public class Receptor : MonoBehaviour
{
    public bool autoUpdate = true; //是否自动更新
    public string receptorName; // 感受器名称，便于在编辑器中识别和管理
    public Brain brain; // 感受器所在生物的决策中心引用

    void Update()
    {
        if (!autoUpdate || brain == null) return;
        OnUpdate();
    }

    public virtual void OnUpdate(){}

}
