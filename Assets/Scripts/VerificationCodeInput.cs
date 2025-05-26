using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class VerificationCodeInput : MonoBehaviour
{
    [Header("输入框设置")]
    public List<TMP_InputField> inputFields = new List<TMP_InputField>();

    [Header("样式设置")]
    public Color normalColor = Color.white;
    public Color focusColor = Color.cyan;
    public Color completeColor = Color.green;

    [Header("事件")]
    public UnityEngine.Events.UnityEvent<string> OnCodeComplete;
    public UnityEngine.Events.UnityEvent<string> OnCodeChanged;

    private bool isPasting = false;

    void Start()
    {
        InitializeInputFields();
    }

    void InitializeInputFields()
    {
        for (int i = 0; i < inputFields.Count; i++)
        {
            int index = i; // 避免闭包问题

            // 基础设置
            inputFields[i].characterLimit = 1;
            inputFields[i].contentType = TMP_InputField.ContentType.IntegerNumber;

            // 事件监听
            inputFields[i].onValueChanged.AddListener((value) => OnInputValueChanged(index, value));
            inputFields[i].onSelect.AddListener((value) => OnInputSelected(index));
            inputFields[i].onDeselect.AddListener((value) => OnInputDeselected(index));

            // 添加自定义输入处理
            var inputHandler = inputFields[i].gameObject.GetComponent<TMPInputFieldHandler>();
            if (inputHandler == null)
            {
                inputHandler = inputFields[i].gameObject.AddComponent<TMPInputFieldHandler>();
            }
            inputHandler.Initialize(this, index);
        }

        // 默认选中第一个
        if (inputFields.Count > 0)
        {
            inputFields[0].Select();
        }
    }

    void OnInputValueChanged(int index, string value)
    {
        if (isPasting) return;

        if (!string.IsNullOrEmpty(value))
        {
            // 确保只保留最后一个字符（防止意外输入多个字符）
            if (value.Length > 1)
            {
                inputFields[index].text = value.Substring(value.Length - 1);
                return;
            }

            // 自动跳转到下一个输入框
            MoveToNextField(index);
        }

        UpdateFieldColors();
        CheckCodeComplete();
    }

    public void OnInputSelected(int index)
    {
        // 选中时改变颜色
        var colors = inputFields[index].colors;
        colors.normalColor = focusColor;
        inputFields[index].colors = colors;

        // 选中所有文本（方便替换）
        StartCoroutine(SelectAllText(inputFields[index]));
    }

    public void OnInputDeselected(int index)
    {
        // 取消选中时恢复颜色
        UpdateSingleFieldColor(index);
    }

    // 协程：延迟选中所有文本
    System.Collections.IEnumerator SelectAllText(TMP_InputField field)
    {
        yield return null; // 等待一帧
        field.selectionAnchorPosition = 0;
        field.selectionFocusPosition = field.text.Length;
    }

    void MoveToNextField(int currentIndex)
    {
        if (currentIndex < inputFields.Count - 1)
        {
            inputFields[currentIndex + 1].Select();
            inputFields[currentIndex + 1].ActivateInputField();
        }
    }

    void MoveToPreviousField(int currentIndex)
    {
        if (currentIndex > 0)
        {
            inputFields[currentIndex - 1].Select();
            inputFields[currentIndex - 1].ActivateInputField();
        }
    }

    void UpdateFieldColors()
    {
        for (int i = 0; i < inputFields.Count; i++)
        {
            UpdateSingleFieldColor(i);
        }
    }

    void UpdateSingleFieldColor(int index)
    {
        var colors = inputFields[index].colors;

        // 检查是否是当前激活的输入框
        bool isSelected = inputFields[index] == EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();

        if (isSelected)
        {
            colors.normalColor = focusColor;
        }
        else if (!string.IsNullOrEmpty(inputFields[index].text))
        {
            colors.normalColor = completeColor;
        }
        else
        {
            colors.normalColor = normalColor;
        }

        inputFields[index].colors = colors;
    }

    void CheckCodeComplete()
    {
        string code = GetCurrentCode();
        OnCodeChanged?.Invoke(code);

        if (code.Length == inputFields.Count)
        {
            OnCodeComplete?.Invoke(code);
        }
    }

    public string GetCurrentCode()
    {
        return string.Join("", inputFields.Select(field => field.text));
    }

    public void ClearAll()
    {
        foreach (var field in inputFields)
        {
            field.text = "";
        }
        UpdateFieldColors();
        inputFields[0].Select();
    }

    public void SetCode(string code)
    {
        isPasting = true;

        for (int i = 0; i < inputFields.Count && i < code.Length; i++)
        {
            if (char.IsDigit(code[i]))
            {
                inputFields[i].text = code[i].ToString();
            }
        }

        isPasting = false;
        UpdateFieldColors();
        CheckCodeComplete();

        // 设置焦点到最后一个填充的位置或下一个空位置
        int nextEmptyIndex = inputFields.FindIndex(field => string.IsNullOrEmpty(field.text));
        if (nextEmptyIndex >= 0)
        {
            inputFields[nextEmptyIndex].Select();
        }
        else
        {
            inputFields[inputFields.Count - 1].Select();
        }
    }

    // 处理键盘输入
    public void HandleKeyInput(int fieldIndex, KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Backspace:
                if (string.IsNullOrEmpty(inputFields[fieldIndex].text))
                {
                    MoveToPreviousField(fieldIndex);
                }
                break;

            case KeyCode.LeftArrow:
                MoveToPreviousField(fieldIndex);
                break;

            case KeyCode.RightArrow:
                MoveToNextField(fieldIndex);
                break;
        }
    }

    void Update()
    {
        // 处理粘贴操作
        if (Input.inputString.Length > 1)
        {
            string pastedText = Input.inputString.Where(char.IsDigit).Take(inputFields.Count).Aggregate("", (a, b) => a + b);
            if (pastedText.Length > 1)
            {
                SetCode(pastedText);
            }
        }
    }
}

// 辅助组件：处理每个TMP_InputField的特殊输入
public class TMPInputFieldHandler : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private VerificationCodeInput parentController;
    private int fieldIndex;
    private TMP_InputField inputField;

    public void Initialize(VerificationCodeInput parent, int index)
    {
        parentController = parent;
        fieldIndex = index;
        inputField = GetComponent<TMP_InputField>();
    }

    void Update()
    {
        if (inputField != null && inputField.isFocused)
        {
            // 处理特殊按键
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                parentController.HandleKeyInput(fieldIndex, KeyCode.Backspace);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                parentController.HandleKeyInput(fieldIndex, KeyCode.LeftArrow);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                parentController.HandleKeyInput(fieldIndex, KeyCode.RightArrow);
            }
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        parentController.OnInputSelected(fieldIndex);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        parentController.OnInputDeselected(fieldIndex);
    }
}