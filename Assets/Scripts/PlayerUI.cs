using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// 简化版玩家UI界面 - 移除角色区分，保留通信功能
public class PlayerSignalUI : MonoBehaviour
{
    [Header("玩家信号UI")]
    [SerializeField] private Button sendSignalButton;
    [SerializeField] private TextMeshProUGUI signalDisplayText;

    [Header("显示设置")]
    [SerializeField] private float messageDisplayTime = 3f;

    private float messageTimer = 0f;

    private void Start()
    {
        // 设置按钮点击事件
        if (sendSignalButton != null)
        {
            sendSignalButton.onClick.AddListener(OnSendSignalButtonClicked);
        }

        // 注册信号接收事件
        if (PlayerSignalManager.Instance != null)
        {
            PlayerSignalManager.Instance.OnSignalReceived += OnSignalReceived;
        }

        // 初始化UI
        ClearSignalDisplay();
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (PlayerSignalManager.Instance != null)
        {
            PlayerSignalManager.Instance.OnSignalReceived -= OnSignalReceived;
        }
    }

    private void Update()
    {
        // 更新消息显示计时器
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0)
            {
                ClearSignalDisplay();
            }
        }
    }

    // 按钮点击事件
    public void OnSendSignalButtonClicked()
    {
        if (PlayerSignalManager.Instance != null)
        {
            // 发送按钮按下信号
            PlayerSignalManager.Instance.SendButtonPressSignal(1);
            DisplayLocalMessage("已发送信号!");
        }
        else
        {
            DisplayLocalMessage("错误: 信号管理器未找到!");
        }
    }

    // 发送自定义值信号
    public void SendValueSignal(int value)
    {
        if (PlayerSignalManager.Instance != null)
        {
            PlayerSignalManager.Instance.SendButtonPressSignal(value);
            DisplayLocalMessage($"已发送值: {value}");
        }
    }

    // 信号接收回调
    private void OnSignalReceived(PlayerSignalManager.SignalType signalType, int intValue, string stringValue, ulong senderId)
    {
        switch (signalType)
        {
            case PlayerSignalManager.SignalType.ButtonPress:
                signalDisplayText.text = $"收到: 对方按下了按钮! 值: {intValue}";
                signalDisplayText.color = Color.yellow;
                break;

            case PlayerSignalManager.SignalType.GameStart:
                signalDisplayText.text = "收到: 游戏开始!";
                signalDisplayText.color = Color.green;
                break;

            case PlayerSignalManager.SignalType.GameEnd:
                signalDisplayText.text = "收到: 游戏结束!";
                signalDisplayText.color = Color.red;
                break;

            default:
                signalDisplayText.text = $"收到: 未知信号 类型: {signalType}";
                if (!string.IsNullOrEmpty(stringValue))
                {
                    signalDisplayText.text += $" 消息: {stringValue}";
                }
                signalDisplayText.color = Color.white;
                break;
        }

        messageTimer = messageDisplayTime;
    }

    // 显示本地消息
    public void DisplayLocalMessage(string message)
    {
        if (signalDisplayText != null)
        {
            signalDisplayText.text = message;
            signalDisplayText.color = Color.cyan;
            messageTimer = messageDisplayTime;
        }
    }

    // 清除信号显示
    public void ClearSignalDisplay()
    {
        if (signalDisplayText != null)
        {
            signalDisplayText.text = "";
        }
    }
}