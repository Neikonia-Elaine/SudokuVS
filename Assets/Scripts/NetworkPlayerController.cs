using UnityEngine;
using Unity.Netcode;

/// 精简版网络玩家控制器 - 不再区分房主和客户端玩家身份
public class NetworkPlayerController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 网络玩家初始化完成后的处理
        Debug.Log($"玩家已连接 ID: {NetworkManager.Singleton.LocalClientId}");
    }

    // 检查玩家是否可以与游戏交互 - 所有玩家始终可以交互
    public bool CanInteractWithGame()
    {
        return true; // 所有玩家都可以交互
    }
}