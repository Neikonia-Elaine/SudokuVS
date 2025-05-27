using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// 网络游戏管理器 - 修改版
/// 保留双棋盘功能但移除玩家角色区分
/// 负责管理数独游戏的网络同步和多人游戏界面
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Game Settings")]
    [SerializeField] private int defaultEmptyCount = 30; // 默认30个挖空格子

    [Header("Multiplayer UI")]
    [SerializeField] private GameObject twoPlayerGamePanel;
    [SerializeField] private Transform localPlayerBoardContainer; // 本地玩家棋盘容器
    [SerializeField] private Transform remoteBoardContainer; // 远程玩家棋盘容器

    [Header("Sudoku Prefab")]
    [SerializeField] private GameObject sudokuBoardPrefab;

    [Header("远程玩家棋盘设置")]
    [SerializeField] private Color remoteMovedCellColor = new Color(0.2f, 0.6f, 1f, 0.8f); // 远程玩家移动单元格颜色
    [SerializeField] private Color remoteConflictCellColor = new Color(0.9f, 0.3f, 0.3f, 0.7f); // 远程玩家冲突单元格颜色
    [SerializeField] private float remoteBoardScale = 0.33f; // 远程玩家数独大小比例


    [SerializeField] private MenuManager menuManager; 

    // 表示一个移动操作，实现 IEquatable 接口以满足 NetworkList 的要求
    public struct MoveData : INetworkSerializable, System.IEquatable<MoveData>
    {
        public int row;
        public int col;
        public int value;
        public ulong playerId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref row);
            serializer.SerializeValue(ref col);
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref playerId);
        }

        // 实现 IEquatable<MoveData> 接口
        public bool Equals(MoveData other)
        {
            return row == other.row &&
                   col == other.col &&
                   value == other.value &&
                   playerId == other.playerId;
        }

        // 重写 Object.Equals 方法
        public override bool Equals(object obj)
        {
            if (obj is MoveData data)
            {
                return Equals(data);
            }
            return false;
        }

        // 重写 GetHashCode 方法
        public override int GetHashCode()
        {
            return row.GetHashCode() ^
                  (col.GetHashCode() << 2) ^
                  (value.GetHashCode() >> 2) ^
                  (playerId.GetHashCode() >> 1);
        }
    }

    // 游戏状态
    public enum GameState
    {
        Waiting,
        Playing,
        Finished,
    }

    // 网络变量
    private NetworkVariable<GameState> currentState = new NetworkVariable<GameState>(
        GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> gameRandomSeed = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> currentEmptyCount = new NetworkVariable<int>(
        30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 存储所有移动操作的网络列表
    private NetworkList<MoveData> moves;

    // 多人游戏UI对象引用
    private GameObject localBoardInstance;
    private GameObject remoteBoardInstance;
    private SudokuGridSpawner localGridSpawner;
    private SudokuGridSpawner remoteGridSpawner;

    // 追踪远程玩家移动的单元格
    private HashSet<Vector2Int> remoteMovedCells = new HashSet<Vector2Int>();

    public int gameRandomSeedInt = 0;

    private void Awake()
    {
        moves = new NetworkList<MoveData>();

        // 确保多人游戏面板最初是隐藏的
        if (twoPlayerGamePanel != null)
        {
            twoPlayerGamePanel.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 注册网络变量变化回调
        moves.OnListChanged += OnMovesChanged;
        currentState.OnValueChanged += OnGameStateChanged;
        gameRandomSeed.OnValueChanged += OnRandomSeedChanged;
        currentEmptyCount.OnValueChanged += OnEmptyCountChanged;

        // 监听客户端连接事件
        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // if (IsServer)
        // {
        //     // 服务器端初始化游戏
        //     InitializeGame();
        // }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // 取消事件监听
        // if (NetworkManager.Singleton != null)
        // {
        //     NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        // }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        // 清理资源
        if (localBoardInstance != null)
        {
            Destroy(localBoardInstance);
        }

        if (remoteBoardInstance != null)
        {
            Destroy(remoteBoardInstance);
        }
    }

    public void ClearInstance()
    {
        // 清理资源
        if (localBoardInstance != null)
        {
            Destroy(localBoardInstance);
            localBoardInstance = null;
        }

        if (remoteBoardInstance != null)
        {
            Destroy(remoteBoardInstance);
            remoteBoardInstance = null;
        }
    }

    // 当客户端连接时调用
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"客户端已连接: {clientId}");

        // 如果客户端数量为2，说明双方都已连接，显示双人游戏面板
        if (NetworkManager.Singleton.ConnectedClientsIds.Count == 2)
        {
            Debug.Log($"准备初始化游戏界面");
            ShowMultiplayerGamePanel();
        }
    }

    // 显示多人游戏面板
    private void ShowMultiplayerGamePanel()
    {
        if (twoPlayerGamePanel != null)
        {
            twoPlayerGamePanel.SetActive(true);

            // 创建数独界面
            SetupMultiplayerBoards();
        }
    }

    private void InitializeGame()
    {
        if (!IsServer) return;

        Debug.Log("服务器初始化游戏...");

        // 只在首次初始化时设置随机种子，避免重复设置
        if (gameRandomSeed.Value == 0)
        {
            int newSeed = Random.Range(1, 100000);
            gameRandomSeed.Value = newSeed;
            gameRandomSeedInt = newSeed;
            Debug.Log($"设置随机种子: {newSeed}");
        }
        else
        {
            Debug.Log($"使用现有随机种子: {gameRandomSeed.Value}");
            // 确保使用已设置的种子
            //Random.InitState(gameRandomSeed.Value);
            gameRandomSeedInt = gameRandomSeed.Value;
        }

        // 固定空格数量为30，避免变化
        currentEmptyCount.Value = defaultEmptyCount;
        Debug.Log($"设置空格数量: {defaultEmptyCount}");

        // 设置游戏状态
        currentState.Value = GameState.Playing;
        Debug.Log("游戏状态设置为: Playing");

        //确保服务器端（主机）自己也初始化游戏
        if (gameManager != null)
        {
            // 在服务器上也创建游戏界面
            if (localBoardInstance == null || remoteBoardInstance == null)
            {
                Debug.Log("服务器创建多人游戏界面...");
                SetupMultiplayerBoards();
            }
        
            // 延迟初始化游戏，确保UI已准备好
            StartCoroutine(DelayedHostStartGame());
        }
        
        // 告诉所有客户端游戏已开始
        BroadcastGameDataClientRpc();
    }

    // 主机延迟开始游戏
    private System.Collections.IEnumerator DelayedHostStartGame()
    {
        yield return new WaitForSeconds(0.2f); // 短暂延迟
    
        Debug.Log("主机初始化游戏...");
        //Random.InitState(gameRandomSeed.Value);
        gameManager.StartNewGame(gameRandomSeedInt, currentEmptyCount.Value);
    
        // 额外刷新UI
        yield return new WaitForSeconds(0.5f);
        gameManager.UpdateAllCellsUI();
        
        // 更新远程玩家棋盘的固定单元格
        UpdateRemoteFixedCells();
    }

    // 创建多人游戏的数独板
    private void SetupMultiplayerBoards()
    {
        // 创建本地玩家的数独板
        CreateLocalPlayerSudokuBoard();

        // 创建远程玩家的小型数独板
        CreateRemotePlayerSudokuBoard();
    }


    // 创建本地玩家数独板
    private void CreateLocalPlayerSudokuBoard()
    {
        Debug.Log("创建本地玩家数独板");

        // 检查容器
        if (localPlayerBoardContainer == null)
        {
            Debug.LogError("本地玩家数独容器为空!");
            return;
        }

        // 使用GameManager创建数独板
        if (gameManager != null)
        {
            // 设置本地玩家数独板的位置（居中）
            Vector2 boardPosition = Vector2.zero;

            // 重要：确保localBoardInstance的父对象是正确的
            localBoardInstance = gameManager.CreateAndSetupSudokuBoard(boardPosition);

            if (localBoardInstance != null)
            {
                // 强制设置父对象为本地容器
                localBoardInstance.transform.SetParent(localPlayerBoardContainer, false);

                // 重置位置和缩放，确保适当尺寸
                localBoardInstance.transform.localPosition = Vector3.zero;
                localBoardInstance.transform.localScale = Vector3.one;

                // 调整 RectTransform 来确保正确的布局
                RectTransform rt = localBoardInstance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero; // 使用容器的尺寸
                }

                Debug.Log("本地玩家数独板已创建");

                // 获取GridSpawner并设置到GameManager
                localGridSpawner = localBoardInstance.GetComponentInChildren<SudokuGridSpawner>();
                if (localGridSpawner != null)
                {
                    gameManager.SetGridSpawner(localGridSpawner);
                }
                
                // if (gameManager != null)
                // {
                //     gameManager.StartNewGame(currentDifficulty);
                // }
            }
            else
            {
                Debug.LogError("创建本地玩家数独板失败!");
            }
        }
        else
        {
            Debug.LogError("GameManager引用为空，无法创建数独板!");
        }
    }

    // 创建远程玩家的小数独板
    private void CreateRemotePlayerSudokuBoard()
    {
        Debug.Log("创建远程玩家数独板");

        // 检查容器
        if (remoteBoardContainer == null)
        {
            Debug.LogError("远程玩家数独容器为空!");
            return;
        }

        // 销毁现有实例
        if (remoteBoardInstance != null)
        {
            Destroy(remoteBoardInstance);
        }

        // 使用与本地玩家相同的预制体创建远程玩家数独板
        if (gameManager != null && sudokuBoardPrefab != null) // 使用类中的sudokuBoardPrefab，不依赖gameManager的
        {
            // 直接创建远程玩家棋盘，不经过gameManager
            remoteBoardInstance = Instantiate(sudokuBoardPrefab);

            // 明确设置父对象
            remoteBoardInstance.transform.SetParent(remoteBoardContainer, false);

            // 显式设置位置和缩放
            remoteBoardInstance.transform.localPosition = Vector3.zero;
            remoteBoardInstance.transform.localScale = new Vector3(remoteBoardScale, remoteBoardScale, 1f);

            // 调整 RectTransform
            RectTransform rt = remoteBoardInstance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero; // 使用容器的尺寸
            }

            Debug.Log("远程玩家数独板已创建");

            // 获取GridSpawner
            remoteGridSpawner = remoteBoardInstance.GetComponentInChildren<SudokuGridSpawner>();
            if (remoteGridSpawner == null)
            {
                Debug.LogError("无法找到远程GridSpawner组件!");
                return;
            }

            // 清空远程玩家移动记录
            remoteMovedCells.Clear();

            // 初始化远程玩家数独板
            UpdateRemoteFixedCells();

            // 禁用远程玩家数独的交互
            DisableRemoteBoardInteraction();
        }
        else
        {
            Debug.LogError("无法创建远程玩家数独板：缺少必要引用!");
        }
    }


    // 更新远程玩家数独板上的固定单元格显示
    private void UpdateRemoteFixedCells()
    {
        if (remoteGridSpawner == null || gameManager == null) return;

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                CellManager cellManager = remoteGridSpawner.GetCellManager(row, col);
                if (cellManager != null)
                {
                    // 设置数字显示（与本地玩家棋盘相同的数字）
                    int value = gameManager.GetCellValue(row, col);
                    cellManager.SetNumber(value);

                    // 固定单元格使用与本地相同的颜色策略
                    if (gameManager.IsFixedCell(row, col))
                    {
                        cellManager.SetColor(gameManager.fixedNumberColor);
                    }
                }
            }
        }
    }

    // 禁用远程玩家数独的交互功能
    private void DisableRemoteBoardInteraction()
    {
        if (remoteBoardInstance == null) return;

        // 禁用远程玩家数独上的所有Button和Input组件
        CellManager[] cellManagers = remoteBoardInstance.GetComponentsInChildren<CellManager>();
        foreach (CellManager cellManager in cellManagers)
        {
            cellManager.canClick = false;
        }
    }

    // 游戏状态变化回调
    private void OnGameStateChanged(GameState previous, GameState current)
    {
        Debug.Log($"游戏状态从 {previous} 变更为 {current}");

        if (current == GameState.Playing)
        {
            // 如果是客户端，使用相同的随机种子初始化游戏
            if (IsClient)
            {
                // 确保UI界面已创建
                // if (localBoardInstance == null || remoteBoardInstance == null)
                // {
                //     Debug.Log("创建游戏界面");
                //     SetupMultiplayerBoards();
                // }
                //
                // Random.InitState(gameRandomSeed.Value);
                // gameManager.StartNewGame(currentEmptyCount.Value);
            }
        }
    }

    // 随机种子变化回调
    private void OnRandomSeedChanged(int previous, int current)
    {
        Debug.Log($"游戏随机种子从 {previous} 变更为 {current}");

        // 设置随机种子，确保所有客户端生成相同的数独谜题
        // gameRandomSeed.Value = current;
        //Random.InitState(current);
        gameRandomSeedInt = current;
    }

    // 空格数量变化回调
    private void OnEmptyCountChanged(int previous, int current)
    {
        Debug.Log($"游戏空格数量从 {previous} 变更为 {current}");
    }
    
 

    /// <summary>
    /// 修改技能ID-并广播给其他人
    /// </summary>
    /// <param name="skillId"></param>
    public void SetSkillId(int skillId)
    {
        Debug.Log($"发送数据SkillId: {skillId}");
        if (IsServer)
        {
            SendDataToAllClientRpc(skillId);
        }
        else
        {
            SendDataToServerRpc(skillId);
        }
        
    }
    
    // 客户端调用RPC请求修改
    [ServerRpc(RequireOwnership = false)]
    public void SendDataToServerRpc(int data)
    {
        if (!IsServer)
        {
            return;
        }
        Debug.Log($"服务器收到数据: {data}");
        OnSkillChanged(data);
        // 服务器可以再转发给其他客户端
    }
    
    
    // 服务器调用（广播给所有客户端）
    [ClientRpc]
    public void SendDataToAllClientRpc(int data)
    {
        if (IsServer)
        {
            return;
        }
        Debug.Log($"客户端收到数据: {data}");
        OnSkillChanged(data);
    }

    
    // 技能变化回调
    private void OnSkillChanged(int current)
    {
        //Debug.Log($"游戏技能 变更为 {current}");
        menuManager.skillIdTxt.text = $"收到的数据：{current.ToString()}";
    }

    
    
    // 移动列表变化回调
    private void OnMovesChanged(NetworkListEvent<MoveData> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<MoveData>.EventType.Add)
        {
            MoveData move = moves[moves.Count - 1];

            // 忽略玩家自己的移动（已经在本地处理过）
            if (move.playerId != NetworkManager.Singleton.LocalClientId)
            {
                // 在本地应用其他玩家的移动
                //gameManager.SetCellValue(move.row, move.col, move.value);
                Debug.Log($"应用玩家 {move.playerId} 的移动: ({move.row}, {move.col}) = {move.value}");

                // 更新远程玩家的小数独视图
                UpdateRemoteMove(move.row, move.col, move.value);
            }
        }
    }

    // 更新远程玩家数独上的移动
    private void UpdateRemoteMove(int row, int col, int value)
    {
        if (remoteGridSpawner == null) return;

        CellManager cellManager = remoteGridSpawner.GetCellManager(row, col);
        Vector2Int cellPos = new Vector2Int(row, col);

        if (cellManager != null)
        {
            // 更新数字
            cellManager.SetNumber(value);

            // 获取或创建背景图像
            Image bgImage = AddCellBackgroundImage(cellManager.gameObject);

            // 设置背景颜色来表示远程玩家的移动
            if (value > 0 && bgImage != null)
            {
                // 检查是否有冲突
                bool hasConflict = gameManager.HasConflict(row, col, value);

                if (hasConflict)
                {
                    bgImage.color = remoteConflictCellColor;
                    cellManager.SetColor(gameManager.conflictNumberColor);
                }
                else
                {
                    bgImage.color = remoteMovedCellColor;
                    cellManager.SetColor(gameManager.normalNumberColor);
                }

                // 记录远程玩家已移动的单元格
                remoteMovedCells.Add(cellPos);
            }
            else if (bgImage != null)
            {
                // 清除移动（值为0）
                bgImage.color = Color.clear;
                remoteMovedCells.Remove(cellPos);
            }
        }
    }

    // 为单元格添加背景图像组件
    private Image AddCellBackgroundImage(GameObject cellObject)
    {
        // 查找是否已有背景图像组件
        Image bgImage = null;
        Transform bgTransform = cellObject.transform.Find("CellBackground");

        if (bgTransform != null)
        {
            bgImage = bgTransform.GetComponent<Image>();
        }

        // 如果没有找到，创建一个新的
        if (bgImage == null)
        {
            GameObject bgGO = new GameObject("CellBackground");
            bgGO.transform.SetParent(cellObject.transform, false);
            bgImage = bgGO.AddComponent<Image>();
            bgImage.color = Color.clear; // 默认透明

            // 设置为最下层
            bgGO.transform.SetAsFirstSibling();

            // 设置为填充父对象
            RectTransform bgRT = bgImage.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
        }

        return bgImage;
    }

    // 客户端RPC - 服务器调用广播游戏数据到所有客户端
    [ClientRpc]
    private void BroadcastGameDataClientRpc()
    {
        if (IsServer) return; // 服务器已经有游戏数据
        
        Debug.Log("客户端接收到游戏数据广播");
        

        // 确保清除任何进行中的操作
        StopAllCoroutines();

        // 客户端需要初始化相同的游戏
        if (gameManager != null)
        {
            Debug.Log($"客户端使用随机种子 {gameRandomSeedInt} 和空格数量 {currentEmptyCount.Value} 初始化游戏");

            // 设置随机种子
            //Random.InitState(gameRandomSeed.Value);

            // 创建UI界面（如果需要）
            if (localBoardInstance == null || remoteBoardInstance == null)
            {
                Debug.Log("客户端创建多人游戏界面...");
                SetupMultiplayerBoards();
            }
            else
            {
                Debug.Log("客户端使用现有游戏界面");
            }

            // 等待一帧，确保UI已创建
            StartCoroutine(DelayedStartGame());
        }
        else
        {
            Debug.LogError("客户端没有GameManager引用，无法初始化游戏!");
        }
    }

    // 延迟开始游戏，确保UI已创建
    private System.Collections.IEnumerator DelayedStartGame()
    {
        yield return new WaitForSeconds(0.2f); // 等待一小段时间，确保UI已创建

        if (gameManager != null)
        {
            Debug.Log("客户端延迟初始化游戏...");
            // 使用正确的随机种子和空格数量启动游戏
            //Random.InitState(gameRandomSeed.Value);
            gameManager.StartNewGame(gameRandomSeedInt, currentEmptyCount.Value);

            // 再次更新UI，确保显示正确
            yield return new WaitForSeconds(0.5f);
            gameManager.UpdateAllCellsUI();

            // 更新远程玩家棋盘的固定单元格
            UpdateRemoteFixedCells();

            Debug.Log("客户端游戏初始化完成");
        }
        else
        {
            Debug.LogError("DelayedStartGame: gameManager为空");
        }
    }

    // 公共方法 - 供本地GameManager调用，通知网络有新的移动
    public void NotifyMove(int row, int col, int value)
    {
        // 只有在联网模式下才发送RPC
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log($"NotifyMove: 通知网络有新的移动 ({row}, {col}) = {value}");

            if (IsServer)
            {
                // 服务器直接添加到移动列表
                RecordMove(row, col, value, NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                // 客户端通过 RPC 通知服务器
                MakeMoveServerRpc(row, col, value);
            }
        }
    }

    // 记录移动到网络列表
    private void RecordMove(int row, int col, int value, ulong playerId)
    {
        moves.Add(new MoveData
        {
            row = row,
            col = col,
            value = value,
            playerId = playerId
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void MakeMoveServerRpc(int row, int col, int value, ServerRpcParams rpcParams = default)
    {
        // 获取发送RPC的客户端ID
        ulong clientId = rpcParams.Receive.SenderClientId;

        // 记录移动
        RecordMove(row, col, value, clientId);
    }

    // 重置游戏 - 只有主机可以调用
    public void ResetGame(int emptyCount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("只有服务器可以重置游戏!");
            return;
        }

        Debug.Log($"重置游戏，空格数量: {emptyCount}");

        // 更新空格数量
        currentEmptyCount.Value = emptyCount;

        // 清空移动列表
        moves.Clear();

        // 重新初始化游戏
        InitializeGame();

        // 手动刷新界面
        RefreshGameUIClientRpc();
    }

    // 客户端RPC - 用于刷新游戏UI
    [ClientRpc]
    private void RefreshGameUIClientRpc()
    {
        // 不同客户端处理
        if (IsServer)
        {
            // 服务器端已在ResetGame中处理
            Debug.Log("服务器端已在ResetGame中处理UI刷新");
        }
        else
        {
            Debug.Log("收到刷新游戏UI的请求");

            // 确保清除任何进行中的操作
            StopAllCoroutines();
            
            // 客户端处理
            Debug.Log("客户端刷新游戏UI");

            // 确保设置正确的随机种子
            //Random.InitState(gameRandomSeed.Value);

            // 确保UI界面已创建
            if (localBoardInstance == null || remoteBoardInstance == null)
            {
                Debug.Log("客户端因刷新请求创建游戏界面");
                SetupMultiplayerBoards();
            }

            // 重新初始化游戏
            if (gameManager != null)
            {
                StartCoroutine(DelayedStartGame());
            }
            else
            {
                Debug.LogError("无法刷新游戏: GameManager为空");
            }
        }
    }

    // 检查游戏是否完成
    public void CheckGameCompleted()
    {
        if (!IsServer) return;

        // 获取GameManager来检查游戏是否完成
        bool isCompleted = true;

        // 检查所有格子是否填满且无冲突
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int value = gameManager.GetCellValue(row, col);
                if (value == 0 || gameManager.HasConflict(row, col, value))
                {
                    isCompleted = false;
                    break;
                }
            }
            if (!isCompleted) break;
        }

        if (isCompleted)
        {
            currentState.Value = GameState.Finished;
            GameCompletedClientRpc();
        }
    }

    [ClientRpc]
    private void GameCompletedClientRpc()
    {
        Debug.Log("游戏已完成！");
        // 显示游戏结束UI等
    }

    // 设置GameManager引用 - 用于在游戏中途需要更新引用的情况
    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    // 获取当前空格数量
    public int GetCurrentEmptyCount()
    {
        return currentEmptyCount.Value;
    }

    // 获取当前游戏状态
    public GameState GetCurrentGameState()
    {
        return currentState.Value;
    }

    // 开始指定难度的游戏 - 供外部调用
    public void StartGameWithDifficulty(int emptyCount)
    {
        if (!IsServer) return;

        ResetGame(emptyCount);
    }
}