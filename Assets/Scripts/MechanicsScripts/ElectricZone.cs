using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(BoxCollider))]
public class ElectricZone : MonoBehaviour, IToggleable
{
    [Header("Endpoints (top of each pole)")]
    public Transform poleA;
    public Transform poleB;

    [Header("Beams")]
    [Range(1, 10)] public int beamCount = 4;
    public float beamSpacing = 0.4f;
    public float beamWidth = 0.05f;
    public Material beamMaterial;

    [Header("Animation")]
    [Range(2, 30)] public int segments = 10;
    public float jaggedness = 0.05f;
    public float updateInterval = 0.05f;

    [Header("State")]
    [SerializeField] private bool startActive = true;
    public bool IsActive { get; private set; }

    private LineRenderer[] beams;
    private float updateTimer = 0f;

    void Start()
    {
        CreateBeams();
        SetActive(startActive);
    }

    private void CreateBeams()
    {
        beams = new LineRenderer[beamCount];
        Material mat = beamMaterial != null ? beamMaterial : CreateDefaultMaterial();

        for (int i = 0; i < beamCount; i++)
        {
            var go = new GameObject("Beam_" + i);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = beamWidth;
            lr.endWidth = beamWidth;
            lr.useWorldSpace = true;
            lr.material = mat;
            beams[i] = lr;
        }
    }

    private Material CreateDefaultMaterial()
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.color = new Color(0.4f, 0.9f, 1f);
        return mat;
    }

    void Update()
    {
        if (!IsActive || poleA == null || poleB == null) return;
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;
        UpdateLightning();
    }

    private void UpdateLightning()
    {
        Vector3 dir = (poleB.position - poleA.position).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;

        for (int b = 0; b < beamCount; b++)
        {
            if (beams[b] == null) continue;
            float yOffset = -b * beamSpacing;
            Vector3 start = poleA.position + Vector3.up * yOffset;
            Vector3 end = poleB.position + Vector3.up * yOffset;

            var lr = beams[b];
            lr.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 pos = Vector3.Lerp(start, end, t);
                if (i > 0 && i < segments)
                {
                    pos += perp * Random.Range(-jaggedness, jaggedness);
                    pos += Vector3.up * Random.Range(-jaggedness, jaggedness);
                }
                lr.SetPosition(i, pos);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"<color=yellow>[ElectricZone]</color> TriggerEnter from: {other.gameObject.name} (root: {other.transform.root.name})");

        if (!IsActive)
        {
            Debug.Log("<color=yellow>[ElectricZone]</color> But IsActive=false, ignoring");
            return;
        }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("<color=yellow>[ElectricZone]</color> Not server, client ignores");
            return;
        }

        var robot = other.GetComponentInParent<PlayerController>();
        if (robot != null)
        {
            Debug.Log($"{robot.PlayerLabel()} <color=yellow>Zapped</color> by electric fence!");
            robot.StartCoroutine(robot.Die("electric"));
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[ElectricZone]</color> Trigger fired but no PlayerController found on {other.gameObject.name} or parents");
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        if (beams != null)
            foreach (var b in beams) if (b != null) b.enabled = active;
    }
}
