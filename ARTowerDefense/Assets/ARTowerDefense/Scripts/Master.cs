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
    [SerializeField] private GameObject GamePlanePrefab;

    private GameObject m_PlaneSelectionMarker;
    private DetectedPlane m_MarkedPlane;
    private Pose m_MarkedPlaneCenterPose;
    private Vector3[] m_BindingVectors;
    private GameObject[] m_BindingWalls;
    private GameObject[] m_BindingTowers;
    private GameObject m_GamePlane;
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
                _InitializePathGeneration();
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
        m_MarkedPlaneCenterPose = m_MarkedPlane.CenterPose;

        foreach (Vector3 boundaryPolygon in boundaryPolygons)
        {
            Console.WriteLine("( " + boundaryPolygon.x + ", " + boundaryPolygon.y + ", " + boundaryPolygon.z + " )");
        }

        GridGenerator.SetActive(false);
        PointCloud.SetActive(false);
        Debug.Log("Grid generation disabled");
        AdvanceStateButton.SetActive(false);
        Debug.Log("ConfirmButton disabled");
    }

    private void _InitializePathGeneration()
    {
        _SpawnHomeBase();
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

        if (m_GamePlane == null)
        {
            _SpawnGamePlane();
        }

        _PlaceBaseMarker();
    }

    // TODO: place anchors
    private void _SpawnBoundaries()
    {
        Transform anchorTransform = m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform;

        m_BindingTowers = m_BindingVectors.Select(v => Instantiate(TowerPrefab, v, Quaternion.identity)).ToArray();
        foreach (GameObject bindingTower in m_BindingTowers)
        {
            bindingTower.transform.parent = anchorTransform;
        }

        m_BindingWalls = new GameObject[m_BindingVectors.Length];
        for (int i = 0; i < m_BindingVectors.Length; i++)
        {
            m_BindingWalls[i] = Instantiate(WallPrefab,
                Vector3.Lerp(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length], 0.5f),
                Quaternion.identity);
            m_BindingWalls[i].transform.localScale += new Vector3(
                Vector3.Distance(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length]), 0, 0);
            m_BindingWalls[i].transform.parent = anchorTransform;
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

    private void _PlaceGameObject(GameObject prefab)
    {
        Touch touch;
        touch = Input.GetTouch(0);
        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
                                          TrackableHitFlags.FeaturePointWithSurfaceNormal;
        
        if (touch.phase == TouchPhase.Began)
        {
            Debug.Log("Touch began");
            if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.Log("Hit at back of current detected plane");
                }
                else if (_IsWithinBoundaries(hit.Pose.position))
                {
                    GameObject newGameObject = Instantiate(prefab, hit.Pose.position, hit.Pose.rotation);
                    var anchor = hit.Trackable.CreateAnchor(hit.Pose);
                    newGameObject.transform.parent = anchor.transform;
                }
                else
                {
                    Debug.Log("Hit outside the game boundaries");
                }
            }
        }
    }

    private void _SpawnGamePlane()
    {
        m_GamePlane = Instantiate(GamePlanePrefab, m_MarkedPlaneCenterPose.position, Quaternion.identity,
            m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform);

        float maxX = m_BindingVectors.Select(v => v.x).Max();
        float minX = m_BindingVectors.Select(v => v.x).Min();
        float maxZ = m_BindingVectors.Select(v => v.z).Max();
        float minZ = m_BindingVectors.Select(v => v.z).Min();

        float distanceX = Math.Abs(maxX) + Math.Abs(minX);
        float distanceZ = Math.Abs(maxZ) + Math.Abs(minZ);

        float maxDistance = distanceX > distanceZ ? distanceX : distanceZ;

        m_GamePlane.transform.localScale = new Vector3(maxDistance, maxDistance, 1);
        m_GamePlane.transform.Rotate(90, 0 ,0);

        //Mesh mesh = new Mesh();
        //Anchor anchor = m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose);
        //m_GamePlane = Instantiate(GamePlanePrefab, Vector3.zero, Quaternion.identity, anchor.transform);
        //m_GamePlane.transform.position = m_BindingVectors[0];
        //m_GamePlane.GetComponent<MeshFilter>().sharedMesh = mesh;
        //m_GamePlane.GetComponent<MeshCollider>().sharedMesh = mesh;

        //mesh.vertices = m_BindingVectors;
        //List<int> indexesOrder = new List<int>();
        //int pointsCount = m_BindingVectors.Length;

        //for (int inc = 2; ; inc *= 2)
        //{
        //    if (pointsCount <= inc) break;

        //    for (int i = inc / 2; i < pointsCount; i += inc)
        //    {
        //        indexesOrder.Add(i - inc / 2);
        //        indexesOrder.Add(i);
        //        indexesOrder.Add((i + inc / 2) % pointsCount);
        //    }
        //}

        //mesh.triangles = indexesOrder.ToArray();
    }

    private void _PlaceBaseMarker()
    {
        if (Input.touchCount > 0)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended) return;
            Ray ray = FirstPersonCamera.ScreenPointToRay(touch.position);
            var hits = Physics.RaycastAll(ray);

            if (hits.Length == 0)
            {
                Debug.Log("Hit no object on touch");
            }
            else if (hits.Length != 1)
            {
                Debug.Log("Hit multiple objects on touch");
            }
            else
            {
                Vector3 spawnPoint = ray.GetPoint(hits[0].distance);

                if (hits[0].collider.tag != "GamePlane" || !_IsWithinBoundaries(spawnPoint)) return;
                Debug.Log("Hit the GamePlane on touch");

                if (m_BaseMarker != null)
                {
                    Destroy(m_BaseMarker);
                }

                m_BaseMarker = Instantiate(BaseMarkerPrefab, spawnPoint, Quaternion.identity,
                    m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform);

                AdvanceStateButton.SetActive(true);
            }

            //if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            //{
            //    if ((hit.Trackable is DetectedPlane) &&
            //        Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
            //            hit.Pose.rotation * Vector3.up) < 0)
            //    {
            //        Debug.LogError("Hit at back of current DetectedPlane");
            //    }
            //    else
            //    {
            //        if (hit.Trackable is DetectedPlane plane && _IsWithinPlane(hit.Pose.position))
            //        {
            //            Debug.Log("The raycast hit a horizontal plane");
            //            if (plane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
            //            {
            //                if (m_BaseMarker != null)
            //                {
            //                    Destroy(m_BaseMarker);
            //                }

            //                m_BaseMarker = Instantiate(BaseMarkerPrefab, hit.Pose.position, hit.Pose.rotation);
            //            }
            //        }
            //    }
            //}
        }
    }

    private bool _IsWithinBoundaries(Vector3 position)
    {
        position.y = m_BindingWalls[0].transform.position.y;
        RaycastHit[] hits = Physics.RaycastAll(position, Vector3.right, Mathf.Infinity);
        return hits.Count(hit => hit.collider.tag.Equals("GameWall")) == 1;
    }

    private void _SpawnHomeBase()
    {
        Vector3 basePosition = m_BaseMarker.transform.position;
        m_HomeBase = Instantiate(BasePrefab, basePosition, Quaternion.identity,
            m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform);
        Destroy(m_BaseMarker);
    }

    private void _SpawnPath() { }

    private void _PathGenerationLogic()
    {
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
