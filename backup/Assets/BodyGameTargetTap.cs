using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tap instruments to register hits (primary input on MainGame).
/// </summary>
public class BodyGameTargetTap : MonoBehaviour, IPointerDownHandler
{
    public BodyGameScene scene;
    public int index;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (scene != null && scene.isActiveAndEnabled)
            scene.HitTarget(index);
    }
}
