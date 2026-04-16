using UnityEngine;

public class FinishZone : MonoBehaviour
{
    public static bool robot1Finished = false;
    public static bool robot2Finished = false;

    public string acceptedTag;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(acceptedTag))
        {
            if (acceptedTag == "Robot1")
                robot1Finished = true;
            else if (acceptedTag == "Robot2")
                robot2Finished = true;

            Debug.Log(acceptedTag + " finished!");

            if (robot1Finished && robot2Finished)
            {
                Debug.Log("Both robots finished!");
                LevelManager.Instance.CompleteLevel();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(acceptedTag))
        {
            if (acceptedTag == "Robot1")
                robot1Finished = false;
            else if (acceptedTag == "Robot2")
                robot2Finished = false;
        }
    }

    public static void ResetFinish()
    {
        robot1Finished = false;
        robot2Finished = false;
    }
}