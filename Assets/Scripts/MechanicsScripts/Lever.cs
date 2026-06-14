using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Lever : NetworkBehaviour
{
    [Header("Targets to toggle")]
    public GameObject[] connectedTargets;
    public bool invertTargetState = true;

    [Header("Visual")]
    public Transform handle;
    public Vector3 offRotation = new Vector3(-30, 0, 0);
    public Vector3 onRotation = new Vector3(30, 0, 0);
    public float rotationDuration = 0.3f;

    [Header("State")]
    public bool startOn = false;

    private NetworkVariable<bool> netIsOn = new NetworkVariable<bool>();
    private bool localIsOn = false;
    private List<IToggleable> resolvedTargets = new List<IToggleable>();
    private Coroutine rotateCoroutine;

    public bool IsOn => IsSpawned ? netIsOn.Value : localIsOn;

    void Start()
    {
        ResolveTargets();
        if (!IsSpawned)
        {
            localIsOn = startOn;
            ApplyVisualAndTargets(localIsOn, instant: true);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !netIsOn.Value && startOn) netIsOn.Value = true;
        netIsOn.OnValueChanged += OnNetStateChanged;
        ApplyVisualAndTargets(netIsOn.Value, instant: true);
    }

    public override void OnNetworkDespawn()
    {
        netIsOn.OnValueChanged -= OnNetStateChanged;
    }

    private void OnNetStateChanged(bool oldVal, bool newVal)
    {
        ApplyVisualAndTargets(newVal, instant: false);
    }

    public void Flip()
    {
        bool prev = IsOn;
        bool next = !prev;
        Debug.Log($"<color=magenta>[Lever]</color> Flipped: {(prev ? "ON" : "OFF")} → {(next ? "ON" : "OFF")} (targets: {resolvedTargets.Count})");

        if (IsSpawned)
        {
            if (IsServer) netIsOn.Value = next;
            else FlipServerRpc();
        }
        else
        {
            localIsOn = next;
            ApplyVisualAndTargets(localIsOn, instant: false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FlipServerRpc()
    {
        netIsOn.Value = !netIsOn.Value;
    }

    public void ResetToInitial()
    {
        Debug.Log($"<color=magenta>[Lever]</color> ResetToInitial → {(startOn ? "ON" : "OFF")}");
        if (IsSpawned)
        {
            if (IsServer && netIsOn.Value != startOn) netIsOn.Value = startOn;
        }
        else
        {
            localIsOn = startOn;
            ApplyVisualAndTargets(localIsOn, instant: true);
        }
    }

    private void ResolveTargets()
    {
        resolvedTargets.Clear();
        if (connectedTargets == null) return;
        foreach (var go in connectedTargets)
        {
            if (go == null) continue;
            var togg = go.GetComponent<IToggleable>();
            if (togg != null) resolvedTargets.Add(togg);
        }
    }

    private void ApplyVisualAndTargets(bool isOn, bool instant)
    {
        bool targetState = invertTargetState ? !isOn : isOn;
        foreach (var t in resolvedTargets) t.SetActive(targetState);

        if (handle == null) return;
        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        Vector3 targetEuler = isOn ? onRotation : offRotation;
        if (instant)
            handle.localRotation = Quaternion.Euler(targetEuler);
        else
            rotateCoroutine = StartCoroutine(RotateHandle(targetEuler));
    }

    private IEnumerator RotateHandle(Vector3 targetEuler)
    {
        Quaternion start = handle.localRotation;
        Quaternion target = Quaternion.Euler(targetEuler);
        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotationDuration;
            float smooth = t * t * (3f - 2f * t);
            handle.localRotation = Quaternion.Slerp(start, target, smooth);
            yield return null;
        }
        handle.localRotation = target;
    }
}
