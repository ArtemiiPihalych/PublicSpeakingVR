using UnityEngine;
using UnityEngine.XR;

public class VRThumbstickMovement : MonoBehaviour
{
    public Transform xrRoot;
    public Transform head;
    public float moveSpeed = 1.6f;
    public float turnSpeed = 90f;
    public float deadZone = 0.18f;
    public bool enableSmoothTurn = true;

    private void Awake()
    {
        if (xrRoot == null) xrRoot = transform;
        if (head == null && Camera.main != null) head = Camera.main.transform;
    }

    private void Update()
    {
        if (!XRSettings.isDeviceActive && !XRSettings.enabled) return;

        Vector2 moveAxis = ReadAxis(XRNode.LeftHand);
        Vector2 turnAxis = ReadAxis(XRNode.RightHand);

        Move(moveAxis);
        Turn(turnAxis);
    }

    private Vector2 ReadAxis(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
        {
            return axis.magnitude >= deadZone ? axis : Vector2.zero;
        }

        return Vector2.zero;
    }

    private void Move(Vector2 axis)
    {
        if (axis == Vector2.zero || xrRoot == null) return;

        Transform reference = head != null ? head : xrRoot;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up).normalized;
        Vector3 motion = right * axis.x + forward * axis.y;

        xrRoot.position += motion * (moveSpeed * Time.deltaTime);
    }

    private void Turn(Vector2 axis)
    {
        if (!enableSmoothTurn || Mathf.Abs(axis.x) < deadZone || xrRoot == null) return;

        xrRoot.Rotate(Vector3.up, axis.x * turnSpeed * Time.deltaTime, Space.World);
    }
}
