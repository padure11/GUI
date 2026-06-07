using UnityEngine;

public class LevelResettable : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool initialActive;

    void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        initialActive = gameObject.activeSelf;
    }

    public void ResetState()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        if (gameObject.activeSelf != initialActive)
            gameObject.SetActive(initialActive);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var animator = GetComponent<Animator>();
        if (animator != null)
            animator.Rebind();
    }
}
