using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 菜单管理器 - 修订版本，支持双棋盘但移除角色区分
/// </summary>
public class MenuManager : MonoBehaviour
{
    #region 面板引用
    [Header("面板引用")]
    public GameObject mainPanel;
    public GameObject gamePanel;
    public GameObject difficultyPanel;
    public GameObject victoryPanel;
    public GameObject skillPanel;
    public GameObject optionPanel;
    public GameObject timerPanel;
    public GameObject hintPanel;

    [Header("多人游戏面板")]
    public GameObject twoPlayerRoomPanel; // 双人房间面板
    public GameObject multiPlayerGamePanel; // 多人游戏面板
    public Transform localPlayerBoardContainer; // 本地玩家数独容器
    public Transform remoteBoardContainer; // 远程玩家数独容器
    #endregion

    #region 游戏管理器引用
    [Header("游戏管理器引用")]
    public GameManager gameManager;
    public SkillManager skillManager;
    public NetworkGameManager networkGameManager; // 网络游戏管理器
    #endregion

    #region 按钮引用
    [Header("主菜单按钮引用")]
    public Button singleModeButton;
    public Button multiModeButton; // 启用多人模式按钮
    public Button settingButton;
    public Button exitButton;

    [Header("游戏操作按钮引用")]
    public Button restartGameButton; // 重新开始游戏按钮
    public Button backToMenuButton; // 返回主菜单按钮

    [Header("难度选择按钮引用")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("多人游戏按钮")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button backToMainFromRoomButton;

    [Header("创建房间弹窗")]
    public GameObject createRoomPopup;
    public TextMeshProUGUI createRoomTitle;
    public TextMeshProUGUI roomCodeText;
    public Button copyCodeButton;
    public Button closeCreateRoomPopupButton;
    public GameObject createRoomLoadingAnimation;

    [Header("加入房间弹窗")]
    public GameObject joinRoomPopup;
    public TextMeshProUGUI joinRoomTitle;
    public TMP_InputField roomCodeInputField;
    public Button confirmJoinButton;
    public Button closeJoinRoomPopupButton;
    public GameObject joinRoomLoadingAnimation;
    public TextMeshProUGUI joinStatusText;
    #endregion

    [Header("特效引用")]
    public ParticleSystem rainParticle; // 挂载下雨效果粒子

    // 游戏状态
    private int currentDifficulty = 30; // 当前选择的难度（默认为中等）
    private Timer timerScript; // 引用 Timer 脚本

    // 网络游戏UI引用
    private NetworkManagerUI networkManagerUI;

    void Start()
    {
        // 初始化面板显示状态
        InitializePanels();

        // 播放环境特效
        PlayEnvironmentEffects();

        // 绑定按钮事件
        SetupButtonListeners();

        // 查找必要组件
        FindRequiredComponents();
    }

    #region 初始化方法
    // 初始化面板显示状态
    private void InitializePanels()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (skillPanel != null) skillPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (timerPanel != null) timerPanel.SetActive(false);
        if (hintPanel != null) hintPanel.SetActive(false);

        // 多人游戏面板初始化
        if (twoPlayerRoomPanel != null) twoPlayerRoomPanel.SetActive(false);
        if (multiPlayerGamePanel != null) multiPlayerGamePanel.SetActive(false);
        if (createRoomPopup != null) createRoomPopup.SetActive(false);
        if (joinRoomPopup != null) joinRoomPopup.SetActive(false);

        // 禁用加载动画
        if (createRoomLoadingAnimation != null) createRoomLoadingAnimation.SetActive(false);
        if (joinRoomLoadingAnimation != null) joinRoomLoadingAnimation.SetActive(false);
    }

    // 播放环境特效
    private void PlayEnvironmentEffects()
    {
        if (rainParticle != null && !rainParticle.isPlaying)
        {
            rainParticle.Play();
        }
    }

