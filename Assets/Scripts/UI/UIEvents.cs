using UnityEngine;
using UnityEngine.InputSystem;

public class UIEvents : MonoBehaviour
{
    public PointConstraint origin;
    public Transform target;

    public int solverIterations = 10;

    public void SetPositionToOrigin()
    {
        if (origin != null && target != null)
        {
            origin.transform.position = target.position;
        }
    }

    public void ReachTargetWithoutEnd()
    {
        
        for (int i = 0; i < solverIterations; i++)
        {
            origin.transform.position = target.position;
            origin.SolveEntireChainConstraints();
        }
    }

    public void ReachTargetWithEnd()
    {
        for (int i = 0; i < solverIterations; i++)
        {
            if (i < solverIterations / 2 || solverIterations == 1)
            {
                origin.transform.position = target.position;
            }
            origin.SolveEntireChainConstraints();
        }
    }

}
