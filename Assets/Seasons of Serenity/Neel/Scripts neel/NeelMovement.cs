using UnityEngine;

public class NeelMovement : MonoBehaviour
{
    public CharacterController controller;
    public Animator animator;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float rotationSpeed = 180f; // Degrees per second

    private Vector3 velocity;
    private bool isGrounded;

    private WeatherController weatherController;

    void Start()
    {
        // Find the WeatherController in the scene
        weatherController = FindObjectOfType<WeatherController>();

        if (weatherController == null)
            Debug.LogWarning("WeatherController not found in the scene!");
    }

    void Update()
    {
        // ----- Ground Check -----
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // ----- Input -----
        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButtonDown("Jump");

        // ----- Movement -----
        float moveForward = forwardPressed ? 1f : 0f;
        float currentSpeed = (forwardPressed && shiftPressed) ? runSpeed : walkSpeed;

        Vector3 move = transform.forward * moveForward;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // ----- Rotation -----
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;

        transform.Rotate(Vector3.up, horizontalInput * rotationSpeed * Time.deltaTime);

        // ----- Animation -----
        animator.SetFloat("speed", moveForward);
        animator.SetBool("isRunning", forwardPressed && shiftPressed);

        // ----- Jumping -----
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        // ----- Apply Gravity -----
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // ----- Weather Control (Press Q) -----
        if (Input.GetKeyDown(KeyCode.Q) && weatherController != null)
        {
            weatherController.CycleWeather(transform.position);
        }
    }
}
