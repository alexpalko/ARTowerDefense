using UnityEngine;

public class BuildingDivision : MonoBehaviour
{
    public bool IsLocked { get; private set; }
    public bool HasBuilding { get; private set; }
    public bool HasNature { get; private set; }

    private GameObject m_ContainedStructure;

    public void Lock()
    {
        IsLocked = true;
    }

    public void AddBuilding(GameObject buildingPrefab)
    {
        if (!AddContainedStructure(buildingPrefab)) return;
        HasBuilding = true;
    }
    public void AddNature(GameObject naturePrefab)
    {
        if (!AddContainedStructure(naturePrefab)) return;
        m_ContainedStructure.transform.Rotate(0,new System.Random().Next(360),0);
        HasNature = true;
    }

    private bool AddContainedStructure(GameObject buildingPrefab)
    {
        if (IsLocked || HasNature || HasBuilding) return false;
        m_ContainedStructure = Instantiate(buildingPrefab, transform.position, Quaternion.identity, transform);
        return true;
    }
    
    public void Clear()
    {
        if (m_ContainedStructure == null || IsLocked) return;
        Destroy(m_ContainedStructure);
        m_ContainedStructure = null;
        HasBuilding = HasNature = false;
    }
}
