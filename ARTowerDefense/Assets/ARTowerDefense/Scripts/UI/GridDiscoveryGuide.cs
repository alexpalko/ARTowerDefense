using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;
using UnityEngine.UI;

public class GridDiscoveryGuide : MonoBehaviour
{
    /// <summary>
    /// The time to delay, after ARCore loses tracking of any planes, showing the plane
    /// discovery guide.
    /// </summary>
    [Tooltip("The time to delay, after ARCore loses tracking of any planes, showing the plane " +
             "discovery guide.")]
    public float DisplayGuideDelay = 3.0f;

    /// <summary>
    /// The time to delay, after displaying the plane discovery guide, offering more detailed
    /// instructions on how to find a plane.
    /// </summary>
    [Tooltip("The time to delay, after displaying the plane discovery guide, offering more detailed " +
             "instructions on how to find a plane.")]
    public float OfferDetailedInstructionsDelay = 8.0f;

    /// <summary>
    /// The time to delay, after Unity Start, showing the plane discovery guide.
    /// </summary>
    private const float k_OnStartDelay = 1f;

    /// <summary>
    /// The time to delay, after a at least one plane is tracked by ARCore, hiding the plane discovery guide.
    /// </summary>
    private const float k_HideGuideDelay = 0.75f;

    [Tooltip("The snackbar Game Object")]
    [SerializeField]
    private GameObject m_SnackBar;

    [Tooltip("The snackbar text.")]
    [SerializeField]
    private Text m_SnackBarText;

    [Tooltip("The Game Object containing the button to go to the plane selection phase.")]
    [SerializeField]
    private GameObject m_NextButton;

    [Tooltip("The Game Object that provides the feature point cloud visualization.")]
    [SerializeField]
    private GameObject m_FeaturePointCloud;

    [Tooltip("The Game Object that guides the user in the grid selection phase.")]
    [SerializeField]
    private GameObject m_GridSelection;

    /// <summary>
    /// The elapsed time ARCore has been detecting at least one plane.
    /// </summary>
    private float m_DetectedPlaneElapsed;

    /// <summary>
    /// The elapsed time ARCore has been tracking but not detected any planes.
    /// </summary>
    private float m_NotDetectedPlaneElapsed;

    /// <summary>
    /// Indicates whether a lost tracking reason is displayed.
    /// </summary>
    private bool m_IsLostTrackingMessageDisplayed;

    /// <summary>
    /// A list that holds the planes detected by ARCore in the current frame.
    /// </summary>
    private List<DetectedPlane> m_DetectedPlanes = new List<DetectedPlane>();

    public void Start()
    {
        m_NextButton.GetComponent<Button>().onClick.AddListener(_OnNextButtonClicked);
        _CheckFieldsAreNotNull();
    }

    public void OnDestroy()
    {
        m_NextButton.GetComponent<Button>().onClick.RemoveListener(_OnNextButtonClicked);
    }

    public void Update()
    {
        _UpdateDetectedPlaneTrackingState();
        _UpdateUI();
    }

    private void _OnNextButtonClicked()
    {
        gameObject.SetActive(false);
        m_GridSelection.SetActive(true);
    }

    /// <summary>
    /// Checks whether at least one plane being actively tracked exists.
    /// </summary>
    private void _UpdateDetectedPlaneTrackingState()
    {
        if (Session.Status != SessionStatus.Tracking)
        {
            return;
        }

        Session.GetTrackables(m_DetectedPlanes);
        foreach (DetectedPlane plane in m_DetectedPlanes)
        {
            if (!m_NextButton.activeSelf)
            {
                //List<Vector3> polygons = new List<Vector3>();
                //plane.GetBoundaryPolygon(polygons);
                // TODO: check if a large enough plane is found
                m_NextButton.SetActive(true);
            }
            if (plane.TrackingState == TrackingState.Tracking)
            {
                m_DetectedPlaneElapsed += Time.deltaTime;
                m_NotDetectedPlaneElapsed = 0f;
                return;
            }
        }

        m_DetectedPlaneElapsed = 0f;
        m_NotDetectedPlaneElapsed += Time.deltaTime;
    }

    private void _UpdateUI()
    {
        if (Session.Status == SessionStatus.LostTracking &&
            Session.LostTrackingReason != LostTrackingReason.None)
        {
            m_FeaturePointCloud.SetActive(false);
            switch (Session.LostTrackingReason)
            {
                case LostTrackingReason.InsufficientLight:
                    m_SnackBarText.text = "It's dark here, try moving to a well-lit area.";
                    break;
                case LostTrackingReason.ExcessiveMotion:
                    m_SnackBarText.text = "Slow down! You're moving too fast.";
                    break;
                case LostTrackingReason.InsufficientFeatures:
                    m_SnackBarText.text = "Aim the device to a surface with more color and texture.";
                    break;
                default:
                    m_SnackBarText.text = "Motion tracking lost, reloading...";
                    break;
            }

            m_IsLostTrackingMessageDisplayed = true;
            return;
        }

        if (m_IsLostTrackingMessageDisplayed)
        {
            m_IsLostTrackingMessageDisplayed = false;
        }

        if (m_NotDetectedPlaneElapsed > DisplayGuideDelay)
        {
            m_FeaturePointCloud.SetActive(true);
            m_SnackBarText.text = "Move your device in a circular motion over a flat surface.";
        }
        else if (m_NotDetectedPlaneElapsed > 0f || m_DetectedPlaneElapsed > k_HideGuideDelay)
        {
            //m_FeaturePointCloud.SetActive(false);
        }
    }

    /// <summary>
    /// Checks the required fields are not null, and logs a Warning otherwise.
    /// </summary>
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

        if (m_SnackBarText == null)
        {
            Debug.LogError("SnackBarText is null");
        }

        if (m_SnackBar == null)
        {
            Debug.LogError("SnackBar is null");
        }

        if (m_FeaturePointCloud == null)
        {
            Debug.LogError("FeaturePointCloud is null.");
        }

        if (m_GridSelection == null)
        {
            Debug.LogError("GridSelection is null.");
        }
    }
}
