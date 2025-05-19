using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillManager : MonoBehaviour
{
    [Header("技能面板引用")]
    public GameObject skillPanel;
    public GameObject skillContainer;

    [Header("游戏管理器引用")]
    public GameManager gameManager;

    [Header("技能按钮引用")]
    public Button fireSkillButton; // 火焰技能按钮
    public Button grassSkillButton; // 小草技能按钮
    public Button windSkillButton; // 风吹技能按钮

    [Header("技能效果预制体")]
    public GameObject fireEffectPrefab;
    public GameObject grassEffectPrefab; // 小草效果预制体
    public GameObject windEffectPrefab; // 风效果预制体

    [Header("技能参数设置")]
    [Range(1f, 60f)]
    public float fireEffectDuration = 3f;
    [Range(1f, 60f)]
    public float grassEffectDuration = 2f; // 小草动效持续时间
    [Range(1f, 60f)]
    public float windEffectDuration = 3f; // 风效果动画持续时间
    [Range(10f, 300f)]
    public float fakeDurationSeconds = 120f; // 假数字持续时间（2分钟）
    [Range(0.1f, 5f)]
    public float rotationAnimationDuration = 1.5f; // 旋转动画持续时间

    [Header("假数字文本设置")]
    public Color fakeNumberColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
    public float fakeNumberFontSize = 36f;

    [Header("数独格子生成器引用")]
    public SudokuGridSpawner gridSpawner;

    public static SkillManager Instance { get; private set; }

    private GameObject activeFireEffect;
    private GameObject activeGrassEffect; // 小草效果引用
    private GameObject activeWindEffect;
    private List<GameObject> fakeNumberTexts = new List<GameObject>(); // 跟踪所有创建的假数字
    // 风技能
    private bool isRotating = false; // 是否正在旋转
    private bool isGridRotated = false; // 网格是否已被旋转

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (fireEffectPrefab == null)
        {
            Debug.LogError("火焰效果预制体未设置!");
        }

        if (grassEffectPrefab == null)
        {
            Debug.LogError("小草效果预制体未设置!");
        }

        if (windEffectPrefab == null)
        {
            Debug.LogError("风效果预制体未设置!");
        }

        // 设置技能按钮监听
        SetupSkillButtons();
    }

    // 设置技能按钮监听
    private void SetupSkillButtons()
    {
        if (fireSkillButton != null)
        {
            fireSkillButton.onClick.AddListener(ActivateFireSkill);
        }
        else
        {
            Debug.LogError("未设置火焰技能按钮引用!");
        }

        if (grassSkillButton != null)
        {
            grassSkillButton.onClick.AddListener(ActivateGrassSkill);
        }
        else
        {
            Debug.LogError("未设置小草技能按钮引用!");
        }

        if (windSkillButton != null)
        {
            windSkillButton.onClick.AddListener(ActivateWindSkill);
        }
        else
        {
            Debug.LogError("未设置风吹技能按钮引用!");
        }
    }

    public void SetGridSpawner(SudokuGridSpawner spawner)
    {
        this.gridSpawner = spawner;
    }

    #region 火焰技能
    public void ActivateFireSkill()
    {
        Debug.Log("激活火焰技能");

        if (ValidateComponents("火焰"))
        {
            try
            {
                Vector2Int subgrid = gridSpawner.GetRandom3x3Subgrid();
                Debug.Log($"获取到随机3x3区域，左上角坐标为: {subgrid.x},{subgrid.y}");

                ShowFireEffectInSubgrid(subgrid);

                DisableSkillButton(fireSkillButton, fireEffectDuration);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"激活火焰技能时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    public void ShowFireEffectInSubgrid(Vector2Int subgridTopLeft)
    {
        Vector2Int centerPos = new Vector2Int(subgridTopLeft.x + 1, subgridTopLeft.y + 1);
        GameObject centerCell = gridSpawner.GetCellByPosition(centerPos);

        if (centerCell == null)
        {
            Debug.LogWarning($"找不到中心格子: {centerPos}");
            return;
        }

        if (activeFireEffect != null)
        {
            Destroy(activeFireEffect);
        }

        // 获取网格的父容器
        Transform gridParent = gridSpawner.transform.parent;

        // 在父容器下实例化火焰效果
        activeFireEffect = Instantiate(fireEffectPrefab, gridParent);

        // 将火焰效果定位到子网格的位置
        RectTransform rt = activeFireEffect.GetComponent<RectTransform>();
        if (rt != null)
        {
            // 获取中心格子的世界位置
            Vector3 centerPosition = centerCell.transform.position;
            rt.position = centerPosition;

            // 设置大小为300x300
            rt.sizeDelta = new Vector2(300, 300);
        }

        StartCoroutine(HideEffectAfterDelay(activeFireEffect, fireEffectDuration, "火焰"));
    }
    #endregion

    #region 小草技能
    public void ActivateGrassSkill()
    {
        Debug.Log("激活小草技能");

        if (ValidateComponents("小草"))
        {
            try
            {
                // 获取一个随机的空白格子
                Vector2Int targetCell = GetRandomEmptyCell();

                if (targetCell.x < 0 || targetCell.y < 0)
                {
                    Debug.LogWarning("没有空白格子!");
                    return;
                }

                Debug.Log($"获取到随机空白格子，坐标为: {targetCell.x},{targetCell.y}");

                // 显示小草动效
                ShowGrassEffectInCell(targetCell);

                DisableSkillButton(grassSkillButton, grassEffectDuration);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"激活小草技能时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    // 获取随机的空白格子（包括固定和非固定的）
    private Vector2Int GetRandomEmptyCell()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                // 检查是否是空白格子
                if (gameManager.GetCellValue(row, col) == 0)
                {
                    emptyCells.Add(new Vector2Int(row, col));
                }
            }
        }

        // 如果没有空白格子，返回无效坐标
        if (emptyCells.Count == 0)
        {
            return new Vector2Int(-1, -1);
        }

        // 随机选择一个空白格子
        int randomIndex = Random.Range(0, emptyCells.Count);
        return emptyCells[randomIndex];
    }

    // 在指定格子上显示小草效果
    public void ShowGrassEffectInCell(Vector2Int cellPosition)
    {
        GameObject targetCell = gridSpawner.GetCellByPosition(cellPosition);

        if (targetCell == null)
        {
            Debug.LogWarning($"找不到目标格子: {cellPosition}");
            return;
        }

        if (activeGrassEffect != null)
        {
            Destroy(activeGrassEffect);
        }

        // 获取网格的父容器
        Transform gridParent = gridSpawner.transform.parent;

        // 在父容器下实例化小草效果
        activeGrassEffect = Instantiate(grassEffectPrefab, gridParent);

        // 将小草效果定位到目标格子的位置
        RectTransform rt = activeGrassEffect.GetComponent<RectTransform>();
        if (rt != null)
        {
            // 获取目标格子的世界位置
            Vector3 targetPosition = targetCell.transform.position;
            rt.position = targetPosition;

            // 设置适当的大小
            rt.sizeDelta = new Vector2(100, 100); // 格子大小
        }

        // 启动协程，显示动效后创建假数字
        StartCoroutine(ShowFakeNumberAfterDelay(cellPosition));
    }

    // 在动效结束后创建假数字
    private IEnumerator ShowFakeNumberAfterDelay(Vector2Int cellPosition)
    {
        // 等待动效播放完成
        yield return new WaitForSeconds(grassEffectDuration);

        if (activeGrassEffect != null)
        {
            Destroy(activeGrassEffect);
            activeGrassEffect = null;
            Debug.Log("隐藏小草效果");
        }

        // 创建一个1-9的随机数作为假数字
        int randomNumber = Random.Range(1, 10);

        // 获取目标格子
        GameObject targetCell = gridSpawner.GetCellByPosition(cellPosition);
        if (targetCell == null)
        {
            Debug.LogWarning($"找不到目标格子: {cellPosition}");
            yield break;
        }

        // 获取网格的父容器
        Transform gridParent = gridSpawner.transform.parent;

        // 创建假数字文本对象
        GameObject fakeNumberObj = new GameObject("FakeNumber_" + cellPosition.x + "_" + cellPosition.y);
        fakeNumberObj.transform.SetParent(gridParent, false);

        // 添加文本组件
        TextMeshProUGUI textComponent = fakeNumberObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = randomNumber.ToString();
        textComponent.fontSize = fakeNumberFontSize;
        textComponent.color = fakeNumberColor;
        textComponent.alignment = TextAlignmentOptions.Center;

        // 设置RectTransform
        RectTransform fakeNumberRT = fakeNumberObj.GetComponent<RectTransform>();
        fakeNumberRT.position = targetCell.transform.position;
        fakeNumberRT.sizeDelta = new Vector2(100, 100); // 与格子大小相同

        // 确保假数字在单元格前面显示
        Canvas canvas = fakeNumberObj.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5; // 确保在普通数字之上

        // 添加到列表中跟踪
        fakeNumberTexts.Add(fakeNumberObj);

        Debug.Log($"在位置({cellPosition.x},{cellPosition.y})创建假数字: {randomNumber}，将在{fakeDurationSeconds}秒后消失");

        // 启动协程，在指定时间后移除假数字
        StartCoroutine(RemoveFakeNumberAfterDelay(fakeNumberObj, fakeDurationSeconds));
    }

    // 在指定时间后移除假数字
    private IEnumerator RemoveFakeNumberAfterDelay(GameObject fakeNumber, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fakeNumber != null)
        {
            fakeNumberTexts.Remove(fakeNumber);
            Destroy(fakeNumber);
            Debug.Log("移除假数字");
        }
    }
    #endregion

    #region 风吹技能
    // 激活风吹技能
    public void ActivateWindSkill()
    {
        Debug.Log("激活风吹技能");

        if (ValidateComponents("风吹") && !isRotating)
        {
            try
            {
                // 获取整个游戏区域的中心位置
                Vector2Int centerPos = new Vector2Int(4, 4); // 9x9网格的中心位置
                GameObject centerCell = gridSpawner.GetCellByPosition(centerPos);

                if (centerCell == null)
                {
                    Debug.LogWarning($"找不到中心格子: {centerPos}");
                    return;
                }

                // 显示风效果动画
                ShowWindEffect(centerCell.transform.position);

                DisableSkillButton(windSkillButton, windEffectDuration + rotationAnimationDuration);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"激活风吹技能时发生错误: {e.Message}\n{e.StackTrace}");
            }
        }
        else if (isRotating)
        {
            Debug.Log("正在旋转中，请稍候再试");
        }
    }

    // 显示风效果
    private void ShowWindEffect(Vector3 centerPosition)
    {
        if (activeWindEffect != null)
        {
            Destroy(activeWindEffect);
        }

        // 获取网格的父容器
        Transform gridParent = gridSpawner.transform.parent;

        // 在父容器下实例化风效果
        activeWindEffect = Instantiate(windEffectPrefab, gridParent);

        // 将风效果定位到中心位置
        RectTransform rt = activeWindEffect.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.position = centerPosition;
            // 设置足够大的尺寸覆盖整个九宫格
            rt.sizeDelta = new Vector2(500, 500);
        }

        // 在风效果显示一段时间后开始旋转九宫格
        StartCoroutine(RotateGridAfterDelay());
    }

    // 在延迟后旋转九宫格
    private IEnumerator RotateGridAfterDelay()
    {
        // 等待风效果动画播放一段时间
        yield return new WaitForSeconds(windEffectDuration * 0.5f);

        // 开始旋转九宫格
        StartCoroutine(RotateGrid());

        // 在旋转完成后隐藏风效果
        yield return new WaitForSeconds(windEffectDuration * 0.5f + rotationAnimationDuration);

        if (activeWindEffect != null)
        {
            Destroy(activeWindEffect);
            activeWindEffect = null;
            Debug.Log("隐藏风效果");
        }
    }

    // 旋转九宫格 - 平滑顺时针旋转
    private IEnumerator RotateGrid()
    {
        isRotating = true;
        Debug.Log("开始旋转九宫格");

        // 收集所有可填写的格子
        List<Vector2Int> writeableCells = gridSpawner.GetAllWriteableCells();
        HashSet<Vector2Int> writeableCellsSet = new HashSet<Vector2Int>(writeableCells);

        // 从GameManager中获取当前游戏状态
        Dictionary<Vector2Int, CellData> cellData = new Dictionary<Vector2Int, CellData>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Vector2Int pos = new Vector2Int(row, col);
                GameObject cell = gridSpawner.GetCellByPosition(pos);

                if (cell != null)
                {
                    int value = gameManager.GetCellValue(row, col);
                    bool isFixed = gameManager.IsFixedCell(row, col);
                    bool isWriteable = writeableCellsSet.Contains(pos);

                    // 获取CellManager来获取文本信息
                    CellManager cellManager = gridSpawner.GetCellManager(row, col);
                    string text = "";
                    Color textColor = Color.black;

                    if (cellManager != null)
                    {
                        // 假设TextMeshProUGUI可以从CellManager获取
                        TextMeshProUGUI textComp = cellManager.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        if (textComp != null)
                        {
                            text = textComp.text;
                            textColor = textComp.color;
                        }
                    }

                    cellData[pos] = new CellData(cell, text, textColor, isWriteable, value, isFixed);
                }
            }
        }

        // 创建旋转动画所需的临时文本对象（用于显示旋转效果）
        Dictionary<Vector2Int, GameObject> tempTexts = new Dictionary<Vector2Int, GameObject>();
        Transform gridParent = gridSpawner.transform.parent;

        foreach (var entry in cellData)
        {
            Vector2Int origPos = entry.Key;

            if (!string.IsNullOrEmpty(entry.Value.text))
            {
                // 创建临时文本对象
                GameObject tempTextObj = new GameObject($"TempRotationText_{origPos.x}_{origPos.y}");
                tempTextObj.transform.SetParent(gridParent, false);

                TextMeshProUGUI tmpText = tempTextObj.AddComponent<TextMeshProUGUI>();
                tmpText.text = entry.Value.text;
                tmpText.fontSize = 36;
                tmpText.color = entry.Value.textColor;
                tmpText.alignment = TextAlignmentOptions.Center;

                // 设置RectTransform
                RectTransform tempRT = tempTextObj.GetComponent<RectTransform>();
                tempRT.position = entry.Value.cell.transform.position;
                tempRT.sizeDelta = new Vector2(100, 100);

                // 确保临时文本显示在最前面
                Canvas canvas = tempTextObj.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;

                tempTexts[origPos] = tempTextObj;
            }
        }

        // 显示旋转动画 - 平滑顺时针旋转，不从中心缩放
        float elapsedTime = 0f;
        Vector3 gridCenter = new Vector3(
            gridSpawner.transform.position.x,
            gridSpawner.transform.position.y,
            gridSpawner.transform.position.z
        );

        // 计算每个格子到中心的方向和距离
        Dictionary<Vector2Int, Vector3> directionsFromCenter = new Dictionary<Vector2Int, Vector3>();
        Dictionary<Vector2Int, float> distancesFromCenter = new Dictionary<Vector2Int, float>();

        foreach (var entry in tempTexts)
        {
            Vector2Int pos = entry.Key;
            GameObject textObj = entry.Value;

            Vector3 positionInWorld = textObj.transform.position;
            Vector3 directionFromCenter = positionInWorld - gridCenter;

            directionsFromCenter[pos] = directionFromCenter.normalized;
            distancesFromCenter[pos] = directionFromCenter.magnitude;
        }

        while (elapsedTime < rotationAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / rotationAnimationDuration; // 0到1的插值因子

            // 使用缓动函数使动画更自然
            t = Mathf.SmoothStep(0, 1, t);

            // 使用角度插值旋转每个临时文本对象
            float rotationAngle = t * 90f; // 0到90度

            foreach (var entry in tempTexts)
            {
                Vector2Int pos = entry.Key;
                GameObject textObj = entry.Value;

                if (textObj != null)
                {
                    // 获取当前方向和距离
                    Vector3 direction = directionsFromCenter[pos];
                    float distance = distancesFromCenter[pos];

                    // 旋转方向向量（顺时针）
                    float radians = rotationAngle * Mathf.Deg2Rad;
                    Vector3 rotatedDirection = new Vector3(
                        direction.x * Mathf.Cos(radians) + direction.y * Mathf.Sin(radians),
                        -direction.x * Mathf.Sin(radians) + direction.y * Mathf.Cos(radians),
                        direction.z
                    );

                    // 计算新位置
                    Vector3 newPosition = gridCenter + rotatedDirection * distance;

                    // 更新位置
                    textObj.GetComponent<RectTransform>().position = newPosition;

                    // 为文本对象也添加一点旋转效果，使其与整体旋转方向一致
                    textObj.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
                }
            }

            yield return null;
        }

        // 动画结束，准备更新GameManager中的数据

        // 1. 首先创建新的数组来存储旋转后的数据
        int[,] rotatedGameGrid = new int[9, 9];
        bool[,] rotatedFixedCells = new bool[9, 9];

        // 2. 根据旋转规则更新数据
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Vector2Int origPos = new Vector2Int(row, col);
                Vector2Int rotatedPos = GetRotatedPosition(origPos);

                // 将原始位置的值复制到旋转位置
                rotatedGameGrid[rotatedPos.x, rotatedPos.y] = gameManager.GetCellValue(row, col);
                rotatedFixedCells[rotatedPos.x, rotatedPos.y] = gameManager.IsFixedCell(row, col);
            }
        }

        // 3. 更新GameManager中的数据
        gameManager.UpdateGridData(rotatedGameGrid, rotatedFixedCells);

        // 清除临时文本对象
        foreach (var tempText in tempTexts.Values)
        {
            Destroy(tempText);
        }

        // 切换旋转状态
        isGridRotated = !isGridRotated;
        isRotating = false;
        Debug.Log("九宫格旋转完成");
    }

    // 获取旋转后的位置（顺时针旋转90度）
    private Vector2Int GetRotatedPosition(Vector2Int originalPos)
    {
        // 顺时针旋转90度: (row, col) -> (col, 8-row)
        return new Vector2Int(originalPos.y, 8 - originalPos.x);
    }

    // 储存单元格数据的类
    private class CellData
    {
        public GameObject cell;
        public string text;
        public Color textColor;
        public bool isWriteable;
        public int value;
        public bool isFixed;

        public CellData(GameObject cell, string text, Color textColor, bool isWriteable, int value, bool isFixed)
        {
            this.cell = cell;
            this.text = text;
            this.textColor = textColor;
            this.isWriteable = isWriteable;
            this.value = value;
            this.isFixed = isFixed;
        }
    }
    #endregion

    #region 辅助方法
    // 验证组件是否都存在
    private bool ValidateComponents(string skillName)
    {
        if (gridSpawner == null)
        {
            gridSpawner = FindFirstObjectByType<SudokuGridSpawner>();
            if (gridSpawner == null)
            {
                Debug.LogError($"无法找到SudokuGridSpawner引用，{skillName}技能无法激活!");
                return false;
            }
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError($"无法找到GameManager引用，{skillName}技能无法激活!");
                return false;
            }
        }

        return true;
    }

    // 通用的隐藏效果协程
    private IEnumerator HideEffectAfterDelay(GameObject effect, float delay, string effectName)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null)
        {
            Destroy(effect);
            Debug.Log($"隐藏{effectName}效果");
        }
    }

    // 禁用技能按钮一段时间
    private void DisableSkillButton(Button button, float delay)
    {
        if (button != null)
        {
            button.interactable = false;
            StartCoroutine(EnableButtonAfterDelay(button, delay));
        }
    }

    // 延迟后启用按钮
    private IEnumerator EnableButtonAfterDelay(Button button, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (button != null)
        {
            button.interactable = true;
            Debug.Log("重新启用了技能按钮");
        }
    }
    #endregion

    void OnDestroy()
    {
        // 清理所有特效和假数字
        CleanupEffects();
    }

    // 清理所有特效和假数字
    private void CleanupEffects()
    {
        if (activeFireEffect != null)
        {
            Destroy(activeFireEffect);
        }

        if (activeGrassEffect != null)
        {
            Destroy(activeGrassEffect);
        }

        if (activeWindEffect != null)
        {
            Destroy(activeWindEffect);
        }

        // 清理所有假数字
        foreach (GameObject fakeNumber in fakeNumberTexts)
        {
            if (fakeNumber != null)
            {
                Destroy(fakeNumber);
            }
        }
        fakeNumberTexts.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}