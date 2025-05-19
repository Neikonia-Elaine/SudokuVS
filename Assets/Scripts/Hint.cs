using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HintController : MonoBehaviour
{
    public Button[] numberButtons; // 按钮 1~9
    public TextMeshProUGUI[] countTexts; // 对应剩余提示数字

    public Color highlightColor = new Color(1f, 0.8f, 0.2f); // 高亮颜色
    private Color originalHighlightColor = new Color(0f, 0f, 0f, 0f); // 用于清除时重置
    private SudokuGridSpawner gridSpawner;


    void Start()
    {
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int num = i + 1;
            int index = i; // 避免闭包
            EventTriggerListener.Get(numberButtons[i].gameObject).onEnter = (_) => OnHoverNumber(num);
            EventTriggerListener.Get(numberButtons[i].gameObject).onExit = (_) => OnExitNumber();
            numberButtons[i].onClick.AddListener(() => OnClickNumber(num));
        }
    }

    public void SetGridSpawner(SudokuGridSpawner spawner)
    {
        gridSpawner = spawner;
    }

    public void OnHoverNumber(int num) // 鼠标移动高亮
    {
        if (gridSpawner == null) return;

        var gm = gridSpawner.GetGameManager();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (gm.GetCellValue(row, col) == num)
                {
                    GameObject cell = gridSpawner.GetCellByPosition(new Vector2Int(row, col));
                    if (cell != null)
                    {
                        Transform hl = cell.transform.Find("Highlight");
                        if (hl != null)
                        {
                            Image img = hl.GetComponent<Image>();
                            img.color = new Color(1f, 0.8f, 0.2f, 0.5f); // 半透明黄色
                        }
                    }
                }
            }
        }
    }

    public void OnExitNumber()
    {
        if (gridSpawner == null) return;

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                GameObject cell = gridSpawner.GetCellByPosition(new Vector2Int(row, col));
                if (cell != null)
                {
                    Transform hl = cell.transform.Find("Highlight");
                    if (hl != null)
                    {
                        Image img = hl.GetComponent<Image>();
                        img.color = new Color(1f, 0.8f, 0.2f, 0f); // 设为透明
                    }
                }
            }
        }
    }

    public void OnClickNumber(int num)
    {
        // 可选：点击时切换是否显示高亮
    }
}
