using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropIcon : MonoBehaviour, IDropHandler
{
    public DragContainer ContainerIcon;
    public GameObject gm;
    public bool isDropped = false;
    public DragSlot ds;

    Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ResetPosition);
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Assignment is handled once in DragIcon.OnEndDrag to avoid double drops.
    }

    public bool AssignInstrument(DragIcon source)
    {
        if (source == null || source.indexData <= 0 || isDropped || ds == null)
            return false;

        if (source.IsAssignedToSlot())
            return false;

        gm = source.gameObject;
        original = gm.transform.position;
        gm.transform.position = transform.position;
        isDropped = true;
        ds.isFull = true;
        ds.indexData = source.indexData;
        return true;
    }

    public bool AssignInstrument(int instrumentIndex)
    {
        var ss = GetComponentInParent<SoundSelect>();
        if (ss == null)
            ss = FindFirstObjectByType<SoundSelect>();
        if (ss == null || ss.Icons == null || instrumentIndex <= 0 || instrumentIndex > ss.Icons.Length)
            return false;

        var iconParent = ss.Icons[instrumentIndex - 1];
        if (iconParent == null || iconParent.transform.childCount == 0)
            return false;

        var source = iconParent.transform.GetChild(0).GetComponent<DragIcon>();
        return source != null && AssignInstrument(source);
    }

    Vector3 original;

    public void ResetPosition()
    {
        if (!isDropped || gm == null)
            return;

        gm.transform.position = original;
        ds.resetIcon();
        isDropped = false;
        gm = null;
    }
}
