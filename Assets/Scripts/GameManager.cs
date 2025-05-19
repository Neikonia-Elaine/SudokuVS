using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.Netcode;

// 数独游戏核心逻辑
// 生成数独谜题和解法
// 检查游戏完成状态
// 管理游戏状态和数据
// -- 管理数独盘的创建

public class GameManager : MonoBehaviour
{
    // 引用数独网格生成器
    private SudokuGridSpawner gridSpawner;

    // 胜利面板
    public GameObject victoryPanel;

    // 撤回按钮
    public Button undoButton;

    public GameObject currentBoardInstance;
    public GameObject sudokuBoardPrefab;
    public Transform sudokuBoardParent;
    public SkillManager skillManager;

    [Header("颜色设置")]
    public Color fixedNumberColor = new Color(0.2f, 0.2f, 0.6f); // 蓝色
    public Color conflictNumberColor = new Color(0.8f, 0.2f, 0.2f); // 红色
    public Color normalNumberColor = new Color(0.2f, 0.2f, 0.6f); // 黑色

    // 数独数据
    private int[,] solutionGrid = new int[9, 9]; // 完整解决方案
    private int[,] gameGrid = new int[9, 9];     // 当前游戏状态
    private bool[,] fixedCells = new bool[9, 9]; // 标记初始固定数字

    // 冲突高亮追踪
    private List<Vector2Int> conflictHighlightedCells = new List<Vector2Int>(); // 跟踪哪些单元格被高亮为冲突

    // 历史记录 - 用于撤回功能
    private List<MoveRecord> moveHistory = new List<MoveRecord>();
    private int maxHistorySize = 50; // 最大历史记录数量

    // 系统随机数生成器
    private System.Random rng = new System.Random();

    // 添加网络游戏管理器引用
    [Header("Network")]
    [SerializeField] private NetworkGameManager networkGameManager;
    private bool isNetworkGame = false;

    // 撤回操作记录类
    private class MoveRecord
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public int PreviousValue { get; private set; }
        public int NewValue { get; private set; }

