using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private bool isMoving = false;
    private bool isResetting = false;

    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {

        if (transform.position.y < -5f && !isResetting)
        {
            StartCoroutine(ResetWithAnimation());
        }

        if (isMoving) return;
    }

 public IEnumerator MoveForward() {
        Debug.Log("isMoving = " + isMoving);
        isMoving = true;
        Debug.Log("isMoving = " + isMoving);

        if(Physics.Raycast(transform.position, transform.forward, 2f))
        {
            Debug.LogWarning("Blocked!");
            isMoving = false;
            yield break;
        }
        
        Debug.Log("Player moving forward");

        Vector3 targetPosition = transform.position + transform.forward * 2f;

        while (targetPosition != transform.position) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;

        Debug.Log("isMoving = " + isMoving);
        isMoving = false;
        Debug.Log("isMoving = " + isMoving);
        Debug.Log("Player pos: " + transform.position);

        Debug.Log("MOVE FORWARD");
    }

    public IEnumerator MoveBack() {
        Debug.Log("isMoving = " + isMoving);
        isMoving = true;
        Debug.Log("isMoving = " + isMoving);

        if(Physics.Raycast(transform.position, -transform.forward, 2f))
        {
            Debug.LogWarning("Blocked!");
            isMoving = false;
            yield break;
        }

        Debug.Log("Player moving back");

        Vector3 targetPosition = transform.position - transform.forward * 2f;

        while (targetPosition != transform.position) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        
        Debug.Log("isMoving = " + isMoving);
        isMoving = false;
        Debug.Log("isMoving = " + isMoving);
        Debug.Log("Player pos: " + transform.position);

        Debug.Log("MOVE BACKWARD");
    }

    public IEnumerator TurnLeft() {
        Debug.Log("isMoving = " + isMoving);
        isMoving = true;
        Debug.Log("isMoving = " + isMoving);

        Debug.Log("Player turning left");

        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, -90f, 0);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotateSpeed * 90f * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        Debug.Log("isMoving = " + isMoving);
        isMoving = false;
        Debug.Log("isMoving = " + isMoving);
        Debug.Log("Player rot: " + transform.rotation.eulerAngles);

        Debug.Log("TURNED LEFT");
    }

    public IEnumerator TurnRight() {
        Debug.Log("isMoving = " + isMoving);
        isMoving = true;
        Debug.Log("isMoving = " + isMoving);

        Debug.Log("Player turning right");

        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 90f, 0);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotateSpeed * 90f * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        Debug.Log("isMoving = " + isMoving);
        isMoving = false;
        Debug.Log("isMoving = " + isMoving);
        Debug.Log("Player rot: " + transform.rotation.eulerAngles);

        Debug.Log("TURNED RIGHT");
    }

    public IEnumerator Jump() {
        Debug.Log("isMoving = " + isMoving);
        isMoving = true;
        Debug.Log("isMoving = " + isMoving);

        Vector3 forwardTargetPosition = transform.position + transform.forward * 2f;
        Vector3 targetPosition = transform.position;

        RaycastHit hit;
        if (Physics.Raycast(forwardTargetPosition + Vector3.up * 2f * 2, Vector3.down, out hit, 2f * 2)) {
            targetPosition = new Vector3(forwardTargetPosition.x, Mathf.Round(hit.point.y + 1f), forwardTargetPosition.z);
            Debug.Log("Jump on to: " + targetPosition);
        }
        else {
            Vector3 gap = transform.position + transform.forward * 2f;

            if (Physics.Raycast(gap + Vector3.up * 4f, Vector3.down, out hit, 4f)) {
                targetPosition = new Vector3(gap.x, Mathf.Round(hit.point.y + 1f), gap.z);
                Debug.Log("Jump over to: " + targetPosition);
            }
            else {
                Debug.LogWarning("Can't jump");
                Debug.Log("isMoving = " + isMoving);
                isMoving = false;
                Debug.Log("isMoving = " + isMoving);
                Debug.Log("Player pos: " + transform.position);
                yield break;
            }
        }

        transform.position = targetPosition;

        Debug.Log("isMoving = " + isMoving);
        isMoving = false;
        Debug.Log("isMoving = " + isMoving);
        Debug.Log("Player pos: " + transform.position);


        Debug.Log("Jumped");
    }
    
    IEnumerator ResetWithAnimation()
    {
        isResetting = true;
        isMoving = true;
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 originalScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        transform.position = startPosition;

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        isResetting = false;
        isMoving = false;
    }

    public IEnumerator Push()
    {
        isMoving = true;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 2f))
        {
            // Verifică dacă obiectul e pushable
            if (hit.collider.CompareTag("PushableCrate"))
            {
                Vector3 pushDirection = transform.forward;
                Vector3 objectPos = hit.transform.position;
                Vector3 targetPos = objectPos + pushDirection * 2f;
                
                // Verifică dacă e loc în spatele obiectului
                if (!Physics.Raycast(objectPos, pushDirection, 2f))
                {
                    // Verifică dacă e podea sub poziția țintă
                    if (Physics.Raycast(targetPos + Vector3.up, Vector3.down, 3f))
                    {
                        // Mută obiectul smooth
                        Transform obj = hit.transform;
                        Vector3 startPos = obj.position;
                        float elapsed = 0f;
                        float duration = 0.5f;
                        
                        while (elapsed < duration)
                        {
                            elapsed += Time.deltaTime;
                            float t = elapsed / duration;
                            obj.position = Vector3.Lerp(startPos, targetPos, t);
                            yield return null;
                        }
                        obj.position = targetPos;

                        Debug.Log("Object pushed");
                    }
                    else
                    {
                        Debug.Log("No floor behind object!");
                    }
                }
                else
                {
                    Debug.Log("No space to push!");
                }
            }
            else
            {
                Debug.Log("Object is not pushable!");
            }
        }
        else
        {
            Debug.Log("Nothing to push!");
        }
        
        yield return new WaitForSeconds(0.1f);
        isMoving = false;

        Debug.Log("Pushed");
    }

    public IEnumerator Press() {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, 2f))
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name);

            PressButton button = hit.collider.GetComponent<PressButton>();
            if (button != null)
            {
                button.Activate();
                yield return new WaitForSeconds(1.5f);
            }
        }

        yield return null;

        Debug.Log("Pressed");
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}