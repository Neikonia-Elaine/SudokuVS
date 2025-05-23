using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// 每个格子的 UI 和交互控制组件
public class CellManager : MonoBehaviour, IPointerClickHandler
{
    private int row;
    private int col;

    private SudokuGridSpawner spawner;
    private TextMeshProUGUI numberText;
    private Image highlightOverlay;

    public bool canClick = true;

    // 初始化格子
    public void Init(int r, int c, SudokuGridSpawner s, TextMeshProUGUI text, Image highlight)
    {
        row = r;
        col = c;
        spawner = s;
        numberText = text;
        highlightOverlay = highlight;
    }

    // 点击时通知 Spawner
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick)
        {
            return;
        }
        spawner.OnCellClicked(row, col);
        SetHighlight(true);
    }

    // 设置显示的数字
    public void SetNumber(int number)
    {
        numberText.text = number > 0 ? number.ToString() : "";
    }

    // 清除当前单元格的数字
    public void ClearNumber()
    {
        numberText.text = "";
    }

    // 设置数字颜色（固定、冲突、正常）
    public void SetColor(Color color)
    {
        numberText.color = color;
    }

    // 设置是否高亮（用于悬停提示）
    public void SetHighlight(bool on)
    {
        highlightOverlay.color = on
            ? new Color(1f, 0.8f, 0.2f, 0.5f)
            : new Color(1f, 0.8f, 0.2f, 0f); // 同色，透明度控制
    }

    // 提供访问接口（可用于同步/调试）
    public int GetRow() => row;
    public int GetCol() => col;
}