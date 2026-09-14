using UnityEngine;
using Unity.Netcode;

public class Package : NetworkBehaviour
{
    public bool isFragile = false;
    public float breakThreshold = 10f; // Batas kecepatan lempar/benturan sebelum hancur
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReleaseServerRpc(Vector3 force)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.TryRemoveParent();
        }

        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }

        // Cek langsung saat dilempar: jika paket fragile DAN dilempar terlalu kencang
        if (isFragile && force.magnitude > breakThreshold)
        {
            Debug.Log("Paket hancur karena dilempar terlalu kencang!");
            DestroyPackageServer();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // Cek benturan fisik (misal menabrak tembok/lantai terlalu keras)
        float impactVelocity = collision.relativeVelocity.magnitude;
        if (isFragile && impactVelocity > breakThreshold)
        {
            Debug.Log("Paket hancur karena membentur objek terlalu keras!");
            DestroyPackageServer();
        }
    }

    void DestroyPackageServer()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true); // Hapus objek secara sinkron di semua client jaringan
        }
    }
}