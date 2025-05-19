using UnityEngine;
using TMPro; // 或者 using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerText();
        }
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // 开始计时并清零
    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        UpdateTimerText();
    }

    // 暂停计时
    public void PauseTimer()
    {
        isRunning = false;
    }

    // 恢复计时（不清零）
    public void ResumeTimer()
    {
        isRunning = true;
    }

    // 重置计时（清零 + 停止）
    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = false;
        UpdateTimerText();
    }

    // 获取当前时间（秒）
    public float GetTime()
    {
        return elapsedTime;
    }
}
