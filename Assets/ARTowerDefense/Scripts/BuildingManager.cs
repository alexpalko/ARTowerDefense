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

    public GameObject CannonTowerPrefab;
    public GameObject CrossbowTowerPrefab;
    public GameObject ThunderTowerPrefab;
    public GameObject CropFarmPrefab;
    public GameObject ChickenFarmPrefab;
    public GameObject MillPrefab;

    private readonly int[] m_PriceList = {25, 60, 150, 50, 80, 200};

    private List<GameObject> m_Divisions;

    private GameObject m_FocusedDivision;

    private int m_BuildingToConstructId = -1;
    private GameObject m_SelectedBuildingDivision;

    void OnEnable()
    {
        TriggerBuildingsPanelButton.SetActive(true);
        m_Divisions = Master.AvailableDivisionObjects.ToList();
    }

    void Update()
    {


        if (TryGetDivisionHit(out var hit))
        {
            _UpdateButtonStates(hit);
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
        m_FocusedDivision = null;
        if (m_BuildingToConstructId < 0) return;

        if (m_Divisions.Contains(hit.collider.transform.parent.gameObject))
        {
            m_FocusedDivision = hit.collider.transform.parent.gameObject;
        }

        if (m_FocusedDivision == null) return;
        var buildingDiv = m_FocusedDivision.GetComponent<BuildingDivision>();

        if (!buildingDiv.HasNature &&
            !buildingDiv.HasBuilding)
        {
            BuildButton.SetActive(true);
        }
        else if (buildingDiv.HasBuilding)
        {
            SelectButton.SetActive(true);
        }
    }

    private List<Renderer> m_HighlightedDivRenderers;

    private void _HighlightDivisions()
    {
        if (m_FocusedDivision == null)
        {
            foreach (var division in m_Divisions)
            {
                _ClearDivisionHighlight(division.GetComponentInChildren<Renderer>());
            }
            return;
        }

        foreach (var division in m_Divisions)
        {
            var distance = _GetSqrMagnitude(division.transform, m_FocusedDivision.transform);
            float alpha = distance < .01f ? 0 : .75f * .05f / distance;
            _HighlightDivision(division.GetComponent<BuildingDivision>(), division.GetComponentInChildren<Renderer>(),
                alpha);
        }

        //var rend = hit.collider.gameObject.GetComponent<Renderer>();
        
        //var rends = _GetNeighborRenderers();
        //m_HighlightedDivRenderers.Add(rend);

        //var buildingDiv = rend.transform.parent.GetComponent<BuildingDivision>();
        //if (!buildingDiv.HasNature &&
        //    !buildingDiv.HasBuilding)
        //{
        //    rend.material.color = new Color(0, 255, 0, .5f);
        //}
        //else
        //{
        //    rend.material.color = new Color(255, 0, 0, .5f);
        //}

        //rend.enabled = true;

        //foreach (var renderer1 in rends)
        //{
        //    buildingDiv = renderer1.transform.parent.GetComponent<BuildingDivision>();
        //    _HighlightDivision(renderer1.transform.parent.GetComponent<BuildingDivision>(), renderer1, .25f);
        //}
    }

    private void _HighlightDivision(BuildingDivision buildingDiv, Renderer rend, float alpha)
    {
        if (!buildingDiv.HasNature &&
            !buildingDiv.HasBuilding)
        {
            rend.material.color = new Color(0, 255, 0, alpha);
        }
        else
        {
            rend.material.color = new Color(255, 0, 0, alpha);
        }
    }

    private void _ClearDivisionHighlight(Renderer rend)
    {
        rend.material.color = new Color(0,0,0,0);
    }

    private float _GetSqrMagnitude(Transform t1, Transform t2)
    {
        return (t1.position - t2.position).sqrMagnitude;
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
        m_FocusedDivision = null;
    }

    public void Select()
    {
        m_SelectedBuildingDivision = m_FocusedDivision;
        DemolishButton.SetActive(true);
    }

    public void Demolish()
    {
        m_SelectedBuildingDivision.GetComponent<BuildingDivision>().RemoveBuilding();
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
