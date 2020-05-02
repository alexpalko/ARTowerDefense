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
        _UpdateButtonStates();
    }

    private void _UpdateButtonStates()
    {
        BuildButton.SetActive(false);
        SelectButton.SetActive(false);
        DemolishButton.SetActive(m_SelectedBuildingDivision != null);
        m_FocusedDivision = null;
        m_FocusedDivision = null;

        var ray = new Ray(FirstPersonCamera.transform.position, FirstPersonCamera.transform.forward);
        var hits = Physics.RaycastAll(ray);

        if (!hits.Any()) return;

        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag("Division") || m_BuildingToConstructId < 0) continue;

            Debug.Log("Hit division collider.");

            if (m_Divisions.Contains(hit.collider.gameObject.transform.parent.gameObject))
            {
                m_FocusedDivision = hit.collider.transform.parent.gameObject;
            }

            if (m_FocusedDivision == null) return;
            var buildingDiv = m_FocusedDivision.GetComponent<BuildingDivision>();

            if (!buildingDiv.HasNature &&
                !buildingDiv.HasBuilding)
            {
                BuildButton.SetActive(true);
                break;
            }

            if (buildingDiv.HasBuilding)
            {
                SelectButton.SetActive(true);
            }

            break;
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
