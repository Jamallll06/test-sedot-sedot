using UnityEngine;

public class ShelfSlot : MonoBehaviour
{
    // Mengecek apakah sudah ada paket di slot ini
    public bool IsOccupied()
    {
        // Mengecek dalam radius 0.4 meter dari titik tengah kotak rak
        // (Sesuaikan angka 0.4f jika paketmu lebih besar/kecil)
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.2f);
        foreach (Collider col in colliders)
        {
            if (col.GetComponent<Package>() != null)
            {
                return true; // Kotak sudah terisi
            }
        }
        return false; // Kotak kosong
    }
}