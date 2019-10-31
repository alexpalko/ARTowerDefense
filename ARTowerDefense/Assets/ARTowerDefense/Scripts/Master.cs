using System;
using System.Collections.Generic;
using System.Linq;
using GoogleARCore;
using GoogleARCoreInternal;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Input = GoogleARCore.InstantPreviewInput;
#endif

public class Master : MonoBehaviour
{
    [SerializeField] private GameObject ARCoreDevice;
    [SerializeField] private Camera FirstPersonCamera;
    [SerializeField] private GameObject GridGenerator;
    [SerializeField] private GameObject PointCloud;
    [SerializeField] private GameObject AdvanceStateButton;
    [SerializeField] private GameObject BaseMarkerManipulator;

    [SerializeField] private GameObject PlaneMarkerPrefab;
    [SerializeField] private GameObject WallPrefab;
    [SerializeField] private GameObject TowerPrefab;
    [SerializeField] private GameObject BaseMarkerPrefab;
    [SerializeField] private GameObject BasePrefab;

    private GameObject m_PlaneSelectionMarker;
    private DetectedPlane m_MarkedPlane;
    private Vector3[] m_BindingVectors;
    private GameObject[] m_BindingWalls;
    private GameObject[] m_BindingTowers;
    private GameObject m_BaseMarker;
    private GameObject m_HomeBase;


    private enum GameState
    {
        STARTED, GRID_DETECTION, GAME_SPACE_INSTANTIATION, PATH_GENERATION, GAME_LOOP, GAME_OVER, PAUSED
    }

    private GameState m_GameState = GameState.GRID_DETECTION;

    private bool m_IsQuitting;

    public void Awake()
    {
        Application.targetFrameRate = 60;
    }

    // Update is called once per frame
    void Update()
    {
        _UpdateApplicationLifecycle();

        switch (m_GameState)
        {
            case GameState.STARTED:
                break;
            case GameState.GRID_DETECTION:
                _GridDetectionLogic();
                break;
            case GameState.GAME_SPACE_INSTANTIATION:
                _GameSpaceInitializationLogic();
                break;
            case GameState.PATH_GENERATION:
                _PathGenerationLogic();
                break;
            case GameState.GAME_LOOP:
                break;
            case GameState.PAUSED:
                break;
            case GameState.GAME_OVER:
                break;
            default:
                throw new InvalidOperationException($"The session is in an undefined state. Current state: {m_GameState}");
        }
    }

    public void AdvanceGameState()
    {
        switch (m_GameState)
        {
            case GameState.STARTED:
                m_GameState = GameState.GRID_DETECTION;
                break;
            case GameState.GRID_DETECTION:
                m_GameState = GameState.GAME_SPACE_INSTANTIATION;
                _InitializeGameSpaceInstantiation();
                break;
            case GameState.GAME_SPACE_INSTANTIATION:
                m_GameState = GameState.PATH_GENERATION;
                break;
            case GameState.PATH_GENERATION:
                m_GameState = GameState.GAME_LOOP;
                _InitializeGameLoop();
                break;
            case GameState.GAME_LOOP:
                m_GameState = GameState.GAME_OVER;
                _InitializeGameOver();
                break;
            case GameState.GAME_OVER:
                m_GameState = GameState.GRID_DETECTION;
                _InitializeGridDetection();
                break;
            case GameState.PAUSED:
                throw new InvalidOperationException("Method should not be accessed when the session is in PAUSED state.");
            default:
                throw new InvalidOperationException("The session is in an undefined state. Current state: {m_GameStage}");
        }
        Debug.Log($"Game stage changed to {m_GameState}");
    }

    public void ToStartState()
    {
        m_GameState = GameState.STARTED;
    }

    public void ToGameSpaceInstantiationState()
    {
        m_GameState = GameState.GAME_SPACE_INSTANTIATION;
    }

    public void ToPathGenerationState()
    {
        m_GameState = GameState.PATH_GENERATION;
    }