        public MoveRecord(int row, int col, int previousValue, int newValue)
        {
            Row = row;
            Col = col;
            PreviousValue = previousValue;
            NewValue = newValue;
        }
    }

    void Start()
    {
        // 隐藏胜利面板（如果有）
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // 设置撤回按钮
        SetupUndoButton();

        // 检测是否处于网络游戏模式
        isNetworkGame = networkGameManager != null && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient;
    }


    // 开始新游戏
    public void StartNewGame(int emptyCount)
    {
        // 清空历史记录
        moveHistory.Clear();

        // 禁用撤回按钮
        if (undoButton != null)
        {
            undoButton.interactable = false;
        }

        // 清除所有冲突高亮
        conflictHighlightedCells.Clear();

        // 生成数独解决方案
        GenerateSudokuSolution();

        // 复制解决方案到游戏网格
        CopySolutionToGame();

        // 挖空指定数量的格子
        CreatePuzzleByRemovingDigits(emptyCount);

        // 更新UI显示
        UpdateAllCellsUI();

        // 将 SudokuGridSpawner 注入给 HintController
        HintController hint = FindFirstObjectByType<HintController>();
        if (hint != null)
        {
            hint.SetGridSpawner(gridSpawner);
        }

        // 隐藏胜利面板（如果有）
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // 清除网格生成器中的选择状态
        if (gridSpawner != null)
        {
            gridSpawner.ClearSelection();
        }
    }

    // 创建数独盘
    public GameObject CreateAndSetupSudokuBoard(Vector2 anchoredPosition)
    {
        if (currentBoardInstance != null)
        {
            Destroy(currentBoardInstance);
        }

        currentBoardInstance = Instantiate(sudokuBoardPrefab, sudokuBoardParent);
        currentBoardInstance.transform.localScale = Vector3.one;

        RectTransform rt = currentBoardInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = anchoredPosition;  // 设置位置
        }

        SudokuGridSpawner spawner = currentBoardInstance.GetComponentInChildren<SudokuGridSpawner>();
        SetGridSpawner(spawner);
        if (skillManager != null)
        {
            skillManager.SetGridSpawner(spawner);
        }

        return currentBoardInstance;
    }

    // 生成数独解决方案
    private void GenerateSudokuSolution()
    {
        // 清空数独网格
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                solutionGrid[i, j] = 0;
            }
        }

        // 使用回溯算法填充数独
        SolveSudoku(0, 0);

        // 为了增加随机性，对生成的数独进行随机变换
        RandomizeSudoku();
    }

    // 使用回溯算法解数独
    private bool SolveSudoku(int row, int col)
    {
        // 如果已经到了最后一列，移到下一行
        if (col == 9)
        {
            col = 0;
            row++;

            // 如果已经到了最后一行，说明数独已解决
            if (row == 9)
                return true;
        }

        // 如果当前位置已有数字，则处理下一个位置
        if (solutionGrid[row, col] != 0)
            return SolveSudoku(row, col + 1);

        // 创建1-9的随机顺序数组
        List<int> numbers = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Shuffle(numbers);

        // 尝试每个数字
        foreach (int num in numbers)
        {
            // 检查当前数字是否可以放在这个位置
            if (IsValidPlacement(row, col, num))
            {
                // 放置数字
                solutionGrid[row, col] = num;

                // 递归解决剩余的数独
                if (SolveSudoku(row, col + 1))
                    return true;

                // 如果后续无法解决，回溯并尝试下一个数字
                solutionGrid[row, col] = 0;
            }
        }

        // 无法解决
        return false;
    }

    // 检查数字是否可以放在指定位置
    private bool IsValidPlacement(int row, int col, int num)
    {
        // 检查行
        for (int c = 0; c < 9; c++)
        {
            if (solutionGrid[row, c] == num)
                return false;
        }

        // 检查列
        for (int r = 0; r < 9; r++)
        {
            if (solutionGrid[r, col] == num)
                return false;
        }

        // 检查3x3方格
        int boxRow = row - row % 3;
        int boxCol = col - col % 3;

        for (int r = boxRow; r < boxRow + 3; r++)
        {
            for (int c = boxCol; c < boxCol + 3; c++)
            {
                if (solutionGrid[r, c] == num)
                    return false;
            }
        }

        // 如果没有冲突，返回true
        return true;
    }

    // 随机打乱数组
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // 随机变换数独以增加多样性
    private void RandomizeSudoku()
    {
        // 简化为随机交换几个数字
        for (int i = 0; i < 10; i++)
        {
            int a = rng.Next(1, 10);
            int b = rng.Next(1, 10);
            if (a != b)
                SwapNumbers(a, b);
        }
    }

    // 交换数独中的两个数字
    private void SwapNumbers(int a, int b)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (solutionGrid[row, col] == a)
                    solutionGrid[row, col] = b;
                else if (solutionGrid[row, col] == b)
                    solutionGrid[row, col] = a;
            }
        }
    }

    // 将解决方案复制到游戏网格
    private void CopySolutionToGame()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                gameGrid[row, col] = solutionGrid[row, col];
                fixedCells[row, col] = false; // 重置固定标记
            }
        }
    }

    // 通过移除数字创建谜题
    private void CreatePuzzleByRemovingDigits(int count)
    {
        // 创建所有单元格的列表
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                positions.Add(new Vector2Int(row, col));
            }
        }

        // 随机打乱位置
        Shuffle(positions);

        // 移除指定数量的数字
        count = Mathf.Min(count, positions.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = positions[i];
            gameGrid[pos.x, pos.y] = 0; // 清空单元格
        }

        // 标记固定的单元格
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                fixedCells[row, col] = gameGrid[row, col] != 0; // 非空格子为固定格子
            }
        }
    }

    // 包含网络同步
    public void SetCellValue(int row, int col, int value)
    {
        // 如果是固定单元格，则不能修改
        if (fixedCells[row, col])
            return;

        // 获取当前值
        int currentValue = gameGrid[row, col];

        // 如果值没有变化，则不进行操作
        if (currentValue == value)
            return;

        // 记录操作历史
        moveHistory.Add(new MoveRecord(row, col, currentValue, value));

        // 如果历史记录超过最大数量，移除最早的记录
        if (moveHistory.Count > maxHistorySize)
        {
            moveHistory.RemoveAt(0);
        }

        // 启用撤回按钮
        if (undoButton != null)
        {
            undoButton.interactable = true;
        }

        // 清除所有冲突高亮
        ClearAllConflictHighlights();

        // 更新游戏网格中的值
        gameGrid[row, col] = value;

        // 更新UI并检查冲突
        UpdateCellUI(row, col);

        // 重新检查整个网格的冲突状态
        RefreshAllConflicts();

        // 检查游戏是否完成
        CheckGameCompletion();

        // 如果是网络游戏，通知网络游戏管理器
        if (isNetworkGame && networkGameManager != null)
        {
            networkGameManager.NotifyMove(row, col, value);
        }
    }


    // 清除选中单元格的值 - 使用SudokuGridSpawner的GetLastSelectedCell方法
    public void ClearSelectedCellValue()
    {
        if (gridSpawner != null)
        {
            Vector2Int selectedCell = gridSpawner.GetLastSelectedCell();
            if (selectedCell.x >= 0 && selectedCell.y >= 0)
            {
                SetCellValue(selectedCell.x, selectedCell.y, 0);
            }
        }
    }

    // 设置撤回按钮
    private void SetupUndoButton()
    {
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(UndoLastMove);
            // 初始时禁用撤回按钮，因为没有操作可撤回
            undoButton.interactable = false;
        }
        else
        {
            Debug.LogWarning("未设置撤回按钮引用!");
        }
    }

    // 撤回上一步操作
    public void UndoLastMove()
    {
        if (moveHistory.Count > 0)
        {
            // 获取最后一次操作记录
            MoveRecord lastMove = moveHistory[moveHistory.Count - 1];
            moveHistory.RemoveAt(moveHistory.Count - 1);

            // 清除所有冲突高亮
            ClearAllConflictHighlights();

            // 恢复到上一个状态
            gameGrid[lastMove.Row, lastMove.Col] = lastMove.PreviousValue;

            // 更新UI
            UpdateCellUI(lastMove.Row, lastMove.Col);

            // 重新检查整个网格的冲突状态
            RefreshAllConflicts();

            Debug.Log($"撤回操作: 位置({lastMove.Row},{lastMove.Col})，从{lastMove.NewValue}恢复到{lastMove.PreviousValue}");

            // 如果没有更多历史记录，禁用撤回按钮
            if (moveHistory.Count == 0 && undoButton != null)
            {
                undoButton.interactable = false;
            }
        }
        else
        {
            Debug.Log("没有操作可撤回");
        }
    }

    // 更新单元格UI（颜色等）
    public void UpdateCellUI(int row, int col)
    {
        // 获取单元格的当前值
        int value = gameGrid[row, col];

        // 检查是否有冲突
        bool hasConflict = HasConflict(row, col, value);

        // 获取对应的CellManager
        CellManager cellManager = gridSpawner.GetCellManager(row, col);
        if (cellManager != null)
        {
            // 设置显示的数字
            cellManager.SetNumber(value);

            // 根据状态设置颜色
            Color textColor;
            if (fixedCells[row, col])
            {
                textColor = fixedNumberColor;
            }
            else if (hasConflict)
            {
                textColor = conflictNumberColor;

                Vector2Int cellPos = new Vector2Int(row, col);
                if (!conflictHighlightedCells.Contains(cellPos))
                {
                    conflictHighlightedCells.Add(cellPos);
                }
            }
            else
            {
                textColor = normalNumberColor;

                // 如果单元格之前被标记为冲突，现在不再冲突，从列表中移除
                Vector2Int cellPos = new Vector2Int(row, col);
                conflictHighlightedCells.Remove(cellPos);
            }

            cellManager.SetColor(textColor);
        }

        // 如果当前数字有冲突，高亮所有相关冲突的数字
        if (hasConflict && value != 0)
        {
            HighlightConflicts(row, col, value);
        }
    }

    // 高亮显示所有冲突的数字
    private void HighlightConflicts(int row, int col, int value)
    {
        // 检查并高亮行中的冲突
        for (int c = 0; c < 9; c++)
        {
            if (c != col && gameGrid[row, c] == value)
            {
                CellManager conflictCell = gridSpawner.GetCellManager(row, c);
                if (conflictCell != null)
                {
                    conflictCell.SetColor(conflictNumberColor);
                    conflictHighlightedCells.Add(new Vector2Int(row, c));
                }

                // 同时设置当前单元格为冲突高亮
                CellManager currentCell = gridSpawner.GetCellManager(row, col);
                if (currentCell != null)
                {
                    currentCell.SetColor(conflictNumberColor);
                    if (!conflictHighlightedCells.Contains(new Vector2Int(row, col)))
                    {
                        conflictHighlightedCells.Add(new Vector2Int(row, col));
                    }
                }
            }
        }

        // 检查并高亮列中的冲突
        for (int r = 0; r < 9; r++)
        {
            if (r != row && gameGrid[r, col] == value)
            {
                CellManager conflictCell = gridSpawner.GetCellManager(r, col);
                if (conflictCell != null)
                {
                    conflictCell.SetColor(conflictNumberColor);
                    conflictHighlightedCells.Add(new Vector2Int(r, col));
                }

                // 同时设置当前单元格为冲突高亮
                CellManager currentCell = gridSpawner.GetCellManager(row, col);
                if (currentCell != null)
                {
                    currentCell.SetColor(conflictNumberColor);
                    if (!conflictHighlightedCells.Contains(new Vector2Int(row, col)))
                    {
                        conflictHighlightedCells.Add(new Vector2Int(row, col));
                    }
                }
            }
        }

        // 检查并高亮3x3方格中的冲突
        int boxRow = row - row % 3;
        int boxCol = col - col % 3;
        for (int r = boxRow; r < boxRow + 3; r++)
        {
            for (int c = boxCol; c < boxCol + 3; c++)
            {
                if ((r != row || c != col) && gameGrid[r, c] == value)
                {
                    CellManager conflictCell = gridSpawner.GetCellManager(r, c);
                    if (conflictCell != null)
                    {
                        conflictCell.SetColor(conflictNumberColor);
                        conflictHighlightedCells.Add(new Vector2Int(r, c));
                    }

                    // 同时设置当前单元格为冲突高亮
                    CellManager currentCell = gridSpawner.GetCellManager(row, col);
                    if (currentCell != null)
                    {
                        currentCell.SetColor(conflictNumberColor);
                        if (!conflictHighlightedCells.Contains(new Vector2Int(row, col)))
                        {
                            conflictHighlightedCells.Add(new Vector2Int(row, col));
                        }
                    }
                }
            }
        }
    }

    // 刷新所有单元格的冲突状态
    private void RefreshAllConflicts()
    {
        // 先清除所有单元格的冲突高亮
        ClearAllConflictHighlights();

        // 重新检查所有单元格是否有冲突，并添加到高亮列表
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int value = gameGrid[row, col];
                if (value > 0 && HasConflict(row, col, value))
                {
                    // 更新冲突状态
                    UpdateCellUI(row, col);
                }
            }
        }
    }

    // 清除所有单元格的冲突高亮
    private void ClearAllConflictHighlights()
    {
        // 遍历并清除所有高亮的单元格
        foreach (Vector2Int cellPos in conflictHighlightedCells)
        {
            CellManager cellManager = gridSpawner.GetCellManager(cellPos.x, cellPos.y);
            if (cellManager != null)
            {
                // 根据单元格类型设置正确的颜色
                if (fixedCells[cellPos.x, cellPos.y])
                {
                    cellManager.SetColor(fixedNumberColor);
                }
                else
                {
                    cellManager.SetColor(normalNumberColor);
                }
            }
        }

        // 清空高亮列表
        conflictHighlightedCells.Clear();
    }

    // 检查单元格的数字是否有冲突（重复）
    public bool HasConflict(int row, int col, int num)
    {
        // 如果单元格为空（值为0），返回false
        if (num == 0)
            return false;

        // 检查行是否有重复
        for (int c = 0; c < 9; c++)
        {
            if (c != col && gameGrid[row, c] == num)
                return true;
        }

        // 检查列是否有重复
        for (int r = 0; r < 9; r++)
        {
            if (r != row && gameGrid[r, col] == num)
                return true;
        }

        // 检查3x3方格是否有重复
        int boxRow = row - row % 3;
        int boxCol = col - col % 3;

        for (int r = boxRow; r < boxRow + 3; r++)
        {
            for (int c = boxCol; c < boxCol + 3; c++)
            {
                if ((r != row || c != col) && gameGrid[r, c] == num)
                    return true;
            }
        }

        // 如果没有冲突，返回false
        return false;
    }

    // 判断是否为固定单元格
    public bool IsFixedCell(int row, int col)
    {
        // 添加边界检查
        if (row < 0 || row >= 9 || col < 0 || col >= 9)
            return true; // 超出范围视为固定单元格

        return fixedCells[row, col];
    }

    // 获取当前游戏网格的值
    public int GetCellValue(int row, int col)
    {
        // 添加边界检查
        if (row < 0 || row >= 9 || col < 0 || col >= 9)
            return 0;

        return gameGrid[row, col];
    }

    // 获取单元格的正确解答值 - 用于验证或提示功能
    public int GetCellSolution(int row, int col)
    {
        return solutionGrid[row, col];
    }

    // 检查游戏是否完成
    private void CheckGameCompletion()
    {
        // 检查所有格子是否填满
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (gameGrid[row, col] == 0)
                    return; // 有空格，游戏未完成

                // 检查是否有冲突
                if (HasConflict(row, col, gameGrid[row, col]))
                    return; // 有冲突，游戏未完成
            }
        }

        // 所有格子都已填满且没有冲突，游戏完成
        Debug.Log("恭喜！数独已完成！");
        ShowVictoryPanel();
        // 通知网络管理器游戏完成
        if (isNetworkGame && networkGameManager != null)
        {
            networkGameManager.CheckGameCompleted();
        }
    }

    // 显示胜利面板
    private void ShowVictoryPanel()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }

    // 刷新整个网格UI
    public void UpdateAllCellsUI()
    {
        // 清除所有冲突高亮
        conflictHighlightedCells.Clear();

        // 更新所有单元格UI
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                UpdateCellUI(row, col);
            }
        }
    }

    // 允许风吹技能更新游戏数据
    public void UpdateGridData(int[,] newGameGrid, bool[,] newFixedCells)
    {
        // 清空历史记录，因为整个网格状态已经改变
        moveHistory.Clear();

        // 禁用撤回按钮
        if (undoButton != null)
        {
            undoButton.interactable = false;
        }

        // 清除所有冲突高亮
        ClearAllConflictHighlights();

        // 更新游戏网格数据
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                gameGrid[row, col] = newGameGrid[row, col];
                fixedCells[row, col] = newFixedCells[row, col];
            }
        }

        // 刷新UI显示
        UpdateAllCellsUI();

        // 在更新数据后检查游戏是否完成
        CheckGameCompletion();
    }

    public void SetGridSpawner(SudokuGridSpawner spawner)
    {
        this.gridSpawner = spawner;
    }

    // 获取当前选中单元格的值
    public int GetSelectedCellValue()
    {
        if (gridSpawner == null) return 0;

        Vector2Int selectedCell = gridSpawner.GetLastSelectedCell();
        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            return GetCellValue(selectedCell.x, selectedCell.y);
        }
        return 0;
    }

    // 检查当前选中的单元格是否为固定单元格
    public bool IsSelectedCellFixed()
    {
        if (gridSpawner == null) return false;

        Vector2Int selectedCell = gridSpawner.GetLastSelectedCell();
        if (selectedCell.x >= 0 && selectedCell.y >= 0)
        {
            return IsFixedCell(selectedCell.x, selectedCell.y);
        }
        return false;
    }
}