using System;
using UnityEngine;

public class BuildingDivision : MonoBehaviour
{
    public bool IsLocked { get; private set; }
    public bool HasBuilding { get; private set; }
    public bool HasNature { get; private set; }

    private GameObject m_Building;
    private GameObject m_Nature;

    public void Lock()
    {
        IsLocked = true;
    }

    public bool AddBuilding(GameObject buildingPrefab)
    {
        if (IsLocked || HasNature || HasBuilding) return false;
        m_Building = Instantiate(buildingPrefab, transform.position, Quaternion.identity, transform);
        HasBuilding = true;
        return true;
    }

    public void RemoveBuilding()
    {
        if (m_Building == null || IsLocked) return;
        Destroy(m_Building);
        m_Building = null;
        HasBuilding = false;
    }

    private void AddNature()
    {
        throw new NotImplementedException();
    }

    public void RemoveNature()
    {
        if(m_Nature == null) return;
        Destroy(m_Nature);
        m_Nature = null;
        HasNature = false;
    }

    public void ClearNature()
    {
        Destroy(m_Nature);
        HasNature = true;
    }
}
