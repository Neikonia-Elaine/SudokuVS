using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class EventTriggerListener : EventTrigger
{
    public Action<PointerEventData> onEnter;
    public Action<PointerEventData> onExit;

    public static EventTriggerListener Get(GameObject go)
    {
        var listener = go.GetComponent<EventTriggerListener>();
        if (listener == null)
            listener = go.AddComponent<EventTriggerListener>();
        return listener;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        onEnter?.Invoke(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        onExit?.Invoke(eventData);
    }
}