    public void ToGameLoopState()
    {
        m_GameState = GameState.PATH_GENERATION;
    }

    public void ToPausedState()
    {
        m_GameState = GameState.PAUSED;
    }

    private void _InitializeGameSpaceInstantiation()
    {
        Debug.Log($"Initializing {GameState.GAME_SPACE_INSTANTIATION} state");
        Destroy(m_PlaneSelectionMarker);
        List<Vector3> boundaryPolygons = new List<Vector3>();
        m_MarkedPlane.GetBoundaryPolygon(boundaryPolygons);
        m_BindingVectors = boundaryPolygons.ToArray();

        foreach (Vector3 boundaryPolygon in boundaryPolygons)
        {
            Console.WriteLine("( " + boundaryPolygon.x + ", " + boundaryPolygon.y + ", " + boundaryPolygon.z + " )");
        }

        //ARCoreDevice.GetComponent<ARCoreSessionConfig>().PlaneFindingMode = DetectedPlaneFindingMode.Vertical;
        GridGenerator.SetActive(false);
        PointCloud.SetActive(false);
        Debug.Log("Grid generation disabled");
        AdvanceStateButton.SetActive(false);
        Debug.Log("ConfirmButton disabled");
    }

    private void _InitializeGameLoop()
    {
        throw new NotImplementedException();
    }

    private void _InitializeGameOver()
    {
        throw new NotImplementedException();
    }

    private void _InitializeGridDetection()
    {
        throw new NotImplementedException();
    }

    private void _GridDetectionLogic()
    {
        Touch touch;

        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return;
        }

        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                                          TrackableHitFlags.FeaturePointWithSurfaceNormal;

        if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
        {
            if (IsPointerOverUIObject()) return;

            Debug.Log("Plane intersection found");

            if ((hit.Trackable is DetectedPlane) &&
                Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                    hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.LogError("Hit at back of current DetectedPlane");
            }
            else
            {
                if (hit.Trackable is DetectedPlane plane)
                {
                    Debug.Log("The raycast hit a horizontal plane");
                    if (plane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
                    {
                        if (m_PlaneSelectionMarker != null)
                        {
                            Debug.Log("Old marker was removed");
                            Destroy(m_PlaneSelectionMarker);
                        }
                        m_PlaneSelectionMarker = Instantiate(PlaneMarkerPrefab, hit.Pose.position, hit.Pose.rotation);
                        Anchor anchor = hit.Trackable.CreateAnchor(hit.Pose);
                        gameObject.transform.parent = anchor.transform;
                        m_MarkedPlane = plane;
                        Debug.Log("New base marker placed");
                        AdvanceStateButton.SetActive(true);
                        Debug.Log("ConfirmButton activated");
                    }
                }
            }
        }
    }

    private void _GameSpaceInitializationLogic()
    {
        if (m_BindingWalls == null)
        {
            _SpawnBoundaries();
        }


        _PlaceBaseMarker();
        //BaseMarkerManipulator.SetActive(true);


        //if (m_HomeBase == null)
        //{
        //    _SpawnHomeBase();
        //}
    }

