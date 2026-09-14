using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

public class RelayManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField joinCodeInput;
    public TextMeshProUGUI joinCodeDisplayText; // Teks untuk menampilkan kode di layar Host
    public GameObject mainMenuPanel;
    public GameObject joinCodePanel;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Sign in sebagai: " + AuthenticationService.Instance.PlayerId);

        // Pastikan panel awal sesuai
        if (joinCodePanel != null) joinCodePanel.SetActive(false);
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Join Code: " + joinCode);

            // Tampilkan Join Code ke UI agar Host bisa melihatnya
            if (joinCodeDisplayText != null)
            {
                joinCodeDisplayText.text = "Join Code: " + joinCode;
            }

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();

            // Sembunyikan menu setelah sukses jadi host
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();

            // Sembunyikan menu setelah sukses join
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (joinCodePanel != null) joinCodePanel.SetActive(false);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    // Fungsi Tombol UI
    public void OnHostClicked()
    {
        CreateRelay();
    }

    public void OnOpenJoinMenuClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (joinCodePanel != null) joinCodePanel.SetActive(true);
    }

    public void OnSubmitJoinClicked()
    {
        JoinRelay(joinCodeInput.text);
    }

    public void OnBackClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (joinCodePanel != null) joinCodePanel.SetActive(false);
    }
}