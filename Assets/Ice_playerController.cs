using UnityEngine;
using UnityEngine.InputSystem;

public class Ice_playerController : MonoBehaviour
{
    // Components
    private Rigidbody myRB;
    private Transform myAvatar;

    // Input
    [SerializeField] private InputAction WASD;
    private Vector2 movementInput;

    // Movement
    [SerializeField] private float movementSpeed = 5f;

    // Store original scale
    private Vector3 originalScale;

    private void OnEnable()
    {
        WASD.Enable();
    }

    private void OnDisable()
    {
        WASD.Disable();
    }

    void Start()
    {
        // Get Rigidbody
        myRB = GetComponent<Rigidbody>();

        // Get avatar (child object)
        myAvatar = transform.GetChild(0);

        // Save original size
        originalScale = myAvatar.localScale;
    }

    void Update()
    {
        // Read input
        movementInput = WASD.ReadValue<Vector2>();

        // Flip character without changing size
        if (movementInput.x != 0)
        {
            myAvatar.localScale = new Vector3(
                Mathf.Sign(movementInput.x) * Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
    }

    private void FixedUpdate()
    {
        myRB.linearVelocity = new Vector3(
            movementInput.x,
            movementInput.y,
            0
        ) * movementSpeed;
    }
}