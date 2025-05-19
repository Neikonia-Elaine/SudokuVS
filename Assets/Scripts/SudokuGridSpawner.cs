using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

// 负责生成数独格子及管理格子交互
// 管理cell的状态更新，获取选中格子的坐标，和各种你可能需要的cell坐标
public class SudokuGridSpawner : MonoBehaviour
{
    public GameObject cellPrefab;
    public GameObject numberSelectorPrefab; // 包含9个数字按钮的面板
    public Transform gridParent;

    // 引用GameManager
    private GameManager gameManager;

    // 存储单元格和选择器面板
    private Dictionary<Vector2Int, GameObject> cells = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, CellManager> cellManagers = new Dictionary<Vector2Int, CellManager>();
    private Dictionary<Vector2Int, GameObject> selectorPanels = new Dictionary<Vector2Int, GameObject>();

    // 跟踪当前选中的单元格
    private Vector2Int lastSelectedCell = new Vector2Int(-1, -1);

    void Awake()
    {
        // 获取GameManager引用
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("无法找到GameManager!");
        }
    }

    void Start()
    {
        CreateSudokuGrid();
    }

    void Update()
    {
        // 监听键盘Delete键或Backspace键，清除选中的单元格数字
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 检查是否按下了删除键
        if (keyboard.deleteKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
        {
            Debug.Log($"检测到删除键被按下，当前选中单元格为 ({lastSelectedCell.x}, {lastSelectedCell.y})");

            // 检查是否有选中的单元格
            if (lastSelectedCell.x >= 0 && lastSelectedCell.y >= 0)
            {
                // 检查是否是非固定单元格
                if (!gameManager.IsFixedCell(lastSelectedCell.x, lastSelectedCell.y))
                {
                    Debug.Log($"清除单元格 ({lastSelectedCell.x}, {lastSelectedCell.y}) 的值");

                    // 调用GameManager设置单元格值为0（清除）
                    gameManager.SetCellValue(lastSelectedCell.x, lastSelectedCell.y, 0);

                    // 隐藏数字选择面板
                    Vector2Int cellPos = lastSelectedCell;
                    if (selectorPanels.TryGetValue(cellPos, out GameObject panel))
                    {
                        panel.SetActive(false);
                    }
                }
                else
                {
                    Debug.Log($"单元格 ({lastSelectedCell.x}, {lastSelectedCell.y}) 是固定的，不能清除");
                }
            }
            else
            {
                Debug.Log("没有选中的单元格");
            }
        }
    }

    public GameManager GetGameManager()
    {
        return gameManager;
    }

    // 创建数独网格UI
    public void CreateSudokuGrid()
    {
        // 清空现有网格（如果有）
        ClearGrid();

        // 创建81个小格子
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Vector2Int cellPos = new Vector2Int(row, col);
                CreateCell(cellPos);
            }
        }
    }

    // 清空网格
    private void ClearGrid()
    {
        // 清除所有现有单元格
        foreach (var cell in cells.Values)
        {
            if (cell != null)
            {
                Destroy(cell);
            }
        }

        cells.Clear();
        cellManagers.Clear();
        selectorPanels.Clear();
    }

    private void CreateCell(Vector2Int position)
    {
        int row = position.x;
        int col = position.y;

        // 实例化单元格
        GameObject cell = Instantiate(cellPrefab, gridParent);
        cell.name = $"Cell_{row}_{col}";

        // 存入字典
        cells[position] = cell;

        // 设置3x3区域背景色差异
        Image cellImage = cell.GetComponent<Image>();
        if (cellImage != null)
        {
            bool isEvenBlock = (row / 3 + col / 3) % 2 == 0;
            cellImage.color = isEvenBlock ? new Color(0.99f, 0.99f, 0.99f) : new Color(0.85f, 0.85f, 0.85f);
        }

        // 创建高亮遮罩
        GameObject highlightGO = new GameObject("Highlight");
        highlightGO.transform.SetParent(cell.transform, false);
        Image highlightImage = highlightGO.AddComponent<Image>();
        highlightImage.color = new Color(1f, 0.8f, 0.2f, 0f); // 初始为透明
        RectTransform highlightRT = highlightGO.GetComponent<RectTransform>();
        highlightRT.anchorMin = Vector2.zero;
        highlightRT.anchorMax = Vector2.one;
        highlightRT.offsetMin = Vector2.zero;
        highlightRT.offsetMax = Vector2.zero;

        // 创建数字显示 Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(cell.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 36;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // 添加CellManager组件
        CellManager cellManager = cell.AddComponent<CellManager>();
        cellManager.Init(row, col, this, text, highlightImage);

        // 存储CellManager引用
        cellManagers[position] = cellManager;

        // 创建并绑定数字选择面板
        GameObject selector = Instantiate(numberSelectorPrefab, cell.transform);
        selector.SetActive(false);
        selectorPanels[position] = selector;
        BindNumberButtons(selector, position);
    }

    // 绑定数字按钮事件
    private void BindNumberButtons(GameObject selector, Vector2Int cellPos)
    {
        Button[] numberButtons = selector.GetComponentsInChildren<Button>();
        for (int k = 0; k < numberButtons.Length; k++)
        {
            int num = k + 1; // 数字 1-9

            // 在Lambda中使用值传递而非引用，避免闭包问题
            int finalRow = cellPos.x;
            int finalCol = cellPos.y;

            numberButtons[k].onClick.AddListener(() => OnNumberClicked(num, finalRow, finalCol));
        }

        // 添加清除按钮处理（如果有的话）
        Button clearButton = selector.transform.Find("ClearButton")?.GetComponent<Button>();
        if (clearButton != null)
        {
            int finalRow = cellPos.x;
            int finalCol = cellPos.y;

            clearButton.onClick.AddListener(() => {
                // 如果是固定单元格，则不响应点击
                if (!gameManager.IsFixedCell(finalRow, finalCol))
                {
                    // 清除单元格值（设置为0）
                    gameManager.SetCellValue(finalRow, finalCol, 0);

                    // 隐藏数字选择面板
                    Vector2Int pos = new Vector2Int(finalRow, finalCol);
                    if (selectorPanels.TryGetValue(pos, out GameObject panel))
                    {
                        panel.SetActive(false);
                    }
                }
            });
        }
    }

    // 单元格被点击时调用
    public void OnCellClicked(int row, int col)
    {
        Debug.Log($"单元格被点击: ({row}, {col})");

        // 获取上一个选中的单元格，并取消其选中高亮
        if (lastSelectedCell.x >= 0 && lastSelectedCell.y >= 0)
        {
            CellManager lastCellManager = GetCellManager(lastSelectedCell.x, lastSelectedCell.y);
            if (lastCellManager != null)
            {
                lastCellManager.SetHighlight(false);

                // 如果上一个选中的单元格有值，更新其UI以确保冲突高亮正确显示
                int value = gameManager.GetCellValue(lastSelectedCell.x, lastSelectedCell.y);
                if (value > 0)
                {
                    gameManager.UpdateCellUI(lastSelectedCell.x, lastSelectedCell.y);
                }
            }
        }

        // 设置新选中的单元格高亮
        CellManager cellManager = GetCellManager(row, col);
        if (cellManager != null)
        {
            cellManager.SetHighlight(true);
        }

        // 更新最后选中的单元格记录
        lastSelectedCell = new Vector2Int(row, col);

        // 如果是固定单元格，则不显示数字选择面板
        if (gameManager.IsFixedCell(row, col))
            return;

        // 隐藏所有数字选择面板
        foreach (var item in selectorPanels.Values)
        {
            item.SetActive(false);
        }

        // 显示当前单元格的数字选择面板
        Vector2Int cellPos = new Vector2Int(row, col);
        if (selectorPanels.TryGetValue(cellPos, out GameObject selectorPanel))
        {
            selectorPanel.SetActive(true);
        }
    }

    // 数字按钮被点击时调用
    public void OnNumberClicked(int number, int row, int col)
    {
        // 如果是固定单元格，则不响应点击
        if (gameManager.IsFixedCell(row, col))
            return;

        Vector2Int cellPos = new Vector2Int(row, col);

        // 更新游戏状态
        gameManager.SetCellValue(row, col, number);

        // 隐藏数字选择面板
        if (selectorPanels.TryGetValue(cellPos, out GameObject panel))
        {
            panel.SetActive(false);
        }
    }

    // 清除当前选中单元格的值
    public void ClearSelectedCellValue()
    {
        if (lastSelectedCell.x >= 0 && lastSelectedCell.y >= 0)
        {
            // 判断非固定单元格才允许清除
            if (!gameManager.IsFixedCell(lastSelectedCell.x, lastSelectedCell.y))
            {
                Debug.Log($"清除单元格 ({lastSelectedCell.x}, {lastSelectedCell.y}) 的值");
                gameManager.SetCellValue(lastSelectedCell.x, lastSelectedCell.y, 0);

                // 隐藏数字选择面板
                Vector2Int cellPos = lastSelectedCell;
                if (selectorPanels.TryGetValue(cellPos, out GameObject panel))
                {
                    panel.SetActive(false);
                }
            }
        }
    }

    // 清除选中状态 - 可以在其他地方调用，如按ESC键时
    public void ClearSelection()
    {
        if (lastSelectedCell.x >= 0 && lastSelectedCell.y >= 0)
        {
            CellManager cellManager = GetCellManager(lastSelectedCell.x, lastSelectedCell.y);
            if (cellManager != null)
            {
                cellManager.SetHighlight(false);
            }

            // 隐藏数字选择面板
            Vector2Int cellPos = lastSelectedCell;
            if (selectorPanels.TryGetValue(cellPos, out GameObject panel))
            {
                panel.SetActive(false);
            }

            lastSelectedCell = new Vector2Int(-1, -1);
        }
    }

    // 获取CellManager组件
    public CellManager GetCellManager(int row, int col)
    {
        Vector2Int pos = new Vector2Int(row, col);
        if (cellManagers.TryGetValue(pos, out CellManager cellManager))
        {
            return cellManager;
        }
        return null;
    }

    // 获取随机3x3区域（返回左上角坐标）
    public Vector2Int GetRandom3x3Subgrid()
    {
        int subgridRow = Random.Range(0, 3); // 0, 1, 或 2
        int subgridCol = Random.Range(0, 3); // 0, 1, 或 2

        // 返回此区域左上角的坐标
        return new Vector2Int(subgridRow * 3, subgridCol * 3);
    }

    // 获取3x3区域内的随机单元格
    public Vector2Int GetRandomCellInSubgrid(Vector2Int subgridTopLeft)
    {
        int row = subgridTopLeft.x + Random.Range(0, 3); // 在3x3内随机
        int col = subgridTopLeft.y + Random.Range(0, 3); // 在3x3内随机

        return new Vector2Int(row, col);
    }

    // 获取一个完全随机的单元格
    public Vector2Int GetRandomCell()
    {
        int row = Random.Range(0, 9);
        int col = Random.Range(0, 9);

        return new Vector2Int(row, col);
    }

    // 通过位置获取单元格
    public GameObject GetCellByPosition(Vector2Int position)
    {
        if (cells.TryGetValue(position, out GameObject cell))
        {
            return cell;
        }
        return null;
    }

    // 获取一个随机的可填写格子（非固定且为空的格子）- for 技能
    public Vector2Int GetRandomWriteableCell()
    {
        List<Vector2Int> writeableCells = new List<Vector2Int>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                // 检查是否是可填写的格子（非固定且为空）
                if (!gameManager.IsFixedCell(row, col) && gameManager.GetCellValue(row, col) == 0)
                {
                    writeableCells.Add(new Vector2Int(row, col));
                }
            }
        }

        // 如果没有可填写的格子，返回无效坐标
        if (writeableCells.Count == 0)
        {
            return new Vector2Int(-1, -1);
        }

        // 随机选择一个可填写的格子
        int randomIndex = Random.Range(0, writeableCells.Count);
        return writeableCells[randomIndex];
    }

    // 获取所有可填写的格子（非固定且为空的格子）
    public List<Vector2Int> GetAllWriteableCells()
    {
        List<Vector2Int> writeableCells = new List<Vector2Int>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                // 检查是否是可填写的格子（非固定且为空）
                if (!gameManager.IsFixedCell(row, col) && gameManager.GetCellValue(row, col) == 0)
                {
                    writeableCells.Add(new Vector2Int(row, col));
                }
            }
        }

        return writeableCells;
    }

    // 获取当前选中的格子
    public Vector2Int GetLastSelectedCell()
    {
        return lastSelectedCell;
    }

    // 获取当前选中的行索引
    public int GetSelectedRow()
    {
        return lastSelectedCell.x;
    }

    // 获取当前选中的列索引
    public int GetSelectedCol()
    {
        return lastSelectedCell.y;
    }
}