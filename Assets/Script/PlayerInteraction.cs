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

        if (heldPackage != null && holdPosition != null)
        {
            Vector3 targetLocalPos = transform.InverseTransformPoint(holdPosition.position);
            Quaternion targetLocalRot = Quaternion.Inverse(transform.rotation) * holdPosition.rotation;

            SyncPackagePositionServerRpc(heldPackageNetObj, targetLocalPos, targetLocalRot);
        }
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
            packageObj.TrySetParent(transform);

            Rigidbody rb = packageObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }
    }

    [ServerRpc]
    void SyncPackagePositionServerRpc(NetworkObjectReference packageRef, Vector3 localPos, Quaternion localRot)
    {
        if (packageRef.TryGet(out NetworkObject packageObj))
        {
            packageObj.transform.localPosition = localPos;
            packageObj.transform.localRotation = localRot;
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
            Vector3 throwForceVector = heldPackage.isFragile ? throwDirection * 2f : throwDirection * currentThrowForce;

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