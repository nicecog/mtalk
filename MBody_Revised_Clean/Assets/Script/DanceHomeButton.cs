using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-right home button on dance selection pages → BodyDanceSelect (post-login hub).
/// </summary>
public class DanceHomeButton : MonoBehaviour
{
    const string ButtonName = "RuntimeHomeButton";

    void OnEnable() {
        EnsureHomeButton();
    }

    void EnsureHomeButton() {
        var existing = transform.Find(ButtonName);
        if (existing != null) {
            existing.gameObject.SetActive(true);
            existing.SetAsLastSibling();
            return;
        }

        var go = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        // DanceSelect uses center-anchored zero-size root; match other HUD homes.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(1250f, 780f);
        rect.sizeDelta = new Vector2(120f, 120f);

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.raycastTarget = true;
        TryApplyHomeSprite(image);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(GoHome);

        // House glyph fallback when no sprite is available.
        if (image.sprite == null) {
            image.color = new Color(0.15f, 0.55f, 0.85f, 0.95f);
            var labelGo = new GameObject("Icon", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "⌂";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 72;
            label.color = Color.white;
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        go.transform.SetAsLastSibling();
        MBodyDiagLog.Step("DanceHome", $"Home button created on '{name}'");
    }

    static void TryApplyHomeSprite(Image image) {
        // Reuse an existing Home button sprite from the loaded scenes when present.
        var images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < images.Length; i++) {
            var img = images[i];
            if (img == null || img.sprite == null)
                continue;
            if (img.gameObject.name == "Home" || img.gameObject.name.StartsWith("Home ")) {
                image.sprite = img.sprite;
                image.preserveAspect = true;
                return;
            }
        }
    }

    void GoHome() {
        MBodyDiagLog.Step("DanceHome", $"Home clicked from '{name}' -> BodyDanceSelect");
        BodyResourceLifecycle.ReleaseBodyCapture();

        var home = SceneFlowRegistry.Get("BodyDanceSelect");
        if (home == null) {
            MBodyDiagLog.Warn("DanceHome", "BodyDanceSelect page not found");
            return;
        }

        SceneFlowNavigator.ShowOnly(home, gameObject);
    }
}
