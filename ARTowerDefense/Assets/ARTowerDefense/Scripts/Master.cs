using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ARTowerDefense.Scripts;
using GoogleARCore;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = System.Random;
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
    [SerializeField] private GameObject DivisionMarkerPrefab;
    [SerializeField] private GameObject PlaneCornerMarkerPrefab;
    [SerializeField] private GameObject SpawnerPrefab;
    [SerializeField] private GameObject PathPrefab;

    private GameObject m_PlaneSelectionMarker;
    private DetectedPlane m_MarkedPlane;
    private Pose m_MarkedPlaneCenterPose;
    private Vector3[] m_BindingVectors;
    private GameObject[] m_BindingWalls;
    private GameObject[] m_BindingTowers;
    private GameObject m_GamePlane;
    private GameObject m_BaseMarker;
    private GameObject m_HomeBase;

    private const float m_DivisionLength = .1f;

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
                AdvanceGameState();
                break;
            case GameState.PATH_GENERATION:
                if (!m_PlaneSplit)
                {
                    _SplitPlane();
                }
                else 
                {
                    _PlaceBase();
                    if (m_BasePlaced)
                    {
                        AdvanceStateButton.SetActive(true);
                    }
                }
                break;
            case GameState.GAME_LOOP:
                if (m_Moves == null)
                {
                    _InitializeMoves();
                }
                if (_GeneratePath() != null)
                {
                    _BuildPath();
                    m_GameState = GameState.PAUSED;
                    Debug.Log("Application went into paused state.");
                }
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
                //_InitializeGameLoop();
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
        AdvanceStateButton.SetActive(false);
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
                        m_AnchorTransform = m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform;
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
        List<Vector3> bindingVectorsList = m_BindingVectors.ToList();
        _ConsolidateBoundaries(bindingVectorsList);
        m_BindingVectors = bindingVectorsList.ToArray();

        if (m_BindingWalls == null)
        {
            _SpawnBoundaries();
        }

        if (m_GamePlane == null)
        {
            _SpawnGamePlane();
        }

        //_PlaceBaseMarker();
    }

    private void _ConsolidateBoundaries(List<Vector3> vectors)
    {
        for (int i = 0; i < vectors.Count; i++)
        {
            var closeByVectorsIndexes = new List<int>();
            for (int j = i + 1; j < vectors.Count; j++)
            {
                if (Vector3.Distance(vectors[i], vectors[j]) < .5f)
                {
                    closeByVectorsIndexes.Add(j);
                }
            }

            if (closeByVectorsIndexes.Count != 0)
            {
                var newVector = vectors[i];
                foreach (int index in closeByVectorsIndexes)
                {
                    newVector += vectors[index];
                }

                newVector /= closeByVectorsIndexes.Count + 1;

                vectors.Remove(vectors[i]);
                //m_BindingVectors[i] = newVector;
                vectors.Insert(i, newVector);
                vectors.RemoveAll(vect =>
                    closeByVectorsIndexes.Select(idx => vectors[idx]).Contains(vect));
                _ConsolidateBoundaries(vectors);
                return;
            }
        }
    }

    private void _SpawnBoundaries()
    {

        m_BindingTowers = m_BindingVectors.Select(v => Instantiate(TowerPrefab, v, Quaternion.identity)).ToArray();
        foreach (GameObject bindingTower in m_BindingTowers)
        {
            bindingTower.transform.parent = m_AnchorTransform;
        }

        m_BindingWalls = new GameObject[m_BindingVectors.Length];
        for (int i = 0; i < m_BindingVectors.Length; i++)
        {
            m_BindingWalls[i] = Instantiate(WallPrefab,
                Vector3.Lerp(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length], 0.5f),
                Quaternion.identity);
            m_BindingWalls[i].transform.localScale += new Vector3(
                Vector3.Distance(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length]), 0, 0);
            m_BindingWalls[i].transform.parent = m_AnchorTransform;
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
                    //var anchor = hit.Trackable.CreateAnchor(hit.Pose);
                    newGameObject.transform.parent = m_AnchorTransform;
                }
                else
                {
                    Debug.Log("Hit outside the game boundaries");
                }
            }
        }
    }

    private bool _IsWithinBoundaries(Vector3 position)
    {
        position.y = m_BindingWalls[0].transform.position.y;
        RaycastHit[] hits = Physics.RaycastAll(position, Vector3.right, Mathf.Infinity);
        return hits.Count(hit => hit.collider.tag.Equals("GameWall")) == 1;
    }

    private void _SpawnGamePlane()
    {

        float maxX = m_BindingVectors.Select(v => v.x).Max();
        float minX = m_BindingVectors.Select(v => v.x).Min();
        float maxZ = m_BindingVectors.Select(v => v.z).Max();
        float minZ = m_BindingVectors.Select(v => v.z).Min();

        float distanceX = maxX - minX;
        float distanceZ = maxZ - minZ;
        float maxDistance = distanceX > distanceZ ? distanceX : distanceZ;

        float middleX = (maxX + minX) / 2;
        float middleZ = (maxZ + minZ) / 2;

        m_GamePlane = Instantiate(GamePlanePrefab, new Vector3(middleX, m_BindingVectors[0].y, middleZ), Quaternion.identity,
            /*m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform*/m_AnchorTransform);
        m_GamePlane.transform.localScale = new Vector3(maxDistance, maxDistance, 1);
        m_GamePlane.transform.Rotate(90, 0 ,0);
        Debug.Log(m_GamePlane.transform.localScale);
    }

    // ########################################################
    // ################### PATH GENERATION  ###################
    // ########################################################

    private List<Division> m_Divisions;
    private Division m_PathEnd;
    private Division m_PathStart;
    private List<Division> m_AvailableDivisions;
    private List<Division> m_PathNodeDivisions;
    private bool m_PlaneSplit;
    private bool m_BasePlaced;
    private HashSet<Division> availableNodeDivisions;
    private HashSet<Division> availableDivisions;
    private Stack<Division> path;
    private float m_AvailableDivisionsThreshold = .3f;
    private float m_IncreaseThresholdCount = 1;
    private Vector3[] m_Moves;
    private Transform m_AnchorTransform;

    private void _SplitPlane()
    {
        Renderer rend = m_GamePlane.GetComponent<Renderer>();
        Debug.Log("Acquired game plane renderer");

        float y = rend.bounds.min.y;

        Vector3[] planeCorners = {
            rend.bounds.max,
            new Vector3(rend.bounds.max.x, y, rend.bounds.min.z),
            rend.bounds.min,
            new Vector3(rend.bounds.min.x, y, rend.bounds.max.z),
        };

        foreach (Vector3 planeCorner in planeCorners)
        {
            Instantiate(PlaneCornerMarkerPrefab, planeCorner, Quaternion.identity,
                /*m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform*/m_AnchorTransform);
        }

        Debug.Log("Instantiated plane corners");

        List<Division> divisions = new List<Division>();

        for (float x = rend.bounds.min.x; x < rend.bounds.max.x; x += m_DivisionLength)
        {
            for (float z = rend.bounds.min.z; z < rend.bounds.max.z; z += m_DivisionLength)
            {
                divisions.Add(new Division(new Vector3(x, y, z), new Vector3(x + m_DivisionLength, y, z + m_DivisionLength)));
            }
        }

        _TrimDivisions(divisions);
        
        m_Divisions = divisions;
        Debug.Log($"Will spawn {m_Divisions.Count} divisions.");
        foreach (Division division in divisions)
        {
            Instantiate(DivisionMarkerPrefab, division.Center, Quaternion.identity,
               /* m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform*/m_AnchorTransform);
            Debug.Log("Spawned division marker.");
        }

        availableDivisions = new HashSet<Division>(divisions);
        m_PlaneSplit = true;
    }

    private void _TrimDivisions(List<Division> divisions)
    {
        Debug.Log("Started trim division.");
        Debug.Log($"Original divisions count: {divisions.Count}");


        divisions.RemoveAll(div =>
        {

            var raycastHits = Physics.RaycastAll(div.Point1, Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
                //Debug.Log("Division removed on first check");
                return true;
            }

            raycastHits = Physics.RaycastAll(div.Point2, Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
               // Debug.Log("Division removed on second check");
                return true;
            }

            raycastHits = Physics.RaycastAll(new Vector3(div.Point1.x, div.Point1.y, div.Point2.z), Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
               // Debug.Log("Division removed on fourth check");
                return true;
            }

            raycastHits = Physics.RaycastAll(new Vector3(div.Point2.x, div.Point1.y, div.Point1.z), Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
                return true;
            }


            return false;
        });

        Debug.Log($"Division count after trimming: {divisions.Count}");
    }

    private bool _IsWithinBoundaries(RaycastHit[] hits)
    {
        if (hits.Length == 1 && hits[0].collider.tag.Equals("GameWall")) return true;
        if (hits.Length > 3) return false;

        IEnumerable<Collider> colliders = hits.Select(hit => hit.collider);
        List<Collider> towerColliders = colliders.Where(col => col.tag.Equals("GameTower")).ToList();
        List<Collider> wallColliders = colliders.Where(col => col.tag.Equals("GameWall")).ToList();

        if (towerColliders.Count() != 1) return false;

        foreach (Collider wallCollider in wallColliders)
        {
            if (!towerColliders[0].bounds.Intersects(wallCollider.bounds))
            {
                Debug.Log("Tower not intersecting wall.");
                return false;
            }
        }

        return true;
    }

    private void _PlaceBase()
    {
        if (Input.touchCount <= 0) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;
        Debug.Log("Touch detected");
        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Ended) return;
        Ray ray = FirstPersonCamera.ScreenPointToRay(touch.position);
        RaycastHit[] hits = Physics.RaycastAll(ray);


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
            Vector3 hitPoint = ray.GetPoint(hits[0].distance);
            Division baseDivision = m_Divisions.SingleOrDefault(div => div.Includes(hitPoint));

            if (baseDivision == null)
            {
                Debug.Log("Hit could not be placed on a valid division.");
                return;
            }

            if (m_HomeBase != null)
            {
                Destroy(m_HomeBase);
            }

            m_HomeBase = Instantiate(BasePrefab, baseDivision.Center, Quaternion.identity,
                /*m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform*/m_AnchorTransform);

            Vector3 front = baseDivision.Center - m_HomeBase.transform.forward * m_DivisionLength;
            m_PathEnd = m_Divisions.SingleOrDefault(div => div.Includes(front));
            if (m_PathEnd == null)
            {
                front = baseDivision.Center - m_HomeBase.transform.forward * -1;
                m_PathEnd = m_Divisions.SingleOrDefault(div => div.Includes(front));
                if (m_PathEnd == null) return;
            }

            //if (hits[0].collider.tag != "GamePlane" || !_IsWithinBoundaries(hitPoint)) return;
            //Debug.Log("Hit the GamePlane on touch");

            //TODO: allow user to place marker again

            //if (m_BaseMarker != null)
            //{
            //    Destroy(m_BaseMarker);
            //}

            //m_BaseMarker = Instantiate(BaseMarkerPrefab, hitPoint, Quaternion.identity,
            //    m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform);

            m_AvailableDivisions = m_Divisions.Where(div => !_CheckProximity(div, baseDivision)).ToList();
            m_PathNodeDivisions = new List<Division> { baseDivision };

            m_BasePlaced = true;

            //AdvanceStateButton.SetActive(true);
        }
    }

    private static bool _CheckProximity(Division division1, Division division2)
    {
        List<Vector3> division1Points = new List<Vector3>
        {
            division1.Point1, division1.Point2,
            new Vector3(division1.Point1.x, division1.Point2.y, division1.Point2.z),
            new Vector3(division1.Point2.x, division1.Point1.y, division1.Point1.z),
        };

        List<Vector3> division2Points = new List<Vector3>
        {
            division2.Point1, division2.Point2,
            new Vector3(division2.Point1.x, division2.Point1.y, division2.Point2.z),
            new Vector3(division2.Point2.x, division2.Point1.y, division2.Point1.z),
        };

        return division2Points.Any(div => division1Points.Contains(div));
    }

    private void _SpawnHomeBase()
    {
        Vector3 basePosition = m_BaseMarker.transform.position;
        m_HomeBase = Instantiate(BasePrefab, basePosition, Quaternion.identity,
            /*m_MarkedPlane.CreateAnchor(m_MarkedPlaneCenterPose).transform*/m_AnchorTransform);
        Destroy(m_BaseMarker);
    }

    private Division _GeneratePath()
    {
        Debug.Log("Started generating path.");
        availableNodeDivisions = new HashSet<Division>(m_PathNodeDivisions);
        availableDivisions = new HashSet<Division>(m_AvailableDivisions);
        path = new Stack<Division>();
        //path.Push(m_PathStart);
        return _GenerateRandomPath(m_PathEnd);
    }

    private void _InitializeMoves()
    {
        m_Moves = new[]
        {
            new Vector3(m_DivisionLength, 0, 0),
            new Vector3(-m_DivisionLength, 0, 0),
            new Vector3(0, 0, m_DivisionLength),
            new Vector3(0, 0, -m_DivisionLength)
        };
    }

    private Division _GenerateRandomPath(Division currentDivision)
    {
        if (Math.Abs(m_IncreaseThresholdCount++ % 2000) < 1e-5)
        {
            m_AvailableDivisionsThreshold += .1f;
        }

        if (availableDivisions.Count < m_AvailableDivisionsThreshold * m_Divisions.Count && _CanPlaceSpawner(currentDivision))
        {
            path.Push(currentDivision);
            return currentDivision;
        }

        Debug.Log("Started generating random path.");

        Division previousDivision = null;

        // Remove neighbors of previous path
        if (path.Any())
        {
            previousDivision = path.Peek();
        }

        List<Division> markedDivisions = new List<Division>();

        if (previousDivision != null)
        {
            Debug.Log("Previous division found.");
            foreach (Vector3 move in m_Moves)
            {
                var neighborCenter = move + previousDivision.Center;
                var neighborDivision = availableDivisions.FirstOrDefault(div => div.Includes(neighborCenter));
                if (neighborDivision != null)
                {
                    markedDivisions.Add(neighborDivision);
                }
            }
        }
        availableDivisions.RemoveWhere(div => markedDivisions.Contains(div));

        IEnumerable<Division> possibleNextDivisions = _RandomizeNextDivisions(currentDivision);
        path.Push(currentDivision);
        Debug.Log($"The path contains {path.Count} divisions.");
        foreach (Division nextDivision in possibleNextDivisions)
        {
            availableDivisions.Remove(nextDivision);
            var res = _GenerateRandomPath(nextDivision);
            if (res != null) return res;
            availableDivisions.Add(nextDivision);
        }

        foreach (Division markedDivision in markedDivisions)
        {
            availableDivisions.Add(markedDivision);
        }

        path.Pop();
        return null;
    }

    private IEnumerable<Division> _RandomizeNextDivisions(Division currentDivision)
    {
        List<Vector3> centers = m_Moves.Select(mov => currentDivision.Center + mov).ToList();
        List<Division> divisions = new List<Division>();
        foreach (Vector3 center in centers)
        {
            var nextDivision = availableDivisions.FirstOrDefault(div => div.Includes(center));
            if (nextDivision != null)
            {
                divisions.Add(nextDivision);
            }
        }
        Random random = new Random();
        return divisions.OrderBy(_ => random.Next());
    }

    private bool _CanPlaceSpawner(Division currentDivision)
    {
        Division previousDivision = path.Peek();
        var direction = currentDivision.Center - previousDivision.Center;
        Division spawnerPosition = availableDivisions.FirstOrDefault(div => div.Includes(currentDivision.Center + direction));
        if (spawnerPosition != null)
        {
            Instantiate(SpawnerPrefab, spawnerPosition.Center, Quaternion.identity);
            return true;
        }

        return false;
    }

    private bool ColliderContainsPoint(Transform colliderTransform, Vector3 point)
    {
        Vector3 localPos = colliderTransform.InverseTransformPoint(point);
        return Mathf.Abs(localPos.x) < 0.5f && Mathf.Abs(localPos.y) < 0.5f && Mathf.Abs(localPos.z) < 0.5f;
    }
    
    private void _BuildPath()
    {
        foreach (Division pathDivision in path)
        {
            Instantiate(PathPrefab, pathDivision.Center, Quaternion.identity);
        }

        GameObject[] visualizers = GameObject.FindGameObjectsWithTag("DivisionVisualizer");

        foreach (GameObject visualizer in visualizers)
        {
            Destroy(visualizer);
        }
    }

    // ########################################################
    // ###################### GAME LOOP #######################
    // ########################################################

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
