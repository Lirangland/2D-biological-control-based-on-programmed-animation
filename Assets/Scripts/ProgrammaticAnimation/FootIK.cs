using Unity.VisualScripting;
using UnityEngine;

//用于2D俯视角节肢动物脚步控制，绑定在骨骼上，向左右发射可以自定义长度的射线，检测射线尽头和左右脚的距离（不需要打在物体上），超出一定阈值后设置左右脚到当前位置
public class FootIK : MonoBehaviour
{
    public float rayLength = 1f; // 射线长度
    public float threshold = 0.1f; // 距离阈值
    public Transform leftFoot; // 左脚骨骼
    public Transform rightFoot; // 右脚骨骼

    void Update()
    {
        if (leftFoot != null){
            RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, -transform.right, rayLength, ~LayerMask.GetMask("Creature"));//忽略生物层碰撞
            float leftDistance = hitLeft.collider != null ? hitLeft.distance : rayLength;
            if (Vector2.Distance(transform.position + -transform.right * leftDistance, leftFoot.position) > threshold)
             {
                 leftFoot.position = transform.position + -transform.right * leftDistance;
             }
        }
        if (rightFoot != null){
            RaycastHit2D hitRight = Physics2D.Raycast(transform.position, transform.right, rayLength, ~LayerMask.GetMask("Creature"));
            float rightDistance = hitRight.collider != null ? hitRight.distance : rayLength;
            if (Vector2.Distance(transform.position + transform.right * rightDistance, rightFoot.position) > threshold)
             {
                 rightFoot.position = transform.position + transform.right * rightDistance;
             }
        }
    }
}
