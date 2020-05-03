using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BuildingManager : MonoBehaviour
{
    public Camera FirstPersonCamera;
    public GameObject TriggerBuildingsPanelButton;
    public GameObject BuildButton;
    public GameObject SelectButton;
    public GameObject DemolishButton;
    public GameObject BuildingsPanel;
    public List<GameObject> BuildingInfoPanels;

    public GameObject[] BuildingPrefabs;
    public GameObject[] TransparentBuildingPrefabs;
    public int[] PriceList = {25, 60, 150, 50, 80, 200};
    public int NatureRemovalCost;

    private List<BuildingDivision> m_Divisions;
    private BuildingDivision m_FocusedDivision; 
    private int m_BuildingToConstructId = -1;
    private BuildingDivision m_SelectedBuildingDivision;

    void OnEnable()
    {
        TriggerBuildingsPanelButton.SetActive(true);
        m_Divisions = Master.DivisionGameObjectDictionary.Values.ToList();
    }

    void Update()
    {
        if (!TryGetDivisionHit(out var hit)) return;
        _UpdateButtonStates(hit);
    }

    private bool TryGetDivisionHit(out RaycastHit hit)
    {
        var ray = new Ray(FirstPersonCamera.transform.position, FirstPersonCamera.transform.forward);
        var any = false;
        hit = Physics.RaycastAll(ray).FirstOrDefault(h =>
        {
            if (!h.collider.CompareTag("Division")) return false;
            any = true;
            return true;

        });

        return any;
    }

    private void _UpdateButtonStates(RaycastHit hit)
    {
        BuildButton.SetActive(false);
        SelectButton.SetActive(false);
        DemolishButton.SetActive(m_SelectedBuildingDivision != null);
        if (m_FocusedDivision != null)
        {
            m_FocusedDivision.ClearTransparentStructure();
            if (!m_FocusedDivision.Equals(m_SelectedBuildingDivision))
            {
                m_FocusedDivision.HideOutline();
            }
        }

        m_FocusedDivision = null;

        if (m_Divisions.Contains(hit.collider.transform.parent.GetComponent<BuildingDivision>()))
        {
            m_FocusedDivision = hit.collider.transform.parent.gameObject.GetComponent<BuildingDivision>();
        }

        if (m_FocusedDivision == null)
        {
            return;
        }

        if (m_FocusedDivision.IsLocked)
        {
            if (m_BuildingToConstructId != -1)
            {
                m_FocusedDivision.ShowInvalidTransparentStructure(TransparentBuildingPrefabs[m_BuildingToConstructId]);
            }
            return;
        }

        if (m_BuildingToConstructId != -1)
        {
            if (!m_FocusedDivision.HasNature &&
                !m_FocusedDivision.HasBuilding)
            {
                BuildButton.SetActive(true);
                m_FocusedDivision.ShowValidTransparentStructure(TransparentBuildingPrefabs[m_BuildingToConstructId]);
            }
            else
            {
                m_FocusedDivision.ShowInvalidTransparentStructure(TransparentBuildingPrefabs[m_BuildingToConstructId]);
            }
        }

        if (m_FocusedDivision.HasNature || m_FocusedDivision.HasBuilding)
        {
            SelectButton.SetActive(true);
            if (!m_FocusedDivision.Equals(m_SelectedBuildingDivision))
            {
                m_FocusedDivision.ShowHoverOutline();
            }
        }
    }

    public void SelectBuildingToConstruct(int x)
    {
        if (x < 0 || x > 7)
        {
            Debug.LogError($"Invalid building code: {x}");
            return;
        }
        m_BuildingToConstructId = x;
    }

    public void Construct()
    {
        if (!CoinManager.RemoveCoins(PriceList[m_BuildingToConstructId]))
        {
            return;
        }
        m_FocusedDivision.GetComponent<BuildingDivision>().AddBuilding(BuildingPrefabs[m_BuildingToConstructId]);
        m_FocusedDivision.ClearTransparentStructure();
    }

    public void Select()
    {
        if(m_FocusedDivision.IsLocked) return;
        if (m_SelectedBuildingDivision != null)
        {
            m_SelectedBuildingDivision.HideOutline();
        }
        m_SelectedBuildingDivision = m_FocusedDivision;
        m_FocusedDivision.ShowSelectedOutline();
        DemolishButton.SetActive(true);
        m_BuildingToConstructId = -1;
    }

    public void Demolish()
    {
        if (m_SelectedBuildingDivision.HasNature)
        {
            if (CoinManager.Coins >= NatureRemovalCost)
            {
                CoinManager.RemoveCoins(NatureRemovalCost);
            }
            else
            {
                return;
            }
        }
        m_SelectedBuildingDivision.Clear();
        m_SelectedBuildingDivision = null;
    }

    public void UpdateBuildingsPanelStatus()
    {
        BuildingsPanel.SetActive(!BuildingsPanel.activeSelf);
        Debug.Log($"Set buildings panel status to: {BuildingsPanel.activeSelf}");
    }

    public void UpdateBuildingInfoPanelsStatus(GameObject currentInfoPanel)
    {
        currentInfoPanel.SetActive(!currentInfoPanel.activeSelf);
        foreach (var infoPanel in BuildingInfoPanels)
        {
            if (infoPanel.Equals(currentInfoPanel)) continue;
            infoPanel.SetActive(false);
        }
    }
}