    // 查找必要组件
    private void FindRequiredComponents()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("无法找到GameManager引用!");
            }
        }

        if (skillManager == null)
        {
            skillManager = FindFirstObjectByType<SkillManager>();
            if (skillManager == null)
            {
                Debug.LogWarning("无法找到SkillManager引用，部分功能可能不可用!");
            }
        }

        if (timerPanel != null && timerScript == null)
        {
            timerScript = timerPanel.GetComponent<Timer>();
            if (timerScript == null)
            {
                Debug.LogWarning("Timer脚本未找到，计时功能可能不可用!");
            }
        }

        // 查找NetworkManagerUI组件
        if (networkManagerUI == null)
        {
            networkManagerUI = FindFirstObjectByType<NetworkManagerUI>();
            if (networkManagerUI == null)
            {
                Debug.LogWarning("NetworkManagerUI未找到，多人游戏功能可能不可用!");
            }
        }

        // 查找NetworkGameManager组件
        if (networkGameManager == null)
        {
            networkGameManager = FindFirstObjectByType<NetworkGameManager>();
            if (networkGameManager == null)
            {
                Debug.LogWarning("NetworkGameManager未找到，多人游戏功能可能不可用!");
            }
        }
    }

    // 设置所有按钮监听器
    private void SetupButtonListeners()
    {
        // 主菜单按钮
        if (singleModeButton != null)
        {
            singleModeButton.onClick.AddListener(OnSingleModeButtonClicked);
        }

        if (multiModeButton != null)
        {
            multiModeButton.onClick.AddListener(OnMultiModeButtonClicked);
        }

        // 设置按钮
        if (settingButton != null)
        {
            settingButton.onClick.AddListener(OpenSettingsPanel);
        }

        // 返回主菜单按钮
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(BackToMainMenu);
        }

        // 重新开始游戏按钮
        if (restartGameButton != null)
        {
            restartGameButton.onClick.AddListener(RestartGame);
        }

        // 退出游戏按钮
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(QuitGame);
        }

        // 多人游戏按钮
        if (createRoomButton != null)
        {
            createRoomButton.onClick.AddListener(ShowCreateRoomPopup);
        }

        if (joinRoomButton != null)
        {
            joinRoomButton.onClick.AddListener(ShowJoinRoomPopup);
        }

        if (backToMainFromRoomButton != null)
        {
            backToMainFromRoomButton.onClick.AddListener(() => {
                SetPanelActive(twoPlayerRoomPanel, false);
                SetPanelActive(mainPanel, true);
                BackToMainMenu();
            });
        }

        // 创建房间弹窗按钮
        if (copyCodeButton != null)
        {
            copyCodeButton.onClick.AddListener(CopyRoomCodeToClipboard);
        }

        if (closeCreateRoomPopupButton != null)
        {
            closeCreateRoomPopupButton.onClick.AddListener(() => {
                SetPanelActive(createRoomPopup, false);
                // 如果正在连接，断开连接
                if (Unity.Netcode.NetworkManager.Singleton != null &&
                    Unity.Netcode.NetworkManager.Singleton.IsListening)
                {
                    Unity.Netcode.NetworkManager.Singleton.Shutdown();
                }
            });
        }

        // 加入房间弹窗按钮
        if (confirmJoinButton != null)
        {
            confirmJoinButton.onClick.AddListener(JoinRoom);
        }

        if (closeJoinRoomPopupButton != null)
        {
            closeJoinRoomPopupButton.onClick.AddListener(() => {
                SetPanelActive(joinRoomPopup, false);
                // 如果正在连接，断开连接
                if (Unity.Netcode.NetworkManager.Singleton != null &&
                    Unity.Netcode.NetworkManager.Singleton.IsListening)
                {
                    Unity.Netcode.NetworkManager.Singleton.Shutdown();
                }
            });
        }

        // 难度按钮可以在Inspector中直接绑定OnDifficultyButtonClicked方法并传入对应难度值
    }
    #endregion

    #region 按钮事件处理
    // 单人模式按钮点击
    public void OnSingleModeButtonClicked()
    {
        Debug.Log("单人模式按钮点击!");
        ShowDifficultyPanel();
    }

    // 多人模式按钮点击
    public void OnMultiModeButtonClicked()
    {
        Debug.Log("多人模式按钮点击!");
        ShowTwoPlayerRoomPanel();
    }

    // 显示双人房间面板
    private void ShowTwoPlayerRoomPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(twoPlayerRoomPanel, true);
    }

    // 显示难度选择面板
    private void ShowDifficultyPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(difficultyPanel, true);
    }

    // 难度按钮点击处理 - 可从Inspector直接绑定并传入参数
    public void OnDifficultyButtonClicked(int emptyCount)
    {
        StartGameWithDifficulty(emptyCount);
    }

    // 开始设置
    public void OpenSettingsPanel()
    {
        Debug.Log("打开设置面板");
        HideAllPanels();
        SetPanelActive(optionPanel, true);
    }

    // 返回主菜单
    public void BackToMainMenu()
    {
        Debug.Log("返回主菜单 - 开始清理网络资源和游戏状态");

        // 0. 特殊检查：确保多人游戏面板始终被关闭
        if (multiPlayerGamePanel != null && multiPlayerGamePanel.activeSelf)
        {
            Debug.Log("发现多人游戏面板仍处于激活状态，强制关闭");
            multiPlayerGamePanel.SetActive(false);
        }

        // 1. 暂停计时器
        if (timerScript != null)
        {
            timerScript.PauseTimer();
        }

        // 2. 确保多人游戏面板明确关闭
        if (multiPlayerGamePanel != null)
        {
            multiPlayerGamePanel.SetActive(false);
        }

        if (twoPlayerRoomPanel != null)
        {
            twoPlayerRoomPanel.SetActive(false);
        }

        // 3. 关闭所有弹窗
        if (createRoomPopup != null)
        {
            createRoomPopup.SetActive(false);
        }

        if (joinRoomPopup != null)
        {
            joinRoomPopup.SetActive(false);
        }

        // 4. 处理网络连接关闭
        // 首先检查是否有网络连接正在进行
        bool wasConnected = false;

        // 优先使用NetworkManagerUI进行断开连接
        if (networkManagerUI != null)
        {
            if (networkManagerUI.IsConnected())
            {
                Debug.Log("通过NetworkManagerUI关闭网络连接...");
                wasConnected = true;
                networkManagerUI.DisconnectFromNetwork();

                // 给NetworkManager一些时间来清理资源
                StartCoroutine(DelayedCleanupAfterNetworkShutdown());
            }
        }
        // 备用方案：直接使用NetworkManager
        else if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            // 检查是否有活跃的连接
            if (Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                Debug.Log("直接关闭NetworkManager连接...");
                wasConnected = true;

                // 关闭网络连接
                Unity.Netcode.NetworkManager.Singleton.Shutdown();

                // 给NetworkManager一些时间来清理资源
                StartCoroutine(DelayedCleanupAfterNetworkShutdown());
            }
        }

        // 5. 隐藏所有其他面板
        HideAllPanels();

        // 5.1 清理数独棋盘实例（如果游戏管理器有这样的引用）
        if (gameManager != null)
        {
            // GameManager有一个currentBoardInstance字段，可以直接销毁它
            if (gameManager.currentBoardInstance != null)
            {
                Debug.Log("销毁GameManager中的currentBoardInstance");
                Destroy(gameManager.currentBoardInstance);
                gameManager.currentBoardInstance = null;
            }

            // 重置其他GameManager状态
            // 使用反射访问和重置isNetworkGame字段（如果需要）
            try
            {
                System.Reflection.FieldInfo networkGameField = gameManager.GetType().GetField("isNetworkGame",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (networkGameField != null)
                {
                    Debug.Log("重置GameManager.isNetworkGame为false");
                    networkGameField.SetValue(gameManager, false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"重置GameManager网络状态时出现异常: {ex.Message}");
            }
        }

        // 6. 显示主菜单
        SetPanelActive(mainPanel, true);

        Debug.Log("已返回主菜单" + (wasConnected ? "，并关闭了网络连接" : ""));
    }

    // 延迟清理网络资源的协程
    private System.Collections.IEnumerator DelayedCleanupAfterNetworkShutdown()
    {
        // 等待一小段时间让NetworkManager完成清理
        yield return new WaitForSeconds(0.5f);

        // 确保NetworkGameManager引用被重置或清理
        if (networkGameManager != null)
        {
            // 如果该对象在DontDestroyOnLoad中，可能需要处理一些特殊逻辑
            try
            {
                // 检查NetworkGameManager是否有所有者并且是否需要销毁
                Unity.Netcode.NetworkObject networkObject = networkGameManager.GetComponent<Unity.Netcode.NetworkObject>();

                if (networkObject != null && networkObject.IsOwner)
                {
                    Debug.Log("清理NetworkGameManager状态");
                    // 如果有自定义清理方法，可以在这里调用
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"清理NetworkGameManager时出现异常: {ex.Message}");
            }
        }

        // 查找场景中可能存在的任何网络相关对象并处理
        Unity.Netcode.NetworkObject[] networkObjects = FindObjectsByType<Unity.Netcode.NetworkObject>(FindObjectsSortMode.None);
        foreach (var netObj in networkObjects)
        {
            // 检查这些对象是否应该在返回主菜单时被销毁
            // 避免销毁DontDestroyOnLoad场景中的对象和带有Persistent标签的对象
            if (netObj.gameObject.scene.name != "DontDestroyOnLoad" && !netObj.gameObject.CompareTag("Persistent"))
            {
                Debug.Log($"清理残留的网络对象: {netObj.name}");
                Destroy(netObj.gameObject);
            }
        }

        // 清理可能的数独板引用
        if (networkGameManager != null)
        {
            // 手动清理可能的数独板引用
            GameObject localBoardInstance = GameObject.Find("LocalPlayerSudokuBoard");
            GameObject remoteBoardInstance = GameObject.Find("RemotePlayerSudokuBoard");

            if (localBoardInstance != null)
            {
                Debug.Log("清理本地玩家数独板引用");
                Destroy(localBoardInstance);
            }

            if (remoteBoardInstance != null)
            {
                Debug.Log("清理远程玩家数独板引用");
                Destroy(remoteBoardInstance);
            }
            
            networkGameManager.ClearInstance();
        }

        // 清理可能留下的其他数独相关对象
        // GameObject[] sudokuObjects = GameObject.FindGameObjectsWithTag("SudokuBoard");
        // foreach (var obj in sudokuObjects)
        // {
        //     Debug.Log($"清理数独相关对象: {obj.name}");
        //     Destroy(obj);
        // }

        // 完成后强制GC回收
        System.GC.Collect();

        Debug.Log("网络资源清理完成");
    }

    // 重新开始当前游戏
    public void RestartGame()
    {
        Debug.Log("重新开始游戏");

        // 隐藏胜利面板（如果可见）
        SetPanelActive(victoryPanel, false);

        // 重置并启动计时器
        if (timerScript != null)
        {
            timerScript.ResetTimer();
            timerScript.StartTimer();
        }

        // 使用当前难度重新开始游戏
        StartGameWithDifficulty(currentDifficulty);
    }

    // 游戏退出
    private void QuitGame()
    {
        Debug.Log("退出游戏按钮点击. 正在退出游戏...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 编辑器中停止运行
#else
        Application.Quit(); // 打包后真正退出程序
#endif
    }
    #endregion

    #region 多人游戏功能
    // 显示创建房间弹窗
    private void ShowCreateRoomPopup()
    {
        SetPanelActive(createRoomPopup, true);
        if (createRoomTitle != null) createRoomTitle.text = "Creating the room...";
        if (roomCodeText != null && roomCodeText.gameObject != null) roomCodeText.gameObject.SetActive(false);
        if (copyCodeButton != null && copyCodeButton.gameObject != null) copyCodeButton.gameObject.SetActive(false);
        if (createRoomLoadingAnimation != null) createRoomLoadingAnimation.SetActive(true);

        // 开始创建房间
        CreateRoom();
    }

    // 显示加入房间弹窗
    private void ShowJoinRoomPopup()
    {
        SetPanelActive(joinRoomPopup, true);
        if (joinRoomTitle != null) joinRoomTitle.text = "Join the room";
        if (roomCodeInputField != null) roomCodeInputField.text = "";
        if (joinStatusText != null) joinStatusText.text = "Please enter the room code";
        if (joinRoomLoadingAnimation != null) joinRoomLoadingAnimation.SetActive(false);
        if (confirmJoinButton != null) confirmJoinButton.interactable = true;
    }

    // 创建多人游戏房间
    private void CreateRoom()
    {
        if (networkManagerUI != null)
        {
            // 委托给NetworkManagerUI处理
            networkManagerUI.CreateRoom();
        }
        else
        {
            Debug.LogError("NetworkManagerUI未找到，无法创建房间!");
            if (createRoomLoadingAnimation != null) createRoomLoadingAnimation.SetActive(false);
            if (createRoomTitle != null) createRoomTitle.text = "Failed to create";
            if (roomCodeText != null && roomCodeText.gameObject != null)
            {
                roomCodeText.gameObject.SetActive(true);
                roomCodeText.text = "Didn't find network";
            }
        }
    }

    // 加入多人游戏房间
    private void JoinRoom()
    {
        if (networkManagerUI != null)
        {
            // 委托给NetworkManagerUI处理
            networkManagerUI.JoinRoom();
        }
        else
        {
            Debug.LogError("NetworkManagerUI未找到，无法加入房间!");
            if (joinRoomLoadingAnimation != null) joinRoomLoadingAnimation.SetActive(false);
            if (joinStatusText != null) joinStatusText.text = "Didn't find networkManager";
        }
    }

    // 复制房间代码到剪贴板
    private void CopyRoomCodeToClipboard()
    {
        if (networkManagerUI != null)
        {
            networkManagerUI.CopyRoomCodeToClipboard();
        }
        else if (roomCodeText != null)
        {
            // 直接复制文本
            GUIUtility.systemCopyBuffer = roomCodeText.text;
            Debug.Log($"房间代码已复制到剪贴板: {roomCodeText.text}");
        }
    }

    // 显示多人游戏面板
    public void ShowMultiplayerGamePanel()
    {
        Debug.Log("显示多人游戏面板");
        HideAllPanels();

        // 确保关闭所有弹窗
        if (createRoomPopup != null) createRoomPopup.SetActive(false);
        if (joinRoomPopup != null) joinRoomPopup.SetActive(false);

        // 确保关闭加载动画
        if (createRoomLoadingAnimation != null) createRoomLoadingAnimation.SetActive(false);
        if (joinRoomLoadingAnimation != null) joinRoomLoadingAnimation.SetActive(false);

        // 显示多人游戏面板
        if (multiPlayerGamePanel != null)
        {
            multiPlayerGamePanel.SetActive(true);
            Debug.Log("多人游戏面板已激活");
        }
        else
        {
            Debug.LogError("多人游戏面板引用为空!");
        }
    }
    #endregion

    #region 游戏流程控制
    // 根据难度开始游戏
    private void StartGameWithDifficulty(int emptyCount)
    {
        Debug.Log($"开始难度为 {emptyCount} 的游戏");

        // 保存当前选择的难度
        currentDifficulty = emptyCount;

        // 隐藏难度选择面板
        SetPanelActive(difficultyPanel, false);

        // 显示游戏相关面板
        ShowGamePanels();

        // 创建数独盘，并放置在适当位置
        Vector2 boardPosition = new Vector2(0f, 0f); // 居中位置
        GameObject boardInstance = gameManager.CreateAndSetupSudokuBoard(boardPosition);

        if (boardInstance == null)
        {
            Debug.LogError("创建数独盘失败!");
            return;
        }

        // 使用延迟调用确保UI组件已经初始化
        Invoke(nameof(InitializeNewGame), 0.1f);
    }

    // 显示游戏相关面板
    private void ShowGamePanels()
    {
        SetPanelActive(gamePanel, true);
        SetPanelActive(skillPanel, true);
        SetPanelActive(hintPanel, true);
        SetPanelActive(timerPanel, true);

        // 启动计时器
        if (timerScript != null)
        {
            timerScript.ResetTimer();
            timerScript.StartTimer();
        }
    }

    // 延迟初始化新游戏 - 避免UI渲染和数据初始化顺序问题
    private void InitializeNewGame()
    {
        if (gameManager != null)
        {
            int newSeed = Random.Range(1, 100000);
            Debug.Log($"设置随机种子: {newSeed}");
            gameManager.StartNewGame(newSeed, currentDifficulty);
        }
        else
        {
            Debug.LogError("GameManager引用丢失，无法开始游戏!");
        }
    }
    #endregion

    #region 辅助方法
    // 设置面板激活状态
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    // 隐藏所有面板
    private void HideAllPanels()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(gamePanel, false);
        SetPanelActive(difficultyPanel, false);
        SetPanelActive(victoryPanel, false);
        SetPanelActive(skillPanel, false);
        SetPanelActive(optionPanel, false);
        SetPanelActive(timerPanel, false);
        SetPanelActive(hintPanel, false);
        SetPanelActive(twoPlayerRoomPanel, false);
        SetPanelActive(multiPlayerGamePanel, false);
        SetPanelActive(createRoomPopup, false);
        SetPanelActive(joinRoomPopup, false);
    }
    #endregion
}