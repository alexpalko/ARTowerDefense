using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using Assets.ARTowerDefense.Scripts;
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

    public GameObject CannonTowerPrefab;
    public GameObject CrossbowTowerPrefab;
    public GameObject ThunderTowerPrefab;
    public GameObject CropFarmPrefab;
    public GameObject ChickenFarmPrefab;
    public GameObject MillPrefab;

    private readonly int[] m_PriceList = {40, 100, 1000, 50, 100, 300};

    private HashSet<Division> m_AvailableDivisions;
    private Dictionary<Division, GameObject> m_DivisionGameObjectsDictionary;
    private Dictionary<Division, GameObject> m_DivisionBuildingDictionary;

    private Division m_DivisionToPlaceOn;

    private int m_BuildingToConstructId = -1;
    private Division m_FocusedBuildingDivision;
    private Division m_SelectedBuildingDivision;

    void OnEnable()
    {
        TriggerBuildingsPanelButton.SetActive(true);
        m_AvailableDivisions = Master.AvailableDivisions;
        m_DivisionGameObjectsDictionary = Master.DivisionGameObjectDictionary;
        m_DivisionBuildingDictionary = new Dictionary<Division, GameObject>();
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
        m_DivisionToPlaceOn = null;

        var ray = new Ray(FirstPersonCamera.transform.position, FirstPersonCamera.transform.forward);
        var hits = Physics.RaycastAll(ray);

        if (!hits.Any()) return;

        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag("Division") || m_BuildingToConstructId < 0) continue;
            m_DivisionToPlaceOn =
                m_AvailableDivisions.SingleOrDefault(div => div.Includes(hit.collider.transform.position));
            if (m_DivisionToPlaceOn != null)
            {
                BuildButton.SetActive(true);
                break;
            }
            else
            {
                m_FocusedBuildingDivision = m_DivisionBuildingDictionary.Keys.FirstOrDefault(div =>
                    div.Includes(hit.collider.transform.position));
                if (m_FocusedBuildingDivision != null)
                {
                    SelectButton.SetActive(true);
                    break;
                }
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
        var newBuilding = Instantiate(buildingToConstruct, m_DivisionGameObjectsDictionary[m_DivisionToPlaceOn].transform.position, Quaternion.identity,
            m_DivisionGameObjectsDictionary[m_DivisionToPlaceOn].transform);
        m_AvailableDivisions.Remove(m_DivisionToPlaceOn);
        m_DivisionBuildingDictionary.Add(m_DivisionToPlaceOn, newBuilding);
        m_DivisionToPlaceOn = null;
    }

    public void Select()
    {
        m_SelectedBuildingDivision = m_FocusedBuildingDivision;
        DemolishButton.SetActive(true);
    }

    public void Demolish()
    {
        var building = m_DivisionBuildingDictionary[m_SelectedBuildingDivision];
        Destroy(building);
        m_DivisionBuildingDictionary.Remove(m_SelectedBuildingDivision);
        m_AvailableDivisions.Add(m_SelectedBuildingDivision);
        m_SelectedBuildingDivision = null;
    }

    public void UpdateBuildingsPanelStatus()
    {
        BuildingsPanel.SetActive(!BuildingsPanel.activeSelf);
        Debug.Log($"Set buildings panel status to: {BuildingsPanel.activeSelf}");
    }

}
