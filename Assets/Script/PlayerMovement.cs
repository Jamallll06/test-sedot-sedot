using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public CharacterController controller;
    public float speed = 12f;
    public float mouseSensitivity = 200f;
    public Transform playerBody;
    public Camera playerCamera;

    private float xRotation = 0f;
    private Vector3 velocity;
    private float gravity = -9.81f;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Player Spawned. ID: {OwnerClientId}, IsOwner: {IsOwner}");

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
    }

    void Update()
    {
        if (!IsClient || !IsOwner) return;

        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // DIPERBAIKI: Hapus perbandingan 'long.MinValue' yang salah
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

        if (x != 0 || z != 0)
        {
            Debug.Log($"Input terdeteksi - X: {x}, Z: {z}");
        }

        if (controller == null) return;

        Vector3 move = playerBody.right * x + playerBody.forward * z;
        controller.Move(move * speed * Time.deltaTime);

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