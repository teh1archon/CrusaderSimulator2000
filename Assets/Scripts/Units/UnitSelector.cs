using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug tool for unit selection and inspection.
/// Units are AUTONOMOUS per GDD - this does NOT control movement.
/// Used for testing/debugging to see unit stats and state.
/// Enable debugForceMove to temporarily override autonomous behavior for testing.
/// </summary>
public class UnitSelector : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private bool enableSelection = true;

    [Header("Debug Tools")]
    [Tooltip("Enable direct movement control for testing (bypasses autonomous behavior)")]
    [SerializeField] private bool debugForceMove = false;
    [SerializeField] private bool showDebugInfo = true;

    private List<Unit> selectedUnits = new List<Unit>();
    private Camera mainCamera;

    // Events for UI to display selection
    public event System.Action<List<Unit>> OnSelectionChanged;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!enableSelection) return;

        // Left-click to select
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectUnit();
        }

        // Right-click for debug movement (only if enabled)
        if (debugForceMove && Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            DebugCommandMovement();
        }

        // Escape to deselect all
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectAll();
        }
    }

    private void TrySelectUnit()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, unitLayer);

        // Multi-select with Shift
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeselectAll();
        }

        if (hit.collider != null)
        {
            var unit = hit.collider.GetComponent<Unit>();
            if (unit != null && !selectedUnits.Contains(unit))
            {
                SelectUnit(unit);
            }
        }
    }

    private void SelectUnit(Unit unit)
    {
        selectedUnits.Add(unit);

        // Update visual selection indicator
        var visuals = unit.GetComponent<UnitVisuals>();
        if (visuals != null)
        {
            visuals.SetSelected(true);
        }

        OnSelectionChanged?.Invoke(selectedUnits);

        if (showDebugInfo)
        {
            Debug.Log($"Selected: {unit.ClassName} | HP: {unit.CurrentHealth}/{unit.MaxHealth} | State: {unit.CurrentState}");
        }
    }

    private void DeselectUnit(Unit unit)
    {
        selectedUnits.Remove(unit);

        var visuals = unit.GetComponent<UnitVisuals>();
        if (visuals != null)
        {
            visuals.SetSelected(false);
        }
    }

    public void DeselectAll()
    {
        foreach (var unit in selectedUnits)
        {
            if (unit != null)
            {
                var visuals = unit.GetComponent<UnitVisuals>();
                if (visuals != null)
                {
                    visuals.SetSelected(false);
                }
            }
        }
        selectedUnits.Clear();
        OnSelectionChanged?.Invoke(selectedUnits);
    }

    /// <summary>
    /// DEBUG ONLY: Force selected units to move (bypasses autonomous AI)
    /// </summary>
    private void DebugCommandMovement()
    {
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        targetPos.z = 0f;

        Debug.Log($"[DEBUG] Force moving {selectedUnits.Count} units to {targetPos}");

        if (selectedUnits.Count == 1)
        {
            ForceUnitMove(selectedUnits[0], targetPos);
        }
        else
        {
            // Formation movement
            float spacing = 1.2f;
            int unitsPerRow = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));

            for (int i = 0; i < selectedUnits.Count; i++)
            {
                int row = i / unitsPerRow;
                int col = i % unitsPerRow;

                float offsetX = (col - (unitsPerRow - 1) / 2f) * spacing;
                float offsetY = (row - (selectedUnits.Count / unitsPerRow - 1) / 2f) * spacing;

                Vector3 formationPos = targetPos + new Vector3(offsetX, offsetY, 0f);
                ForceUnitMove(selectedUnits[i], formationPos);
            }
        }
    }

    private void ForceUnitMove(Unit unit, Vector3 position)
    {
        if (unit == null) return;

        var movement = unit.GetComponent<UnitMovement>();
        if (movement != null)
        {
            movement.MoveTo(position);
        }
    }

    // Public accessors
    public List<Unit> GetSelectedUnits() => new List<Unit>(selectedUnits);
    public int SelectionCount => selectedUnits.Count;
    public bool HasSelection => selectedUnits.Count > 0;

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showDebugInfo || selectedUnits.Count == 0) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label($"<b>Selected Units: {selectedUnits.Count}</b>");

        foreach (var unit in selectedUnits)
        {
            if (unit == null) continue;
            GUILayout.Label($"  {unit.ClassName}: {unit.CurrentHealth}/{unit.MaxHealth} HP - {unit.CurrentState}");
        }

        if (debugForceMove)
        {
            GUILayout.Label("<color=yellow>[DEBUG MODE] Right-click to force move</color>");
        }

        GUILayout.EndArea();
    }
#endif
}
