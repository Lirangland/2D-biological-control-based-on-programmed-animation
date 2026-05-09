using UnityEngine;

//效应器基类，负责根据处理层的决策触发效果，可以是改变骨骼位置、播放动画、生成粒子效果等，绑定在生物根节点上
public class Effector : MonoBehaviour
{
    public bool autoUpdate = true; //是否自动更新
    public string effectorName; // 效应器名称，便于在编辑器中识别和管理
    public Brain brain; // 效应器所在生物的决策中心引用
    public virtual void TriggerEffect(){}

    public virtual void OnUpdate(){}

    void Update()
    {
        if (!autoUpdate || brain == null) return;
            OnUpdate();
    }
}
