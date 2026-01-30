using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles visual feedback for units: health bar, selection indicator, state icons.
/// Attach to the same GameObject as Unit component.
/// </summary>
[RequireComponent(typeof(Unit))]
public class UnitVisuals : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Selection")]
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Color playerColor = Color.cyan;
    [SerializeField] private Color enemyColor = Color.red;

    [Header("State Icons")]
    [SerializeField] private SpriteRenderer stateIcon;

    // Runtime references
    private Unit unit;
    private Canvas healthBarCanvas;
    private Image healthBarFill;
    private Image healthBarBackground;
    private bool isSelected;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    private void Start()
    {
        CreateHealthBar();
        SetupSelectionIndicator();

        // Subscribe to unit events
        unit.OnHealthChanged += UpdateHealthBar;
        unit.OnStateChanged += UpdateStateIcon;
        unit.OnDeath += OnUnitDeath;

        // Initial update
        UpdateHealthBar(unit.CurrentHealth, unit.MaxHealth);
    }

    private void OnDestroy()
    {
        if (unit != null)
        {
            unit.OnHealthChanged -= UpdateHealthBar;
            unit.OnStateChanged -= UpdateStateIcon;
            unit.OnDeath -= OnUnitDeath;
        }
    }

    private void LateUpdate()
    {
        // Keep health bar above unit (world space canvas would handle this automatically)
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.position = transform.position + healthBarOffset;
        }
    }

    #region Health Bar

    private void CreateHealthBar()
    {
        if (healthBarPrefab != null)
        {
            var healthBarObj = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            healthBarCanvas = healthBarObj.GetComponent<Canvas>();
            healthBarFill = healthBarObj.GetComponentInChildren<Image>();
            return;
        }

        // Create simple health bar dynamically
        var canvasObj = new GameObject("HealthBar");
        canvasObj.transform.SetParent(null); // World space
        canvasObj.transform.position = transform.position + healthBarOffset;

        healthBarCanvas = canvasObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.sortingOrder = 10;

        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 100;

        var rectTransform = healthBarCanvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1f, 0.15f);
        rectTransform.localScale = Vector3.one;

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        healthBarBackground = bgObj.AddComponent<Image>();
        healthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = healthyColor;
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        fillRect.pivot = new Vector2(0, 0.5f);
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthBarFill == null) return;

        float healthPercent = max > 0 ? (float)current / max : 0f;

        // Update fill amount
        var fillRect = healthBarFill.GetComponent<RectTransform>();
        fillRect.anchorMax = new Vector2(healthPercent, 1f);

        // Update color based on health
        if (healthPercent > 0.6f)
            healthBarFill.color = healthyColor;
        else if (healthPercent > 0.3f)
            healthBarFill.color = damagedColor;
        else
            healthBarFill.color = criticalColor;

        // Hide health bar if full
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(healthPercent < 1f);
        }
    }

    #endregion

    #region Selection

    private void SetupSelectionIndicator()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
            var renderer = selectionIndicator.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = unit.IsPlayerUnit ? playerColor : enemyColor;
            }
            return;
        }

        // Create simple selection circle
        selectionIndicator = new GameObject("SelectionIndicator");
        selectionIndicator.transform.SetParent(transform, false);
        selectionIndicator.transform.localPosition = Vector3.zero;

        var sr = selectionIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = unit.IsPlayerUnit ? playerColor : enemyColor;
        sr.sortingOrder = -1;
        selectionIndicator.transform.localScale = Vector3.one * 1.5f;

        selectionIndicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(selected);
        }
    }

    public bool IsSelected => isSelected;

    #endregion

    #region State Icons

    private void UpdateStateIcon(UnitState state)
    {
        // TODO: Show state icons (defending shield, attacking sword, etc.)
        if (stateIcon == null) return;

        switch (state)
        {
            case UnitState.Attacking:
                stateIcon.color = Color.red;
                break;
            case UnitState.Defending:
                stateIcon.color = Color.blue;
                break;
            default:
                stateIcon.color = Color.clear;
                break;
        }
    }

    #endregion

    #region Death

    private void OnUnitDeath(Unit deadUnit)
    {
        // Clean up visuals
        if (healthBarCanvas != null)
        {
            Destroy(healthBarCanvas.gameObject);
        }

        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }

        // Fade out sprite
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            StartCoroutine(FadeOut(sr));
        }
    }

    private System.Collections.IEnumerator FadeOut(SpriteRenderer sr)
    {
        float duration = 1f;
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
    }

    #endregion

    #region Helpers

    private Sprite CreateCircleSprite()
    {
        // Create a simple circle texture
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2;
        float innerRadius = radius - 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius && dist > innerRadius)
                    colors[y * size + x] = Color.white;
                else
                    colors[y * size + x] = Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    #endregion
}
