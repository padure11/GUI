using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectiveButton : NetworkBehaviour
{
    [Header("Indicator panel")]
    public Renderer indicatorRenderer;
    public Material redMaterial;
    public Material greenMaterial;

    private static readonly List<ObjectiveButton> all = new List<ObjectiveButton>();

    private NetworkVariable<bool> netPressed = new NetworkVariable<bool>();
    private bool localPressed = false;

    public bool IsPressed => IsSpawned ? netPressed.Value : localPressed;

    void Awake()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    public override void OnDestroy()
    {
        all.Remove(this);
        base.OnDestroy();
    }

    void Start()
    {
        if (!IsSpawned) ApplyVisual(localPressed);
    }

    public override void OnNetworkSpawn()
    {
        netPressed.OnValueChanged += OnNetStateChanged;
        ApplyVisual(netPressed.Value);
    }

    public override void OnNetworkDespawn()
    {
        netPressed.OnValueChanged -= OnNetStateChanged;
    }

    private void OnNetStateChanged(bool oldVal, bool newVal)
    {
        if (newVal && AudioManager.Instance != null) AudioManager.Instance.PlayButtonPress();
        ApplyVisual(newVal);
    }

    public void Press()
    {
        if (IsPressed) return;
        Debug.Log($"<color=lime>[ObjectiveButton]</color> '{name}' pressed");

        if (IsSpawned)
        {
            if (IsServer) netPressed.Value = true;
            else PressServerRpc();
        }
        else
        {
            localPressed = true;
            ApplyVisual(localPressed);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PressServerRpc()
    {
        netPressed.Value = true;
    }

    public void ResetToInitial()
    {
        if (IsSpawned)
        {
            if (IsServer && netPressed.Value) netPressed.Value = false;
        }
        else
        {
            localPressed = false;
            ApplyVisual(localPressed);
        }
    }

    private void ApplyVisual(bool pressed)
    {
        if (indicatorRenderer == null) return;
        var mat = pressed ? greenMaterial : redMaterial;
        if (mat != null) indicatorRenderer.sharedMaterial = mat;
    }

    // True when every button in the level is pressed (vacuously true if there are none).
    public static bool AllPressed()
    {
        foreach (var b in all)
            if (b != null && !b.IsPressed) return false;
        return true;
    }
}
