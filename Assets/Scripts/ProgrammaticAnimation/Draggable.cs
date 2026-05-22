using UnityEngine;
using UnityEngine.InputSystem;

//长按游戏视图挂载此脚本的物体进行拖动，将物体的位置每帧逼近鼠标位置，距离越近移动越慢，形成拖动效果。可以与PointConstraint等约束组件配合使用，形成更复杂的动画效果。
public class Draggable : MonoBehaviour
{
    public bool isDraggable = true; // 是否可拖动
    public float dragSpeed = 10f; // 拖动速度，数值越大拖动越快

    private bool isDragging = false; // 是否正在拖动
    private Vector3 offset; // 鼠标与物体中心的偏移
    private Camera mainCamera;
    private Collider2D targetCollider2D;

    void Awake()
    {
        mainCamera = Camera.main;
        targetCollider2D = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (!isDraggable || mainCamera == null || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) // 鼠标左键按下
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (targetCollider2D != null && targetCollider2D.OverlapPoint(mouseWorldPos))
            {
                isDragging = true;
                offset = transform.position - mouseWorldPos; // 计算偏移
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame) // 鼠标左键抬起
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 targetPosition = GetMouseWorldPosition() + offset;
            targetPosition.z = transform.position.z;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed); // 平滑移动
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePosition = Mouse.current.position.ReadValue();
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = transform.position.z;
        return worldPosition;
    }
}
