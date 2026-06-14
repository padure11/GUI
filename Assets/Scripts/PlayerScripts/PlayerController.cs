using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    private bool isMoving = false;
    private bool isResetting = false;
    private bool isDeadThisRun = false;
    private bool isDying = false;

    [HideInInspector] public int playerIndex = -1;

    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public Vector3 startPosition;
    public Quaternion startRotation;

    [HideInInspector] public Portal pendingPortal;
    [HideInInspector] public float portalCooldownUntil;

    public string PlayerLabel()
    {
        if (playerIndex == 0) return "<color=cyan>[P1]</color>";
        if (playerIndex == 1) return "<color=orange>[P2]</color>";
        return "<color=grey>[P?]</color>";
    }

    [Header("Animation")]
    public Animator animator;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int ClimbHash = Animator.StringToHash("Climb");
    private static readonly int PushHash = Animator.StringToHash("Push");
    private static readonly int PressHash = Animator.StringToHash("Press");
    private static readonly int DieHash = Animator.StringToHash("Die");

    [Header("Boost Jump")]
    public float jumpDuration = 1.0f;
    [Range(0f, 0.6f)] public float jumpWindUp = 0.25f;
    public float jumpArcHeight = 1.2f;
    public float minClearance = 0.8f;
    public ParticleSystem[] jetFlames;

    [Header("Push")]
    public float pushDuration = 1.0f;
    public float crateLaunchDelay = 0.4f;   // după câte secunde de la 'push' pleacă cutia

    [Header("Climb Detection")]
    public float minClimbHeight = 0.5f;
    public float maxClimbHeight = 3f;

    [Header("Death")]
    public GameObject deathEffectPrefab;
    public float deathDuration = 1.2f;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool(IsMovingHash, moving);
    }

    private void TriggerAnim(int hash)
    {
        if (animator != null) animator.SetTrigger(hash);
    }

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        if (NetworkManager.Singleton != null && !IsServer) return;

        if (transform.position.y < -5f && !isResetting)
        {
            StartCoroutine(ResetWithAnimation());
        }
    }

 public IEnumerator MoveForward() {
        if (isDeadThisRun) yield break;
        isMoving = true;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.25f;
        if(Physics.Raycast(rayOrigin, transform.forward, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning($"{PlayerLabel()} Blocked!");
            isMoving = false;
            yield break;
        }

        SetMoving(true);
        Vector3 targetPosition = transform.position + transform.forward * 2f;

        while (targetPosition != transform.position) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        SetMoving(false);
        yield return CheckPortal();
        isMoving = false;
    }

    public IEnumerator MoveBack() {
        if (isDeadThisRun) yield break;
        isMoving = true;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.25f;
        if(Physics.Raycast(rayOrigin, -transform.forward, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.LogWarning($"{PlayerLabel()} Blocked!");
            isMoving = false;
            yield break;
        }

        SetMoving(true);
        Vector3 targetPosition = transform.position - transform.forward * 2f;

        while (targetPosition != transform.position) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        SetMoving(false);
        yield return CheckPortal();
        isMoving = false;
    }

    public IEnumerator TurnLeft() {
        if (isDeadThisRun) yield break;
        isMoving = true;

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
        if (isDeadThisRun) yield break;
        isMoving = true;

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
        if (isDeadThisRun) yield break;
        isMoving = true;

        Vector3 forward = transform.forward * 2f;
        Vector3 directlyAhead = transform.position + forward;
        RaycastHit hit;
        Vector3 target = Vector3.zero;
        bool foundTarget = false;

        // CASE 1: block higher directly ahead → land on top of it
        Vector3 climbOrigin = directlyAhead + Vector3.up * (maxClimbHeight + 2f);
        if (Physics.Raycast(climbOrigin, Vector3.down, out hit, maxClimbHeight + 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            float deltaY = hit.point.y - transform.position.y;
            if (deltaY > minClimbHeight && deltaY < maxClimbHeight)
            {
                target = new Vector3(directlyAhead.x, hit.point.y, directlyAhead.z);
                foundTarget = true;
            }
        }

        // CASE 2: gap directly ahead, floor at same level 2 tiles away
        if (!foundTarget)
        {
            bool tileAheadIsGap = !Physics.Raycast(directlyAhead + Vector3.up * 0.5f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            if (tileAheadIsGap)
            {
                Vector3 jumpDest = transform.position + forward * 2f;
                if (Physics.Raycast(jumpDest + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    float landDeltaY = Mathf.Abs(hit.point.y - transform.position.y);
                    if (landDeltaY < 0.5f)
                    {
                        target = new Vector3(jumpDest.x, transform.position.y, jumpDest.z);
                        foundTarget = true;
                    }
                }
            }
        }

        if (!foundTarget)
        {
            Debug.LogWarning($"{PlayerLabel()} Can't jump (no climbable block, no jumpable gap)");
            isMoving = false;
            yield break;
        }

        yield return BoostJump(target);
        yield return CheckPortal();
        isMoving = false;
    }

    private IEnumerator BoostJump(Vector3 target)
    {
        TriggerAnim(JumpHash);
        yield return new WaitForSeconds(jumpDuration * jumpWindUp);

        SetFlames(true);

        Vector3 start = transform.position;
        float deltaY = target.y - start.y;
        float arcHeight = Mathf.Max(jumpArcHeight, deltaY + minClearance);

        float moveTime = jumpDuration * (1f - jumpWindUp);
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            Vector3 pos = Vector3.Lerp(start, target, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = pos;
            yield return null;
        }
        transform.position = target;

        SetFlames(false);
    }

    private void SetFlames(bool on)
    {
        if (jetFlames == null) return;
        foreach (var ps in jetFlames)
        {
            if (ps == null) continue;
            if (on) ps.Play();
            else ps.Stop();
        }
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

    private IEnumerator CheckPortal()
    {
        if (pendingPortal == null) yield break;
        Portal portal = pendingPortal;
        pendingPortal = null;
        yield return StartCoroutine(DoTeleport(portal));
    }

    private IEnumerator DoTeleport(Portal portal)
    {
        if (portal == null || portal.otherPortal == null) yield break;

        Debug.Log($"{PlayerLabel()} <color=#9b59ff>Teleporting</color> via portal");

        Vector3 originalScale = Vector3.one;
        Vector3 exitPos = portal.GetExitPosition();

        // anti-loop: ignore portals until teleport sequence + buffer is over
        portalCooldownUntil = Time.time + portal.warpOutDuration + portal.warpInDuration + 0.5f;

        // warp out
        float elapsed = 0f;
        while (elapsed < portal.warpOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / portal.warpOutDuration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }
        transform.localScale = Vector3.zero;

        // teleport
        transform.position = exitPos;

        // warp in
        elapsed = 0f;
        while (elapsed < portal.warpInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / portal.warpInDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    public IEnumerator Push()
    {
        if (isDeadThisRun) yield break;
        isMoving = true;
        float startTime = Time.time;
        TriggerAnim(PushHash);

        yield return new WaitForSeconds(crateLaunchDelay);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            // Verifică dacă obiectul e pushable
            if (hit.collider.CompareTag("PushableCrate"))
            {
                Vector3 pushDirection = transform.forward;
                Vector3 objectPos = hit.transform.position;
                Vector3 targetPos = objectPos + pushDirection * 2f;

                // Verifică dacă e loc în spatele obiectului
                if (!Physics.Raycast(objectPos, pushDirection, 2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Verifică dacă e podea sub poziția țintă
                    if (Physics.Raycast(targetPos + Vector3.up, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        // Pumnul trimite cutia înainte 2 unități; robotul stă pe loc
                        Transform obj = hit.transform;
                        Vector3 crateStart = obj.position;
                        float elapsed = 0f;
                        float duration = Mathf.Max(0.1f, pushDuration - crateLaunchDelay);

                        while (elapsed < duration)
                        {
                            elapsed += Time.deltaTime;
                            float t = elapsed / duration;
                            obj.position = Vector3.Lerp(crateStart, targetPos, t);
                            yield return null;
                        }
                        obj.position = targetPos;

                        Debug.Log("Object punched");
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
        
        float remaining = pushDuration - (Time.time - startTime);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        isMoving = false;

        Debug.Log("Pushed");
    }

    public IEnumerator Press() {
        if (isDeadThisRun) yield break;
        RaycastHit hit;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Debug.DrawRay(rayOrigin, transform.forward * 2f, Color.red, 3f);
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"{PlayerLabel()} press → raycast hit '{hit.collider.name}' (root: {hit.collider.transform.root.name})");

            PressButton button = hit.collider.GetComponent<PressButton>();
            ObjectiveButton objBtn = hit.collider.GetComponentInParent<ObjectiveButton>();

            if (button != null || objBtn != null)
            {
                isMoving = true;
                TriggerAnim(PressHash);

                // așteaptă exact cât durează animația de press
                yield return new WaitForSeconds(0.1f);   // lasă tranziția în Press să se așeze
                float animLen = (animator != null) ? animator.GetCurrentAnimatorStateInfo(0).length : 1f;
                if (animLen < 0.05f || animLen > 10f) animLen = 1f;   // siguranță dacă citirea eșuează
                yield return new WaitForSeconds(animLen);

                // abia după ce animația s-a terminat → butonul roșu → verde
                if (button != null)
                {
                    if (IsSpawned)
                        PlayPressAnimationClientRpc();
                    else
                        button.Activate();
                }
                if (objBtn != null)
                    objBtn.Press();

                isMoving = false;
            }
        }
        else
        {
            Debug.Log($"{PlayerLabel()} press → raycast hit NOTHING");
        }

        yield return null;
    }

    public IEnumerator Pull() {
        if (isDeadThisRun) yield break;
        RaycastHit hit;

        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        if (Physics.Raycast(rayOrigin, transform.forward, out hit, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            Lever lever = hit.collider.GetComponentInParent<Lever>();
            if (lever != null)
            {
                TriggerAnim(PressHash);
                lever.Flip();
                yield return new WaitForSeconds(1.5f);
            }
        }

        yield return null;
    }

    [ClientRpc]
    private void PlayPressAnimationClientRpc()
    {
        var button = FindAnyObjectByType<PressButton>();
        if (button != null) button.Activate();
    }

    public bool IsMoving()
    {
        return isMoving || isDying;
    }

    public bool IsDead()
    {
        return isDeadThisRun;
    }

    public bool IsOnFinish()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, ~0, QueryTriggerInteraction.Collide))
            return hit.collider.GetComponentInParent<FinishZone>() != null;
        return false;
    }

    public void ResetToStart()
    {
        StopAllCoroutines();
        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = Vector3.one;
        isMoving = false;
        isResetting = false;
        isDeadThisRun = false;
        isDying = false;
        if (animator != null) animator.Rebind();
    }

    public IEnumerator Die(string cause)
    {
        if (isDeadThisRun) yield break;
        isDeadThisRun = true;
        isDying = true;

        Debug.Log($"{PlayerLabel()} <color=red>Died</color>: {cause}");

        if (IsSpawned) PlayDeathEffectsClientRpc();
        else PlayDeathEffectsLocal();

        // wait one frame for the Die trigger to actually transition into the death state
        yield return null;
        yield return new WaitForSeconds(0.05f);

        float animLen = (animator != null) ? animator.GetCurrentAnimatorStateInfo(0).length : deathDuration;
        if (animLen < 0.05f || animLen > 10f) animLen = deathDuration;
        yield return new WaitForSeconds(animLen);

        transform.position = startPosition;
        transform.rotation = startRotation;
        if (animator != null) animator.Rebind();

        isDying = false;
    }

    [ClientRpc]
    private void PlayDeathEffectsClientRpc()
    {
        PlayDeathEffectsLocal();
    }

    private void PlayDeathEffectsLocal()
    {
        if (animator != null) animator.SetTrigger(DieHash);
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
    }
}