    private void _SpawnBoundaries()
    {
        m_BindingTowers = m_BindingVectors.Select(v => Instantiate(TowerPrefab, v, Quaternion.identity)).ToArray();

        m_BindingWalls = new GameObject[m_BindingVectors.Length];
        for (int i = 0; i < m_BindingVectors.Length; i++)
        {
            m_BindingWalls[i] = Instantiate(WallPrefab,
                Vector3.Lerp(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length], 0.5f),
                Quaternion.identity);
            m_BindingWalls[i].transform.localScale += new Vector3(
                Vector3.Distance(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length]), 0, 0);
        }

        for (int i = 0; i < m_BindingVectors.Length; i++)
        {
            Vector3 point = m_BindingVectors[i];
            Vector3 midPoint = m_BindingWalls[i].transform.position;

            Vector3 pointProjectionOntoX = Vector3.Project(point, Vector3.right);
            Vector3 midPointProjectionOntoX = Vector3.Project(midPoint, Vector3.right);
            Vector3 pointProjectionOntoZ = Vector3.Project(point, Vector3.forward);
            Vector3 midPointProjectionOntoZ = Vector3.Project(midPoint, Vector3.forward);

            float cath1 = Vector3.Distance(pointProjectionOntoX, midPointProjectionOntoX);
            float cath2 = Vector3.Distance(pointProjectionOntoZ, midPointProjectionOntoZ);
            float angle = Mathf.Atan2(cath2, cath1) * Mathf.Rad2Deg;

            m_BindingWalls[i].transform.RotateAround(m_BindingWalls[i].transform.position, Vector3.up, angle);
            Collider fieldCollider = m_BindingWalls[i].GetComponent<Collider>();
            if (ColliderContainsPoint(fieldCollider.transform, point)) continue;
            m_BindingWalls[i].transform.RotateAround(m_BindingWalls[i].transform.position, Vector3.up, -angle * 2);
        }
    }

    private void _PlaceBaseMarker()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                                              TrackableHitFlags.FeaturePointWithSurfaceNormal;
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.LogError("Hit at back of current DetectedPlane");
                }
                else
                {
                    if (hit.Trackable is DetectedPlane plane)
                    {
                        Debug.Log("The raycast hit a horizontal plane");
                        if (plane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
                        {
                            if (m_BaseMarker != null)
                            {
                                Destroy(m_BaseMarker);
                            }

                            m_BaseMarker = Instantiate(BaseMarkerPrefab, hit.Pose.position, hit.Pose.rotation);

                            //Anchor anchor = hit.Trackable.CreateAnchor(hit.Pose);
                            //gameObject.transform.parent = anchor.transform;
                            //m_MarkedPlane = plane;
                            //Debug.Log("New base marker placed");
                            //AdvanceStateButton.SetActive(true);
                            //Debug.Log("ConfirmButton activated");
                        }
                    }
                }
            }

            //Vector3 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
            //touchPosition.y = 0;
            //m_BaseMarker = Instantiate(BaseMarkerPrefab, touchPosition, Quaternion.identity);
        }
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private void _SpawnHomeBase()
    {
        Vector3 basePosition = m_PlaneSelectionMarker.transform.position;
        m_HomeBase = Instantiate(BaseMarkerPrefab, basePosition, Quaternion.identity);
    }

    private void _SpawnPath() { }

    private void _PathGenerationLogic()
    {
        _SpawnBoundaries();
        //_SpawnHomeBase();
    }

    private bool ColliderContainsPoint(Transform colliderTransform, Vector3 point)
    {
        Vector3 localPos = colliderTransform.InverseTransformPoint(point);
        return Mathf.Abs(localPos.x) < 0.5f && Mathf.Abs(localPos.y) < 0.5f && Mathf.Abs(localPos.z) < 0.5f;
    }

    private void _GameLoopLogic() { }

    private void _GameOverLogic() { }

    private void _UpdateApplicationLifecycle()
    {
        // Exit the app when the 'back' button is pressed.
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        // Only allow the screen to sleep when not tracking.
        if (Session.Status != SessionStatus.Tracking)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        if (m_IsQuitting)
        {
            return;
        }

        // Quit if ARCore was unable to connect and give Unity some time for the toast to
        // appear.
        if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
        {
            _ShowAndroidToastMessage("Camera permission is needed to run this application.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
        else if (Session.Status.IsError())
        {
            _ShowAndroidToastMessage(
                "ARCore encountered a problem connecting.  Please start the app again.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }

    }

    private void _DoQuit()
    {
        Application.Quit();
    }

    private void _ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject =
                    toastClass.CallStatic<AndroidJavaObject>(
                        "makeText", unityActivity, message, 0);
                toastObject.Call("show");
            }));
        }
    }

}
