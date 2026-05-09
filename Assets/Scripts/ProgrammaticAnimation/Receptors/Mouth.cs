using UnityEngine;

//口器类，与带有Food组件的物体接触时向处理层发送事件，包含食物信息
public class Mouth : Receptor
{
    void OnCollisionEnter2D(Collision2D other) {
        Food food = other.collider.GetComponent<Food>();
        if (food != null)
        {
            brain.EventTrigger(EventName.TouchFood, food);
        }
    }
}
