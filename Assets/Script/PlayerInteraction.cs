using UnityEngine;
using Unity.Netcode;

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

        HandleInteract(); // Menggantikan fungsi HandlePickup
        HandleThrowing();

        if (heldPackage != null && holdPosition != null)
        {
            Vector3 targetLocalPos = transform.InverseTransformPoint(holdPosition.position);
            Quaternion targetLocalRot = Quaternion.Inverse(transform.rotation) * holdPosition.rotation;

            SyncPackagePositionServerRpc(heldPackageNetObj, targetLocalPos, targetLocalRot);
        }
    }

    void HandleInteract()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (playerCamera == null) return;

            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, pickupRange))
            {
                // KONDISI 1: Tangan Kosong -> Ambil Paket
                if (heldPackage == null)
                {
                    Package pkg = hit.collider.GetComponent<Package>();
                    if (pkg != null)
                    {
                        heldPackage = pkg;
                        heldPackageNetObj = pkg.GetComponent<NetworkObject>();
                        RequestPickupServerRpc(heldPackageNetObj);
                    }
                }
                // KONDISI 2: Sedang Membawa Paket -> Taruh ke Rak
                else
                {
                    ShelfSlot slot = hit.collider.GetComponent<ShelfSlot>();
                    if (slot != null && !slot.IsOccupied())
                    {
                        // Taruh paket di titik slot rak tersebut
                        PlacePackageServerRpc(heldPackageNetObj, slot.transform.position, slot.transform.rotation);

                        // Kosongkan tangan pemain
                        heldPackage = null;
                        heldPackageNetObj = null;
                        currentThrowForce = 0f;
                    }
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
                rb.detectCollisions = false; // Matikan fisik agar tidak nabrak saat dipegang
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

    // FUNGSI BARU: Menaruh paket ke rak secara sinkron di server
    [ServerRpc]
    void PlacePackageServerRpc(NetworkObjectReference packageRef, Vector3 slotPosition, Quaternion slotRotation)
    {
        if (packageRef.TryGet(out NetworkObject packageObj))
        {
            packageObj.TryRemoveParent(); // Lepas paket dari tangan pemain

            // Snap (kunci) posisi paket ke tengah kotak rak
            packageObj.transform.position = slotPosition;
            packageObj.transform.rotation = slotRotation;

            Rigidbody rb = packageObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Kunci fisik agar paket tidak jatuh/tergeser dari rak
                rb.detectCollisions = true; // NYALAKAN KEMBALI collider agar paket bisa di-klik 'E' dan diambil lagi nanti
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