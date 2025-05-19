using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// 处理玩家信息更新和玩家间通信
/// 此脚本通过ServerRpc和ClientRpc直接通信，不需要创建网络对象实例
/// </summary>
public class PlayerSignalManager : NetworkBehaviour
{
    // 单例模式
    public static PlayerSignalManager Instance { get; private set; }

    // 定义网络信号类型
    public enum SignalType : byte
    {
        None = 0,
        ButtonPress = 1,
        GameStart = 2,
        GameEnd = 3,
        // 可以添加更多信号类型
    }

    // 信号接收事件
    public event Action<SignalType, int, string, ulong> OnSignalReceived;

    // 玩家角色枚举 (从NetworkPlayerController复制过来)
    public enum PlayerRole : byte
    {
        Host,     // 房主
        Guest     // 客户玩家
    }

    // 本地玩家角色
    [SerializeField]
    private PlayerRole localPlayerRole = PlayerRole.Host;

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 自动分配角色
        if (base.IsServer || base.IsHost)
        {
            localPlayerRole = PlayerRole.Host;
            Debug.Log("玩家角色已分配: 房主");
        }
        else
        {
            localPlayerRole = PlayerRole.Guest;
            Debug.Log("玩家角色已分配: 挑战者");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region 公共方法 - 发送信号

    /// <summary>
    /// 发送按钮按下信号
    /// </summary>
    /// <param name="value">可选整数值</param>
    public void SendButtonPressSignal(int value = 1)
    {
        SendSignal(SignalType.ButtonPress, value, "");
    }

    /// <summary>
    /// 发送游戏开始信号
    /// </summary>
    public void SendGameStartSignal()
    {
        SendSignal(SignalType.GameStart, 0, "");
    }

    /// <summary>
    /// 发送游戏结束信号
    /// </summary>
    public void SendGameEndSignal()
    {
        SendSignal(SignalType.GameEnd, 0, "");
    }

    /// <summary>
    /// 发送自定义信号
    /// </summary>
    public void SendSignal(SignalType type, int intValue, string stringValue)
    {
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.LogWarning("网络未连接，无法发送信号");
            return;
        }

        // 根据服务器/客户端状态选择发送方式
        if (base.IsServer || base.IsHost)
        {
            // 服务器直接广播给所有客户端
            BroadcastSignalClientRpc(
                (byte)type,
                intValue,
                stringValue,
                NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            // 客户端通过ServerRpc发送给服务器
            SendSignalServerRpc((byte)type, intValue, stringValue);
        }

        Debug.Log($"发送信号: {type}, 值: {intValue}, 字符串: {stringValue}");
    }

    #endregion

    #region 网络RPC

    /// <summary>
    /// 服务器RPC - 客户端调用发送信号到服务器
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendSignalServerRpc(byte signalType, int intValue, string stringValue, ServerRpcParams rpcParams = default)
    {
        // 获取发送者ID
        ulong senderId = rpcParams.Receive.SenderClientId;

        // 服务器收到客户端的信号后广播给所有客户端（包括发送者）
        BroadcastSignalClientRpc(signalType, intValue, stringValue, senderId);
    }

    /// <summary>
    /// 客户端RPC - 服务器广播信号给所有客户端
    /// </summary>
    [ClientRpc]
    private void BroadcastSignalClientRpc(byte signalType, int intValue, string stringValue, ulong senderId)
    {
        // 转换回枚举类型
        SignalType type = (SignalType)signalType;

        // 忽略自己发送的信号（因为服务器会广播给所有人，包括发送者）
        if (senderId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"收到来自玩家{senderId}的信号: {type}, 值: {intValue}");

            // 触发信号接收事件
            OnSignalReceived?.Invoke(type, intValue, stringValue, senderId);
        }
    }

    #endregion

    #region 公共方法 - 获取玩家信息

    /// <summary>
    /// 获取玩家角色
    /// </summary>
    public PlayerRole GetPlayerRole()
    {
        return localPlayerRole;
    }

    /// <summary>
    /// 检查本地玩家是否是房主
    /// </summary>
    public bool IsRoomHost()
    {
        return localPlayerRole == PlayerRole.Host;
    }

    /// <summary>
    /// 检查本地玩家是否是挑战者
    /// </summary>
    public bool IsGuest()
    {
        return localPlayerRole == PlayerRole.Guest;
    }

    #endregion
}