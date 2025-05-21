using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 简化版多人游戏单元格管理器
/// 移除了对手区分逻辑，只保留必要的网络同步功能
/// </summary>
public class MultiplayerCellManager : MonoBehaviour, IPointerClickHandler
{
    // 引用原始CellManager
    private CellManager baseCellManager;

    // 背景图像用于显示不同状态的颜色
    private Image backgroundImage;

    // 是否启用交互
    private bool interactionEnabled = true;

    private void Awake()
    {
        // 获取基础CellManager组件
        baseCellManager = GetComponent<CellManager>();

        // 如果没有基础CellManager，创建一个
        if (baseCellManager == null)
        {
            Debug.LogError("MultiplayerCellManager需要在同一GameObject上有一个CellManager组件!");
        }

        // 创建背景图像
        CreateBackgroundImage();
    }

    // 创建背景图像用于颜色显示
    private void CreateBackgroundImage()
    {
        GameObject bgGO = new GameObject("MPBackground");
        bgGO.transform.SetParent(transform, false);
        backgroundImage = bgGO.AddComponent<Image>();
        backgroundImage.color = Color.clear; // 默认透明

        // 设置为最下层
        bgGO.transform.SetAsFirstSibling();

        // 设置为填充父对象
        RectTransform bgRT = backgroundImage.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
    }

    // 实现IPointerClickHandler接口 - 转发点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        // 只有在交互启用时才处理点击事件
        if (interactionEnabled && baseCellManager != null)
        {
            // 可以在这里转发到原始CellManager的点击逻辑
            Debug.Log("多人模式单元格点击");
        }
        else
        {
            Debug.Log("多人模式单元格点击 - 交互已禁用");
        }
    }

    // 设置背景颜色 - 用于特殊状态的可视化
    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage == null) return;
        backgroundImage.color = color;
    }

    // 禁用交互
    public void DisableInteraction()
    {
        interactionEnabled = false;

        // 禁用按钮组件
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.enabled = false;
        }
    }

    // 启用交互 - 用于恢复交互功能
    public void EnableInteraction()
    {
        interactionEnabled = true;

        // 启用按钮组件
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.enabled = true;
        }
    }

    // 获取基础CellManager（如果需要访问原始功能）
    public CellManager GetBaseCellManager()
    {
        return baseCellManager;
    }

    // 获取交互状态
    public bool IsInteractionEnabled()
    {
        return interactionEnabled;
    }
}