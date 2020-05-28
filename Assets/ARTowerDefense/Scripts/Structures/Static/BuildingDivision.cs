using ARTowerDefense.Structures.Dynamic;
using UnityEngine;

public class BuildingDivision : MonoBehaviour
{
    public bool IsLocked { get; private set; }
    public bool HasBuilding { get; private set; }
    public bool HasNature { get; private set; }

    private GameObject m_ContainedStructure;
    private GameObject m_TransparentStructure;

    public void Lock()
    {
        IsLocked = true;
    }

    public void AddBuilding(GameObject buildingPrefab)
    {
        if (!AddContainedStructure(buildingPrefab, 0)) return;
        HasBuilding = true;
    }

    public void AddBuilding(GameObject buildingPrefab, float yAxisRotation)
    {
        if (!AddContainedStructure(buildingPrefab, yAxisRotation)) return;
        HasBuilding = true;
    }
   
    public void AddNature(GameObject naturePrefab)
    {
        if (!AddContainedStructure(naturePrefab, 0)) return;
        m_ContainedStructure.transform.Rotate(0,new System.Random().Next(360),0);
        HasNature = true;
    }

    private bool AddContainedStructure(GameObject buildingPrefab, float yAxisRotation)
    {
        if (IsLocked || HasNature || HasBuilding) return false;
        m_ContainedStructure = Instantiate(buildingPrefab, transform.position, Quaternion.identity, transform);
        m_ContainedStructure.transform.eulerAngles = new Vector3(0, yAxisRotation, 0);
        return true;
    }
    
    public void Clear()
    {
        if (m_ContainedStructure == null || IsLocked) return;
        Destroy(m_ContainedStructure);
        m_ContainedStructure = null;
        HasBuilding = HasNature = false;
    }

    public void ShowHoverOutline()
    {
        if (m_ContainedStructure == null) return;
        var outlineControllers = m_ContainedStructure.GetComponentsInChildren<OutlineController>();
        foreach (var outlineController in outlineControllers)
        {
            outlineController.ShowHoverOutline();
        }
    }

    public void ShowSelectedOutline()
    {
        if (m_ContainedStructure == null) return;
        var outlineControllers = m_ContainedStructure.GetComponentsInChildren<OutlineController>();
        foreach (var outlineController in outlineControllers)
        {
            outlineController.ShowSelectedOutline();
        }
    }

    public void HideOutline()
    {
        if (m_ContainedStructure == null) return;
        var outlineControllers = m_ContainedStructure.GetComponentsInChildren<OutlineController>();
        foreach (var outlineController in outlineControllers)
        {
            outlineController.HideOutline();
        }
    }

    public void ShowValidTransparentStructure(GameObject prefab)
    {
        m_TransparentStructure = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        m_TransparentStructure.GetComponent<TransparencyController>().ShowValidPlacementColor();
    }

    public void ShowInvalidTransparentStructure(GameObject prefab)
    {
        m_TransparentStructure = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        m_TransparentStructure.GetComponent<TransparencyController>().ShowInvalidPlacementColor();
    }

    public void ClearTransparentStructure()
    {
        Destroy(m_TransparentStructure);
    }
}
