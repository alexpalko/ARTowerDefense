using System.Collections;
using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.UI;

public class GridSelectionGuide : MonoBehaviour
{
    [Tooltip("The snackbar Game Object")]
    [SerializeField]
    private GameObject m_SnackBar;

    [Tooltip("The snackbar text.")]
    [SerializeField]
    private Text m_SnackBarText;

    [Tooltip("The Game Object containing the button to go to the plane selection phase.")]
    [SerializeField]
    private GameObject m_NextButton;

    [Tooltip("The Game Object containing the button to go back to the grid selection phase.")]
    [SerializeField]
    private GameObject m_BackButton;

    [Tooltip("The Game Object that guides the user in the grid discovery phase.")]
    [SerializeField]
    private GameObject m_GridDiscovery;

    [Tooltip("The Game Object that is responsible for generating new grids.")]
    [SerializeField]
    private GameObject m_GridGenerator;

    /// <summary>
    /// A list that holds the planes detected by ARCore in the current frame.
    /// </summary>
    private List<DetectedPlane> m_DetectedPlanes = new List<DetectedPlane>();

    public void OnEnable()
    {
        m_GridGenerator.SetActive(false);
    }

    public void Start()
    {
        m_NextButton.GetComponent<Button>().onClick.AddListener(_OnNextButtonClicked);
        m_BackButton.GetComponent<Button>().onClick.AddListener(_OnBackButtonClicked);

        _CheckFieldsAreNotNull();
    }

    public void OnDestroy()
    {
        m_NextButton.GetComponent<Button>().onClick.RemoveListener(_OnNextButtonClicked);
        m_BackButton.GetComponent<Button>().onClick.RemoveListener(_OnBackButtonClicked);
    }


    public void Update()
    {
        //_UpdateSelectedPlaneState();
        //_UpdateUI();
    }

    private void _UpdateSelectedPlaneState()
    {
        throw new System.NotImplementedException();
    }

    private void _UpdateUI()
    {
        throw new System.NotImplementedException();
    }

    private void _OnNextButtonClicked()
    {
        throw new System.NotImplementedException();
    }

    private void _OnBackButtonClicked()
    {
        gameObject.SetActive(false);
        m_GridGenerator.SetActive(true);
        m_GridDiscovery.SetActive(true);
    }

    private void _CheckFieldsAreNotNull()
    {
        if (m_NextButton == null)
        {
            Debug.LogError("NextButton is null");
        }
        else if (m_NextButton.GetComponent<Button>() == null)
        {
            Debug.LogError("NextButton does not have a Button component.");
        }

        if (m_BackButton == null)
        {
            Debug.LogError("BackButton is null.");
        }
        else if (m_BackButton.GetComponent<Button>() == null)
        {
            Debug.LogError("BackButton does not have a Button component.");
        }

        if (m_SnackBarText == null)
        {
            Debug.LogError("SnackBarText is null");
        }

        if (m_SnackBar == null)
        {
            Debug.LogError("SnackBar is null");
        }

        if (m_GridDiscovery == null)
        {
            Debug.LogError("GridDiscovery is null.");
        }

        if (m_GridGenerator == null)
        {
            Debug.LogError("GridGenerator is null");
        }
    }
}
