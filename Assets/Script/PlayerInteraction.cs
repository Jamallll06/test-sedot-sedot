using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerInteraction : NetworkBehaviour
{
    public Transform holdPosition;
    public Camera playerCamera;
    public float pickupRange = 3f;
    public float maxThrowForce = 15f;
    public float chargeRate = 10f;

    private Package heldPackage;
    private NetworkObject heldPackageNetObj;
    private float currentThrowForce = 0f;

    void Update()
    {
        if (!IsOwner) return;

        HandlePickup();
        HandleThrowing();
    }

    void HandlePickup()
    {
        if (Input.GetKeyDown(KeyCode.E) && heldPackage == null)
        {
            if (playerCamera == null) return;

            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupRange))
            {
                Package pkg = hit.collider.GetComponent<Package>();
                if (pkg != null)
                {
                    heldPackage = pkg;
                    heldPackageNetObj = pkg.GetComponent<NetworkObject>();
                    RequestPickupServerRpc(heldPackageNetObj);
                }
            }
        }
    }

    [ServerRpc]
    void RequestPickupServerRpc(NetworkObjectReference packageRef)
    {
        if (packageRef.TryGet(out NetworkObject packageObj))
        {
            // Gunakan Network Parenting agar paket terlihat oleh semua player di jaringan
            packageObj.TrySetParent(transform);

            // Sesuaikan posisi lokal agar menempel rapi di depan player
            packageObj.transform.localPosition = new Vector3(0f, 0f, 1.5f);
            packageObj.transform.localRotation = Quaternion.identity;

            Rigidbody rb = packageObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }
    }

    void HandleThrowing()
    {
        if (heldPackage == null) return;

        if (Input.GetMouseButton(0) && !heldPackage.isFragile)
        {
            currentThrowForce += chargeRate * Time.deltaTime;
            currentThrowForce = Mathf.Clamp(currentThrowForce, 0, maxThrowForce);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector3 throwDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
            Vector3 throwForceVector = heldPackage.isFragile ? Vector3.zero : throwDirection * currentThrowForce;

            ReleasePackageServerRpc(heldPackageNetObj, throwForceVector);

            heldPackage = null;
            heldPackageNetObj = null;
            currentThrowForce = 0f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ReleasePackageServerRpc(NetworkObjectReference packageRef, Vector3 force)
    {
        if (packageRef.TryGet(out NetworkObject packageObj))
        {
            // Lepas dari parent jaringan
            packageObj.TryRemoveParent();

            Rigidbody rb = packageObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}