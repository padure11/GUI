using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    private bool isMoving = false;
    public Vector3 startPosition;
    private bool isResetting = false;


    void Start()
    {
        startPosition = transform.position;
    }

    // functie pentru update a obiectului player
    void Update()
    {

        if (transform.position.y < -5f && !isResetting)
        {
            StartCoroutine(ResetWithAnimation());
        }

        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.W))
            StartCoroutine(MoveForward(1));
        if (Input.GetKeyDown(KeyCode.S))
            StartCoroutine(MoveBack(1));
        if (Input.GetKeyDown(KeyCode.A))
            StartCoroutine(TurnLeft());
        if (Input.GetKeyDown(KeyCode.D))
            StartCoroutine(TurnRight());
        if (Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(Jump());
    }

    // functia care se excuta ca se apeleaz moveForward(nr pasi)
    public IEnumerator MoveForward(int steps)
    {
        isMoving = true;
        
        for (int i = 0; i < steps; i++)
        {
            // verifică dacă e obstacol în față
            if (Physics.Raycast(transform.position, transform.forward, 2f))
            {
                Debug.LogWarning("Blocked!");
                break; // oprește mișcarea
            }
            
            Vector3 target = transform.position + transform.forward*2f;
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
        }
        
        isMoving = false;
    }

    public IEnumerator MoveBack(int steps)
    {
        isMoving = true;
        
        for (int i = 0; i < steps; i++)
        {
            // verifică dacă e obstacol în spate
            if (Physics.Raycast(transform.position, -transform.forward, 2f))
            {
                Debug.LogWarning("Blocked!");
                break;
            }
            
            Vector3 target = transform.position - transform.forward*2f;
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
        }
        
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
        transform.rotation = targetRotation; // snap exact
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
        
        // detectează blocul din față cu Raycast
        Vector3 forwardPos = transform.position + transform.forward*2f;
        float targetY = transform.position.y; // rămâne la aceeași înălțime default
        
        // verifică dacă e bloc la același nivel sau mai sus
        RaycastHit hit;
        if (Physics.Raycast(forwardPos + Vector3.up * 2, Vector3.down, out hit, 3f))
        {
            targetY = hit.point.y + 0.5f; // se urcă pe suprafața blocului
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

        // micșorează
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        // reset poziție
        transform.position = startPosition;

        // mărește înapoi
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
}