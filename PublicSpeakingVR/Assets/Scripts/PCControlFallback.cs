using UnityEngine;
using UnityEngine.XR;

public class PCControlFallback : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public float moveSpeed = 5f;
    public Transform moveRoot;
    public KeyCode unlockCursorKey = KeyCode.Escape;

    private float xRotation;
    private Transform Root => moveRoot != null ? moveRoot : (transform.parent != null ? transform.parent : transform);

    private void Start()
    {
        if (XRSettings.isDeviceActive)
        {
            Debug.Log("VR headset detected. PC fallback controls are disabled.");
            return;
        }

        Debug.Log("VR headset was not found. PC fallback controls are enabled: WASD + mouse.");
        LockCursor();
    }

    private void Update()
    {
        if (XRSettings.isDeviceActive) return;

        if (Input.GetKeyDown(unlockCursorKey))
        {
            ToggleCursorLock();
        }

        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }

        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        Root.Rotate(Vector3.up * mouseX);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 move = flatRight * x + flatForward * z;
        Root.position += move * moveSpeed * Time.deltaTime;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ToggleCursorLock()
    {
        bool shouldUnlock = Cursor.lockState == CursorLockMode.Locked;
        Cursor.lockState = shouldUnlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldUnlock;
    }
}
