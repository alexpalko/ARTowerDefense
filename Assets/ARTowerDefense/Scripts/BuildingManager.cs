using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using Assets.ARTowerDefense.Scripts;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public Camera FirstPersonCamera;
    public GameObject TriggerBuildingsPanelButton;
    public GameObject BuildButton;
    public GameObject SelectButton;
    public GameObject DemolishButton;
    public GameObject BuildingsPanel;

    public GameObject CannonTowerPrefab;

    public int Coins { get; }
    private HashSet<Division> m_AvailableDivisions;
    private Dictionary<Division, GameObject> m_DivisionGameObjectsDictionary;
    private Dictionary<Division, GameObject> m_DivisionBuildingDictionary;

    private Division m_DivisionToPlaceOn;

    private GameObject m_BuildingToConstruct;
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
            if (!hit.collider.CompareTag("Division") || m_BuildingToConstruct == null) continue;
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
        switch (x)
        {
            case 0:
                break;
            case 1:
                m_BuildingToConstruct = CannonTowerPrefab;
                Debug.Log($"the Cannon Tower was selected");
                break;
            case 2:
                break;
            case 3:
                break;
            case 4: 
                break;
            case 5:
                break;
            case 6:
                break;
            case 7:
                break;
            case 8:
                break;
            default:
                Debug.LogError($"Invalid building code: {x}");
                break;
        }
    }

    public void Construct()
    {
        var newBuilding = Instantiate(m_BuildingToConstruct, m_DivisionToPlaceOn.Center, Quaternion.identity,
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
