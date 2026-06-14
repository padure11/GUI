using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(BoxCollider))]
public class Portal : MonoBehaviour
{
    [Header("Pairing")]
    public Portal otherPortal;
    public Vector3 exitOffset = new Vector3(0f, -0.98f, 0f);

    [Header("Glow (inner disc)")]
    public Renderer glowRenderer;
    [ColorUsage(true, true)] public Color glowColor = new Color(0.2f, 0.5f, 1f);
    [Range(0f, 8f)] public float glowIntensity = 3f;

    [Header("Teleport timing")]
    public float warpOutDuration = 0.35f;
    public float warpInDuration = 0.35f;

    void Start()
    {
        if (otherPortal == null)
            Debug.LogWarning($"[Portal] '{name}' has no otherPortal assigned — teleport disabled.");
        else if (otherPortal.otherPortal != this)
            Debug.LogWarning($"[Portal] '{name}' ↔ '{otherPortal.name}' pairing is not reciprocal. " +
                             $"Set '{otherPortal.name}'.otherPortal back to '{name}'.");

        if (glowRenderer != null)
            glowRenderer.material.SetColor("_GlowColor", glowColor * glowIntensity);
    }

    void OnTriggerEnter(Collider other)
    {
        // server-authoritative: only the server decides teleports
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            && !NetworkManager.Singleton.IsServer) return;

        var robot = other.GetComponentInParent<PlayerController>();
        if (robot == null) return;

        Debug.Log($"<color=#9b59ff>[Portal]</color> '{name}' OnTriggerEnter by {robot.PlayerLabel()}");

        if (otherPortal == null)
        {
            Debug.LogWarning($"<color=#9b59ff>[Portal]</color> '{name}' otherPortal is NULL — ignoring");
            return;
        }
        if (Time.time < robot.portalCooldownUntil)
        {
            Debug.Log($"<color=#9b59ff>[Portal]</color> '{name}' robot in cooldown ({robot.portalCooldownUntil - Time.time:F2}s left) — ignoring");
            return;
        }
        if (robot.pendingPortal != null)
        {
            Debug.Log($"<color=#9b59ff>[Portal]</color> '{name}' robot already has pendingPortal '{robot.pendingPortal.name}' — ignoring");
            return;
        }

        Debug.Log($"{robot.PlayerLabel()} <color=#9b59ff>entered portal</color> '{name}' → will teleport");
        robot.pendingPortal = this;
    }

    public Vector3 GetExitPosition()
    {
        if (otherPortal == null) return transform.position;
        return otherPortal.transform.position + otherPortal.exitOffset;
    }
}
