using UnityEngine;
using UnityEngine.InputSystem;

//长按游戏视图挂载此脚本的物体进行拖动，将物体的位置每帧逼近鼠标位置，距离越近移动越慢，形成拖动效果。可以与PointConstraint等约束组件配合使用，形成更复杂的动画效果。
public class Draggable : MonoBehaviour
{
    public bool isDraggable = true; // 是否可拖动
    public float dragSpeed = 10f; // 拖动速度，数值越大拖动越快

    private bool isDragging = false; // 是否正在拖动
    private Vector3 offset; // 鼠标与物体中心的偏移

    void Update()
    {
        if (!isDraggable) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) // 鼠标左键按下
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && hit.transform == transform)
            {
                isDragging = true;
                offset = transform.position - hit.point; // 计算偏移
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame) // 鼠标左键抬起
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane plane = new Plane(Vector3.forward, transform.position); // 定义一个水平面
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                Vector3 targetPosition = ray.GetPoint(distance) + offset; // 计算目标位置
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed); // 平滑移动
            }
        }
    }
}
