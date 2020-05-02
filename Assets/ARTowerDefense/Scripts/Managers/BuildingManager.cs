using System;
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
    public bool UseDivisionHoverHighlight;
    public int NatureRemovalCost;

    public GameObject CannonTowerPrefab;
    public GameObject CrossbowTowerPrefab;
    public GameObject ThunderTowerPrefab;
    public GameObject CropFarmPrefab;
    public GameObject ChickenFarmPrefab;
    public GameObject MillPrefab;

    public GameObject[] TransparentBuildingPrefabs;
    private readonly int[] m_PriceList = {25, 60, 150, 50, 80, 200};

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
        if (UseDivisionHoverHighlight)
        {
            _HighlightDivisions();
        }
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
        else if (m_FocusedDivision.HasNature || m_FocusedDivision.HasBuilding)
        {
            SelectButton.SetActive(true);
            if (!m_FocusedDivision.Equals(m_SelectedBuildingDivision))
            {
                m_FocusedDivision.ShowHoverOutline();
            }
        }
    }

    private void _HighlightDivisions()
    {
        foreach (var division in m_Divisions)
        {
            _ClearDivisionHighlight(division.GetComponentInChildren<Renderer>());
        }

        if (m_FocusedDivision == null) return;

        foreach (var division in m_Divisions)
        {
            var distance = _GetMagnitude(division.transform, m_FocusedDivision.transform);
            if (distance <= .1 * Math.Sqrt(2) / 2)
            {
                _HighlightDivision(division.GetComponentInChildren<Renderer>(), .5f);
            }
            else if (distance <= .1 * Math.Sqrt(2) / 2 + .1 * Math.Sqrt(2))
            {
                _HighlightDivision(division.GetComponentInChildren<Renderer>(), .1f);
            }
            else if (distance <= .1 * Math.Sqrt(2) / 2 + .1 * Math.Sqrt(2) * 2)
            {
                _HighlightDivision(division.GetComponentInChildren<Renderer>(), .005f);
            }
        }
    }

    private void _HighlightDivision(Renderer rend, float alpha)
    {
        if (!m_FocusedDivision.HasNature &&
            !m_FocusedDivision.HasBuilding)
        {
            rend.material.color = new Color(0, 255, 0, alpha);
        }
        else
        {
            rend.material.color = new Color(255, 0, 0, alpha);
        }

        rend.enabled = true;
    }

    private void _ClearDivisionHighlight(Renderer rend)
    {
        rend.enabled = false;
    }

    private float _GetMagnitude(Transform t1, Transform t2)
    {
        return (t1.position - t2.position).magnitude;
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
        GameObject buildingToConstruct; 
        switch(m_BuildingToConstructId)
        {
            case 0:
                buildingToConstruct = CrossbowTowerPrefab;
                break;
            case 1:
                buildingToConstruct = CannonTowerPrefab;
                break;
            case 2:
                buildingToConstruct = ThunderTowerPrefab;
                break;
            case 3:
                buildingToConstruct = CropFarmPrefab;
                break;
            case 4:
                buildingToConstruct = ChickenFarmPrefab;
                break;
            case 5:
                buildingToConstruct = MillPrefab;
                break;
            default:
                buildingToConstruct = null;
                break;
        }

        if (buildingToConstruct == null)
        {
            Debug.LogError("Building prefab was null");
            m_BuildingToConstructId = -1;
        }

        if (!CoinManager.RemoveCoins(m_PriceList[m_BuildingToConstructId]))
        {
            return;
        }

        m_FocusedDivision.GetComponent<BuildingDivision>().AddBuilding(buildingToConstruct);
        m_FocusedDivision.ClearTransparentStructure();
        //m_FocusedDivision = null;
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
