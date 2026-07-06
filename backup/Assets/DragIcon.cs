using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragIcon : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    const float TapThresholdPixels = 24f;

    public DragContainer ContainerIcon;
    public Image InnerIcon;
    public int indexData = 0;

    bool isDragging;
    bool handledThisGesture;

    void Awake()
    {
        SyncIndexDataFromParent();
    }

    public void SyncIndexDataFromParent()
    {
        var ss = GetComponentInParent<SoundSelect>();
        if (ss == null || ss.Icons == null)
            return;

        for (int i = 0; i < ss.Icons.Length; i++) {
            if (ss.Icons[i] == null || ss.Icons[i].transform.childCount == 0)
                continue;
            if (ss.Icons[i].transform.GetChild(0).gameObject != gameObject)
                continue;

            indexData = i + 1;
            return;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        handledThisGesture = false;
        SyncIndexDataFromParent();
        if (indexData <= 0 || ContainerIcon == null)
            return;

        var button = GetComponent<Button>();
        var image = GetComponent<Image>();
        var sprite = button != null && button.image != null ? button.image.sprite : image != null ? image.sprite : null;
        if (sprite == null)
            return;

        ContainerIcon.sourceIcon = this;
        ContainerIcon.indexData = indexData;
        ContainerIcon.gameObject.GetComponent<Image>().sprite = sprite;
        InnerIcon.sprite = sprite;
        ContainerIcon.GetComponent<Image>().SetNativeSize();
        InnerIcon.rectTransform.sizeDelta = image != null ? image.rectTransform.sizeDelta : InnerIcon.rectTransform.sizeDelta;
        InnerIcon.SetNativeSize();

        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && ContainerIcon != null)
            ContainerIcon.transform.position = eventData.pointerCurrentRaycast.worldPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) {
            CleanupDragVisual();
            return;
        }

        SyncIndexDataFromParent();
        var dist = Vector2.Distance(eventData.pressPosition, eventData.position);
        if (dist < TapThresholdPixels)
            TryAssignToEmptySlot();
        else
            TryDropAtPointer(eventData);

        handledThisGesture = true;
        CleanupDragVisual();
        isDragging = false;
    }

    void CleanupDragVisual()
    {
        if (ContainerIcon == null)
            return;

        ContainerIcon.indexData = 0;
        ContainerIcon.sourceIcon = null;
        ContainerIcon.resetSprite();
        InnerIcon.transform.position = new Vector3(10000f, 10000f, 10000f);
        ContainerIcon.transform.position = new Vector3(10000f, 10000f, 10000f);
    }

    void TryDropAtPointer(PointerEventData eventData)
    {
        if (ContainerIcon == null || ContainerIcon.sourceIcon == null)
            return;

        var source = ContainerIcon.sourceIcon;
        if (source.IsAssignedToSlot())
            return;

        DropIcon best = null;
        var bestDist = float.MaxValue;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++) {
            var drop = results[i].gameObject.GetComponent<DropIcon>()
                ?? results[i].gameObject.GetComponentInParent<DropIcon>();
            if (drop == null || drop.isDropped)
                continue;

            var rect = drop.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventData.pressEventCamera))
                continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, eventData.position, eventData.pressEventCamera, out var local);
            var dist = local.sqrMagnitude;
            if (dist < bestDist) {
                bestDist = dist;
                best = drop;
            }
        }

        if (best != null)
            best.AssignInstrument(source);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging || isDragging || handledThisGesture || indexData <= 0)
            return;

        TryAssignToEmptySlot();
    }

    void TryAssignToEmptySlot()
    {
        SyncIndexDataFromParent();
        if (indexData <= 0 || IsAssignedToSlot())
            return;

        var ss = GetComponentInParent<SoundSelect>();
        if (ss == null || ss.slots == null)
            return;

        for (int i = 0; i < ss.slots.Length; i++) {
            var slot = ss.slots[i];
            if (slot == null || slot.backIcons == null)
                continue;

            var drop = slot.backIcons.GetComponent<DropIcon>();
            if (drop != null && drop.AssignInstrument(this))
                return;
        }
    }

    public bool IsAssignedToSlot()
    {
        var ss = GetComponentInParent<SoundSelect>();
        if (ss == null || ss.slots == null)
            return false;

        for (int i = 0; i < ss.slots.Length; i++) {
            var slot = ss.slots[i];
            if (slot == null || slot.backIcons == null)
                continue;

            var drop = slot.backIcons.GetComponent<DropIcon>();
            if (drop != null && drop.isDropped && drop.gm == gameObject)
                return true;
        }

        return false;
    }
}
