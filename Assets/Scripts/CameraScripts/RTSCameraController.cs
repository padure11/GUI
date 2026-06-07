using UnityEngine;

public class RTSCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public bool useEdgeScrolling = true;
    public float edgeScrollThreshold = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 20f;
    public float minDistance = 5f;
    public float maxDistance = 80f;
    public Transform cameraTransform;

    [Header("Rotation")]
    public float rotationSpeed = 80f;
    public float rotationSmoothing = 8f;
    public Transform pivotTarget;

    private float targetRotationY;
    private bool isRotating = false;
    private Vector3 lastMousePos;

    [Header("Drag")]
    public float dragSpeed = 1f;

    [Header("Follow")]
    public KeyCode recenterKey = KeyCode.F;
    public float followSmoothing = 6f;

    [Header("Per-player start view (camera local transform)")]
    public Vector3 hostCameraPos = new Vector3(-463.32f, -252.15f, 192.19f);
    public Vector3 hostCameraRot = new Vector3(45f, 120f, 0f);
    public Vector3 clientCameraPos = new Vector3(-463.32f, -252.15f, 158.44f);
    public Vector3 clientCameraRot = new Vector3(45f, 45f, 0f);
    private Transform followTarget;
    private bool followEngaged = false;

    private Vector3 dragOrigin;
    private bool isDragging = false;

    public void FocusOn(Transform target)
    {
        if (target == null) return;
        pivotTarget = target;
        followTarget = target;
    }

    // Places the camera at the per-player starting view. Called once at game start.
    public void SetStartView(bool isHost)
    {
        if (cameraTransform == null) return;
        cameraTransform.localPosition = isHost ? hostCameraPos : clientCameraPos;
        cameraTransform.localRotation = Quaternion.Euler(isHost ? hostCameraRot : clientCameraRot);
    }

    // Sets the point that right-click rotation orbits around (the local robot).
    // Does NOT make the camera follow — only affects rotation.
    public void SetPivot(Transform target)
    {
        pivotTarget = target;
    }

    public void Recenter()
    {
        if (followTarget == null) return;
        Vector3 rigPos = transform.position;
        rigPos.x = followTarget.position.x;
        rigPos.z = followTarget.position.z;
        transform.position = rigPos;
        followEngaged = true;
        targetRotationY = transform.eulerAngles.y;
    }

    void Update()
    {
        if (CodeEditorUI.BlocksCamera)
        {
            isDragging = false;
            isRotating = false;
            return;
        }

        //HandleEdgeScroll();
        HandleDrag();
        HandleZoom();
        HandleRotation();
        HandleRecenter();
        HandleAutoFollow();
    }

    void HandleRecenter()
    {
        if (Input.GetKeyDown(recenterKey))
            Recenter();
    }

    void HandleAutoFollow()
    {
        if (!followEngaged || followTarget == null) return;
        if (isDragging) { followEngaged = false; return; }

        Vector3 desired = new Vector3(followTarget.position.x, transform.position.y, followTarget.position.z);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * followSmoothing);
    }

    // void HandleEdgeScroll()
    // {
    //     if (!useEdgeScrolling) return;

    //     Vector3 move = Vector3.zero;

    //     if (Input.mousePosition.x < edgeScrollThreshold)
    //         move -= transform.right;
    //     if (Input.mousePosition.x > Screen.width - edgeScrollThreshold)
    //         move += transform.right;
    //     if (Input.mousePosition.y < edgeScrollThreshold)
    //         move -= transform.forward;
    //     if (Input.mousePosition.y > Screen.height - edgeScrollThreshold)
    //         move += transform.forward;

    //     move.y = 0;
    //     transform.position += move.normalized * moveSpeed * Time.deltaTime;
    // }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = GetWorldPoint();
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
            isDragging = false;

        if (isDragging)
        {
            Vector3 diff = dragOrigin - GetWorldPoint();
            diff.y = 0;
            transform.position += diff;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        Vector3 newPos = cameraTransform.position + cameraTransform.forward * scroll * zoomSpeed;
        newPos.y = Mathf.Clamp(newPos.y, minDistance, maxDistance);
        cameraTransform.position = newPos;
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePos = Input.mousePosition;
            isRotating = true;
            targetRotationY = transform.eulerAngles.y;
        }

        if (Input.GetMouseButtonUp(1))
            isRotating = false;

        if (isRotating)
        {
            float delta = Input.mousePosition.x - lastMousePos.x;
            lastMousePos = Input.mousePosition;
            targetRotationY += delta * rotationSpeed * Time.deltaTime;
        }

        float smoothY = Mathf.LerpAngle(transform.eulerAngles.y, targetRotationY, Time.deltaTime * rotationSmoothing);

        if (pivotTarget != null)
        {
            transform.RotateAround(pivotTarget.position, Vector3.up,
                Mathf.DeltaAngle(transform.eulerAngles.y, smoothY));
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, smoothY, 0f);
        }
    }

    Vector3 GetWorldPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return Vector3.zero;
    }
}