using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public CharacterController controller;
    public float speed = 12f;
    public float crouchSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float mouseSensitivity = 200f;
    public Transform playerBody;
    public Camera playerCamera;

    [Header("Crouch Settings")]
    public float standingHeight = 2f;
    public float crouchHeight = 1f;

    private float xRotation = 0f;
    private Vector3 velocity;

    private Vector3 cameraStandPos;
    private Vector3 cameraCrouchPos;

    // Variabel baru untuk menyimpan posisi tengah kapsul fisika
    private Vector3 standCenter;
    private Vector3 crouchCenter;

    public NetworkVariable<bool> isCrouching = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        if (playerCamera != null)
        {
            cameraStandPos = playerCamera.transform.localPosition;
            cameraCrouchPos = new Vector3(cameraStandPos.x, cameraStandPos.y - (standingHeight - crouchHeight) / 2f, cameraStandPos.z);
        }

        // Simpan center awal dan kalkulasi center saat jongkok agar telapak kaki tetap di tanah
        if (controller != null)
        {
            standCenter = controller.center;
            crouchCenter = standCenter + new Vector3(0, (crouchHeight - standingHeight) / 2f, 0);
        }

        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }

            if (controller != null) controller.enabled = true;
        }

        isCrouching.OnValueChanged += HandleCrouchStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        isCrouching.OnValueChanged -= HandleCrouchStateChanged;
    }

    void HandleCrouchStateChanged(bool previousValue, bool newValue)
    {
        if (controller != null)
        {
            // Terapkan tinggi dan center yang benar
            controller.height = newValue ? crouchHeight : standingHeight;
            controller.center = newValue ? crouchCenter : standCenter;
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = newValue ? cameraCrouchPos : cameraStandPos;
        }
    }

    void Update()
    {
        if (!IsClient || !IsOwner) return;

        HandleMouseLook();
        HandleInput();
        HandleMovement();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching.Value = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching.Value = false;
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (controller == null) return;

        float currentSpeed = isCrouching.Value ? crouchSpeed : speed;

        Vector3 move = playerBody.right * x + playerBody.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (!controller.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f;
        }

        controller.Move(velocity * Time.deltaTime);
    }
}