using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;

/// <summary>
/// 简化版网络管理器UI助手类
/// 集成到MenuManager中使用，处理网络连接和回调，移除玩家角色区分
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private MenuManager menuManager; // 菜单管理器引用
    [SerializeField] private NetworkGameManager networkGameManager; // 网络游戏管理器
    [Header("游戏管理器引用")]
    [SerializeField] private GameManager gameManager;

    // 网络相关
    private string joinCode;
    private bool isConnecting = false;
    private const int MaxPlayers = 2;

    /// <summary>
    /// 获取当前是否正在连接
    /// </summary>
    public bool IsConnecting()
    {
        return isConnecting;
    }

    private void Awake()
    {
        // 确保此对象不会在场景加载时被销毁
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        // 初始化Unity服务
        await InitializeUnityServices();

        // 注册网络管理器事件
        RegisterNetworkManagerEvents();

        // 查找菜单管理器（如果未设置）
        if (menuManager == null)
        {
            menuManager = FindFirstObjectByType<MenuManager>();
        }
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            // 初始化Unity服务
            await UnityServices.InitializeAsync();

            // 匿名登录
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    private void RegisterNetworkManagerEvents()
    {
        // 订阅网络事件
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅网络事件
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    #region 公共网络方法 - 供MenuManager调用

    /// <summary>
    /// 创建游戏房间
    /// </summary>
    public async void CreateRoom()
    {
        isConnecting = true;

        try
        {
            // 创建Relay分配
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);

            // 获取加入代码
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 设置网络传输
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(allocation, "dtls"));

            // 启动主机
            NetworkManager.Singleton.StartHost();

            // 更新UI显示 - 通过MenuManager
            if (menuManager != null)
            {
                if (menuManager.createRoomLoadingAnimation != null)
                    menuManager.createRoomLoadingAnimation.SetActive(false);

                if (menuManager.createRoomTitle != null)
                    menuManager.createRoomTitle.text = "房间已创建";

                if (menuManager.roomCodeText != null && menuManager.roomCodeText.gameObject != null)
                {
                    menuManager.roomCodeText.gameObject.SetActive(true);
                    menuManager.roomCodeText.text = joinCode;
                }

                if (menuManager.copyCodeButton != null && menuManager.copyCodeButton.gameObject != null)
                    menuManager.copyCodeButton.gameObject.SetActive(true);
            }

            Debug.Log($"Room created with code: {joinCode}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create room: {e.Message}");

            // 更新UI显示 - 通过MenuManager
            if (menuManager != null)
            {
                if (menuManager.createRoomLoadingAnimation != null)
                    menuManager.createRoomLoadingAnimation.SetActive(false);

                if (menuManager.createRoomTitle != null)
                    menuManager.createRoomTitle.text = "创建房间失败";

                if (menuManager.roomCodeText != null && menuManager.roomCodeText.gameObject != null)
                {
                    menuManager.roomCodeText.gameObject.SetActive(true);
                    menuManager.roomCodeText.text = "请重试";
                }
            }

            isConnecting = false;
        }
    }

    /// <summary>
    /// 加入游戏房间
    /// </summary>
    public async void JoinRoom()
    {
        // 通过MenuManager获取输入的房间代码
        string code = "";
        if (menuManager != null && menuManager.roomCodeInputField != null)
        {
            code = menuManager.roomCodeInputField.text.Trim();
        }

        if (string.IsNullOrEmpty(code))
        {
            if (menuManager != null && menuManager.joinStatusText != null)
            {
                menuManager.joinStatusText.text = "请输入有效的房间代码";
            }
            return;
        }

        // 更新UI状态
        if (menuManager != null)
        {
            if (menuManager.joinRoomTitle != null)
                menuManager.joinRoomTitle.text = "正在加入房间...";

            if (menuManager.joinStatusText != null)
                menuManager.joinStatusText.text = "连接中...";

            if (menuManager.joinRoomLoadingAnimation != null)
                menuManager.joinRoomLoadingAnimation.SetActive(true);

            if (menuManager.confirmJoinButton != null)
                menuManager.confirmJoinButton.interactable = false;
        }

        isConnecting = true;

        try
        {
            // 加入Relay分配
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            // 设置网络传输
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                new RelayServerData(joinAllocation, "dtls"));

            // 启动客户端
            NetworkManager.Singleton.StartClient();

            Debug.Log($"Joined room with code: {code}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join room: {e.Message}");

            // 更新UI状态
            if (menuManager != null)
            {
                if (menuManager.joinRoomLoadingAnimation != null)
                    menuManager.joinRoomLoadingAnimation.SetActive(false);

                if (menuManager.joinStatusText != null)
                    menuManager.joinStatusText.text = "加入房间失败，请检查房间代码";

                if (menuManager.confirmJoinButton != null)
                    menuManager.confirmJoinButton.interactable = true;
            }

            isConnecting = false;
        }
    }

    /// <summary>
    /// 复制房间代码到剪贴板
    /// </summary>
    public void CopyRoomCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = joinCode;
        Debug.Log($"Room code copied to clipboard: {joinCode}");
    }

    #endregion

    #region 网络事件回调

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"客户端已连接: {clientId}");

        //复位网络状态
        gameManager.ResetIsNetworkGame();

        // 当两名玩家都已连接时，切换到游戏面板
        if (NetworkManager.Singleton.ConnectedClientsIds.Count == 2)
        {
            // 使用主线程调度器执行UI更新
            StartCoroutine(DelayedShowGamePanel());
        }
    }

    private System.Collections.IEnumerator DelayedShowGamePanel()
    {
        // 等待短暂延迟，确保网络连接稳定
        yield return new WaitForSeconds(2.0f);

        Debug.Log("准备切换到游戏界面...");

        // 确保隐藏加载动画
        if (menuManager != null)
        {
            if (menuManager.createRoomLoadingAnimation != null)
                menuManager.createRoomLoadingAnimation.SetActive(false);

            if (menuManager.joinRoomLoadingAnimation != null)
                menuManager.joinRoomLoadingAnimation.SetActive(false);

            // 隐藏弹窗
            if (menuManager.createRoomPopup != null)
                menuManager.createRoomPopup.SetActive(false);

            if (menuManager.joinRoomPopup != null)
                menuManager.joinRoomPopup.SetActive(false);

            // 跳转到游戏面板
            menuManager.ShowMultiplayerGamePanel();
        }

        isConnecting = false;

        // 通知NetworkGameManager刷新游戏状态
        if (networkGameManager != null)
        {
            Debug.Log("通知NetworkGameManager刷新游戏状态");
            // 只有服务器才能调用重置游戏
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                // 使用现有的ResetGame方法，传入默认的30个挖空格子
                networkGameManager.ResetGame(30);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"客户端断开连接: {clientId}");

        // 如果是本地客户端断开连接，返回主菜单
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // 通过MenuManager返回主菜单
            if (menuManager != null)
            {
                menuManager.BackToMainMenu();
            }

            isConnecting = false;
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("服务器已启动");
    }

    #endregion

    /// <summary>
    /// 处理网络断开连接 - 可从MenuManager调用
    /// </summary>
    public void DisconnectFromNetwork()
    {
        Debug.Log("NetworkManagerUI: 执行网络断开连接");

        // 检查是否有活动网络连接
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // 通知其他玩家我们正在断开连接
            if (PlayerSignalManager.Instance != null)
            {
                // 可选: 发送断开连接信号
                try
                {
                    PlayerSignalManager.Instance.SendSignal(
                        PlayerSignalManager.SignalType.GameEnd,
                        0,
                        "Player disconnected");
                }
                catch (System.Exception ex)
                {
                    // 捕获可能的异常，因为断开过程中发送信号可能会失败
                    Debug.LogWarning($"尝试发送断开信号失败: {ex.Message}");
                }
            }

            // 关闭网络连接
            Debug.Log("关闭NetworkManager连接...");
            NetworkManager.Singleton.Shutdown();

            // 重置连接状态
            isConnecting = false;

            // 通知其他依赖网络状态的系统
            OnNetworkDisconnected();
        }
        else
        {
            Debug.Log("没有活动的网络连接需要关闭");
        }
    }

    /// <summary>
    /// 网络断开连接后的回调
    /// </summary>
    private void OnNetworkDisconnected()
    {
        Debug.Log("网络已断开连接，执行清理操作");

        // 重置加入代码
        joinCode = string.Empty;

        // 其他需要在断开连接时执行的清理逻辑
    }

    /// <summary>
    /// 获取当前连接状态
    /// </summary>
    public bool IsConnected()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }
}