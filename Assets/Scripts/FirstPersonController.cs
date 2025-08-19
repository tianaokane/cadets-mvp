using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;
    public float accel = 10f;              // acceleration (clunky start)
    public float decel = 12f;              // decel (clunky stop)
    public float jumpHeight = 1.6f;
    public float gravity = -13f;

    [Header("Look")]
    public Camera cam;
    public float mouseSensitivity = 600f;  // degrees/sec
    public float minPitch = -75f;
    public float maxPitch = 75f;

    [Header("Grounding")]
    public float groundedRememberTime = 0.08f; // small coyote time
    public LayerMask groundMask = ~0;          // default: everything
    public float groundCheckRadius = 0.25f;
    public float groundCheckOffset = 0.1f;

    [Header("Kid Clunk (head bob)")]
    public float bobFrequency = 6.5f;   // steps per second feel
    public float bobAmplitude = 0.035f; // up/down wobble
    public float bobSway = 0.02f;       // tiny left/right sway
    public float bobSpeedThreshold = 0.1f;

    CharacterController cc;
    float pitch;
    Vector3 velocity;         // y-gravity
    Vector3 currentVel;       // smoothed xz velocity
    float groundedRemember;   // timer for coyote jump
    Vector3 camStartLocalPos;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!cam) cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cam) camStartLocalPos = cam.transform.localPosition;
    }

    void Update()
    {
        LookUpdate();
        MoveUpdate();
        HeadBobUpdate();
    }

    void LookUpdate()
{
    // Use raw delta for consistent feel across frame rates
    float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
    float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

    // Horizontal look
    transform.Rotate(Vector3.up * mouseX);

    // Vertical look (pitch)
    pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
    if (cam) cam.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
}

    void MoveUpdate()
    {
        // Simple grounded check with small sphere near feet (more reliable than isGrounded alone)
        Vector3 groundCheckPos = transform.position + Vector3.down * (cc.height * 0.5f - cc.skinWidth - groundCheckOffset);
        bool grounded = Physics.CheckSphere(groundCheckPos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        if (grounded) groundedRemember = groundedRememberTime;
        else groundedRemember -= Time.deltaTime;

        // Input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 desired = (transform.right * h + transform.forward * v).normalized * moveSpeed;

        // Clunky accel/decel feel
        float smooth = (desired.sqrMagnitude > 0.001f) ? accel : decel;
        currentVel = Vector3.MoveTowards(currentVel, desired, smooth * Time.deltaTime);

        // Apply horizontal movement
        cc.Move(currentVel * Time.deltaTime);

        // Jump
        if (groundedRemember > 0f && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            groundedRemember = 0f;
        }

        // Gravity
        if (grounded && velocity.y < 0f) velocity.y = -2f; // stick to ground
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void HeadBobUpdate()
    {
        if (!cam) return;

        // bob only when moving
        Vector3 flatVel = new Vector3(currentVel.x, 0f, currentVel.z);
        float speed = flatVel.magnitude;
        if (speed > bobSpeedThreshold)
        {
            float t = Time.time * bobFrequency;
            float y = Mathf.Sin(t) * bobAmplitude;
            float x = Mathf.Cos(t * 0.5f) * bobSway; // gentle sway
            cam.transform.localPosition = camStartLocalPos + new Vector3(x, y, 0f);
        }
        else
        {
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, camStartLocalPos, 10f * Time.deltaTime);
        }
    }

    // Visualize ground check in Scene view
    void OnDrawGizmosSelected()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        Vector3 groundCheckPos = transform.position + Vector3.down * (cc.height * 0.5f - cc.skinWidth - groundCheckOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPos, groundCheckRadius);
    }
}
