using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 6f;
    public float sprintMultiplier = 1.666f;
    public float jumpForce = 5f;
    public float mouseSensitivity = 0.08f;

    [Header("Ground Check")]
    public LayerMask groundMask = -1;
    public float groundDistance = 0.2f;

    [Header("Áudio de Passos")]
    public AudioSource passoSource;
    public int indicePassoAndar = 16;
    public int indicePassoCorrer = 23;
    public float intervalAndar = 0.5f;
    public float intervalCorrer = 0.3f;

    [Header("Pitch dos Passos")]
    public float pitchAndar = 0.95f;
    public float pitchCorrer = 1.15f;
    public float variacaoPitch = 0.05f;

    private Rigidbody rb;
    private float yaw;
    private Vector3 moveInput;
    private bool isGrounded;
    private bool jumpRequested;
    private float currentSpeed;
    private float proximoPasso;
    private AudioController audioController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (passoSource == null)
        {
            passoSource = gameObject.AddComponent<AudioSource>();
            passoSource.playOnAwake = false;
            passoSource.loop = false;
            passoSource.volume = 0.5f;
        }

        audioController = FindObjectOfType<AudioController>();
        if (audioController != null && audioController.efeitoSource != null)
        {
            passoSource.outputAudioMixerGroup = audioController.efeitoSource.outputAudioMixerGroup;
        }
    }

    void Start()
    {
        currentSpeed = speed;
        yaw = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
        ResetPhysics();
    }

    void OnEnable() => ResetPhysics();

    void ResetPhysics()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = transform.position;
        rb.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        HandleMouseLook();
        HandleMovementInput();
        HandleJumpInput();
        HandleSprint();
        HandleFootsteps();
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;

        RotatePlayer();
        CheckGrounded();
        ApplyMovement();
        ApplyJump();
    }

    // 🔊 Lógica dos sons de passos
    void HandleFootsteps()
    {
        float velocidadeHorizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        bool correndo = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        if (isGrounded && velocidadeHorizontal > 0.1f)
        {
            // Escolhe o índice do clip conforme estado
            int indiceClip = correndo ? indicePassoCorrer : indicePassoAndar;
            float pitchBase = correndo ? pitchCorrer : pitchAndar;
            float intervalo = correndo ? intervalCorrer : intervalAndar;

            if (Time.time >= proximoPasso)
            {
                if (audioController != null &&
                    audioController.efeitos != null &&
                    audioController.efeitos.Length > indiceClip &&
                    audioController.efeitos[indiceClip] != null)
                {
                    passoSource.clip = audioController.efeitos[indiceClip];
                    passoSource.pitch = pitchBase + Random.Range(-variacaoPitch, variacaoPitch);
                    passoSource.Play();

                    proximoPasso = Time.time + intervalo;
                }
            }
        }
        else
        {
            if (passoSource.isPlaying)
                passoSource.Stop();

            proximoPasso = 0f;
        }
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;
        float mouseX = Mouse.current.delta.x.ReadValue();
        yaw += mouseX * mouseSensitivity;
    }

    void RotatePlayer()
    {
        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    void HandleMovementInput()
    {
        moveInput = Vector3.zero;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.wKey.isPressed) moveInput.z += 1f;
        if (kb.sKey.isPressed) moveInput.z -= 1f;
        if (kb.aKey.isPressed) moveInput.x -= 1f;
        if (kb.dKey.isPressed) moveInput.x += 1f;
        moveInput.Normalize();
    }

    void HandleJumpInput()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;
    }

    void HandleSprint()
    {
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            currentSpeed = speed * sprintMultiplier;
        else
            currentSpeed = speed;
    }

    void CheckGrounded()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        float height = col.bounds.size.y;
        Vector3 feetPos = transform.position - new Vector3(0, height * 0.5f, 0);
        feetPos += Vector3.up * 0.05f;
        float rayDistance = groundDistance + 0.3f;

        isGrounded = Physics.Raycast(feetPos, Vector3.down, rayDistance, groundMask);
    }

    void ApplyMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * moveInput.z) + (right * moveInput.x);
        direction.Normalize();

        Vector3 velocity = direction * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    void ApplyJump()
    {
        if (jumpRequested && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;

            // 🔥 SOM DE PULO (índice 19)
            audioController?.TocarEfeito(19);
        }
    }
}