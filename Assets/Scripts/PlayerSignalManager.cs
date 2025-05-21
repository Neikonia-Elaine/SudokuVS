using UnityEngine;
using Unity.Netcode;
using System;


/// 处理玩家信息更新和玩家间通信
/// 精简版 - 移除了玩家角色划分逻辑

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
        Debug.Log($"信号管理器已初始化，本地客户端ID: {NetworkManager.Singleton.LocalClientId}");
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

    
    /// 发送按钮按下信号
    
    /// <param name="value">可选整数值</param>
    public void SendButtonPressSignal(int value = 1)
    {
        SendSignal(SignalType.ButtonPress, value, "");
    }

    
    /// 发送游戏开始信号
    
    public void SendGameStartSignal()
    {
        SendSignal(SignalType.GameStart, 0, "");
    }

    
    /// 发送游戏结束信号
    
    public void SendGameEndSignal()
    {
        SendSignal(SignalType.GameEnd, 0, "");
    }

    
    /// 发送自定义信号
    
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

    
    /// 服务器RPC - 客户端调用发送信号到服务器
    
    [ServerRpc(RequireOwnership = false)]
    private void SendSignalServerRpc(byte signalType, int intValue, string stringValue, ServerRpcParams rpcParams = default)
    {
        // 获取发送者ID
        ulong senderId = rpcParams.Receive.SenderClientId;

        // 服务器收到客户端的信号后广播给所有客户端（包括发送者）
        BroadcastSignalClientRpc(signalType, intValue, stringValue, senderId);
    }

    
    /// 客户端RPC - 服务器广播信号给所有客户端
    
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

    
    /// 判断是否为服务器或主机
    
    public bool IsServerOrHost()
    {
        return IsServer || IsHost;
    }
}