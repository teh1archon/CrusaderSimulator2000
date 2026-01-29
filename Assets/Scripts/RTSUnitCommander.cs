using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RTSUnitCommander : MonoBehaviour
{
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private LayerMask groundLayer;
    
    private List<RTSUnitMovement> selectedUnits = new List<RTSUnitMovement>();
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Left-click to select
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectUnit();
        }
        
        // Right-click to command movement
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            CommandMovement();
        }
    }

    private void TrySelectUnit()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, unitLayer);
        
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            selectedUnits.Clear();
        }
        
        if (hit.collider != null)
        {
            var unit = hit.collider.GetComponent<RTSUnitMovement>();
            if (unit != null && !selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
            }
        }
    }

    private void CommandMovement()
    {
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        targetPos.z = 0f;
        
        if (selectedUnits.Count == 1)
        {
            selectedUnits[0].MoveTo(targetPos);
        }
        else
        {
            // Formation movement - spread units around target point
            MoveInFormation(targetPos);
        }
    }

    private void MoveInFormation(Vector3 center)
    {
        float spacing = 1.2f; // Adjust based on unit size
        int unitsPerRow = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
        
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            int row = i / unitsPerRow;
            int col = i % unitsPerRow;
            
            // Center the formation
            float offsetX = (col - (unitsPerRow - 1) / 2f) * spacing;
            float offsetY = (row - (selectedUnits.Count / unitsPerRow - 1) / 2f) * spacing;
            
            Vector3 formationPos = center + new Vector3(offsetX, offsetY, 0f);
            selectedUnits[i].MoveTo(formationPos);
        }
    }
}