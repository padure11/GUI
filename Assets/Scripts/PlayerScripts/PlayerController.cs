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

        // if (Input.GetKeyDown(KeyCode.W))
        //     StartCoroutine(MoveForward(1));
        // if (Input.GetKeyDown(KeyCode.S))
        //     StartCoroutine(MoveBack(1));
        // if (Input.GetKeyDown(KeyCode.A))
        //     StartCoroutine(TurnLeft());
        // if (Input.GetKeyDown(KeyCode.D))
        //     StartCoroutine(TurnRight());
        // if (Input.GetKeyDown(KeyCode.Space))
        //     StartCoroutine(Jump());
    }

    public IEnumerator MoveForward()
    {
        isMoving = true;
        Debug.Log("Moving true");
        
        if (Physics.Raycast(transform.position, transform.forward, 2f))
        {
            Debug.LogWarning("Blocked!");
        }
        
        Debug.Log("Player moving forward");
        Vector3 target = transform.position + transform.forward*2f;

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;

        isMoving = false;
        Debug.Log("Moving false");
    }

    public IEnumerator MoveBack()
    {
        isMoving = true;

        if (Physics.Raycast(transform.position, -transform.forward, 2f))
        {
            Debug.LogWarning("Blocked!");
        }
        
        Vector3 target = transform.position - transform.forward*2f;
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        
        isMoving = false;
    }

    public IEnumerator TurnLeft()
    {
        isMoving = true;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, -90f, 0);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotateSpeed * 90f * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRotation;
        isMoving = false;
    }

    public IEnumerator TurnRight()
    {
        isMoving = true;
        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 90f, 0);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotateSpeed * 90f * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetRotation;
        isMoving = false;
    }

    public IEnumerator Jump()
    {
        isMoving = true;
        
        Vector3 forwardPos = transform.position + transform.forward*2f;
        float targetY = transform.position.y;
        
        RaycastHit hit;
        if (Physics.Raycast(forwardPos + Vector3.up * 2, Vector3.down, out hit, 3f))
        {
            targetY = hit.point.y + 0.5f;
        }
        
        Vector3 targetPos = new Vector3(forwardPos.x, targetY, forwardPos.z);
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        transform.position = targetPos;
        isMoving = false;
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
    }
}