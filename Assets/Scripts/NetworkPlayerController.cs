using UnityEngine;
using Unity.Netcode;
using TMPro;

/// 管理网络玩家的角色和交互
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI playerStatusText;

    [Header("Settings")]
    [SerializeField] private Color hostColor = Color.green;
    [SerializeField] private Color clientColor = Color.blue;

    // 玩家角色枚举
    public enum PlayerRole : byte
    {
        Host,     // 房主
        Guest     // 客户玩家
    }

    // 网络变量 - 玩家角色
    private NetworkVariable<PlayerRole> playerRole = new NetworkVariable<PlayerRole>(
        PlayerRole.Host, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 网络变量 - 玩家名称
    private NetworkVariable<Unity.Collections.FixedString32Bytes> playerName =
        new NetworkVariable<Unity.Collections.FixedString32Bytes>(
            "Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 注册网络变量变化回调
        playerRole.OnValueChanged += OnPlayerRoleChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;

        if (IsServer)
        {
            // 服务器端分配角色
            AssignPlayerRoles();
        }

        if (IsOwner)
        {
            // 设置自己的名称
            SetPlayerName($"Player {NetworkManager.Singleton.LocalClientId}");
        }

        // 更新UI
        UpdatePlayerUI();
    }

    // 分配玩家角色
    private void AssignPlayerRoles()
    {
        if (!IsServer) return;

        // 获取所有客户端ID
        var clientIds = NetworkManager.Singleton.ConnectedClientsIds;

        foreach (var clientId in clientIds)
        {
            var clientObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerController = clientObject.GetComponent<NetworkPlayerController>();

            if (playerController != null)
            {
                if (clientId == NetworkManager.ServerClientId)
                {
                    // 服务器玩家是房主
                    playerController.playerRole.Value = PlayerRole.Host;
                }
                else
                {
                    // 其他玩家是客户玩家
                    playerController.playerRole.Value = PlayerRole.Guest;
                }
            }
        }
    }

    // 玩家角色变化回调
    private void OnPlayerRoleChanged(PlayerRole previous, PlayerRole current)
    {
        UpdatePlayerUI();
    }

    // 玩家名称变化回调
    private void OnPlayerNameChanged(Unity.Collections.FixedString32Bytes previous, Unity.Collections.FixedString32Bytes current)
    {
        UpdatePlayerUI();
    }

    // 更新玩家UI
    private void UpdatePlayerUI()
    {
        if (playerStatusText == null) return;

        string roleText = "";
        Color roleColor = Color.white;

        switch (playerRole.Value)
        {
            case PlayerRole.Host:
                roleText = "房主";
                roleColor = hostColor;
                break;
            case PlayerRole.Guest:
                roleText = "挑战者";
                roleColor = clientColor;
                break;
        }

        playerStatusText.text = $"{playerName.Value} ({roleText})";
        playerStatusText.color = roleColor;
    }

    // 设置玩家名称 (只有拥有者可以调用)
    public void SetPlayerName(string name)
    {
        if (IsOwner)
        {
            playerName.Value = name;
        }
    }

    // 检查玩家是否可以与游戏交互
    public bool CanInteractWithGame()
    {
        return true; // 现在两种角色都可以交互
    }

    // 获取玩家角色
    public PlayerRole GetPlayerRole()
    {
        return playerRole.Value;
    }
}