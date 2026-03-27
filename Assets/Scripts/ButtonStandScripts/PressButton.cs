using UnityEngine;

public class PressButton : MonoBehaviour
{
    public RoboArm roboArm;

    public void Activate()
    {
        roboArm.PlayAnimation();
    }
}
