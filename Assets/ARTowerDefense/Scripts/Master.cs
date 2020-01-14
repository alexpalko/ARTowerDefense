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

namespace ARTowerDefense
{
    public class Master : MonoBehaviour
    {
        [SerializeField] private Camera FirstPersonCamera;
        [SerializeField] private GameObject GridGenerator;
        [SerializeField] private GameObject PointCloud;
        [SerializeField] private GameObject ToBasePlacementButton;
        [SerializeField] private GameObject ToGameLoopButton;
        [SerializeField] private GameObject PlacePrefabButton;
        [SerializeField] private GameObject Crosshair;
        [SerializeField] private GameObject CoinManager;
        [SerializeField] private GameObject BuildingManager;
        [SerializeField] private GameObject GridDetectionPanel;
        [SerializeField] private GameObject GameInitializationPanel;
        [SerializeField] private GameObject GameLoopPanel;
        [SerializeField] private GameObject GamePausedPanel;
        [SerializeField] private GameObject GameOverPanel;
        [SerializeField] private GameObject VictoryText;
        [SerializeField] private GameObject DefeatText;

        [SerializeField] private GameObject PlaneMarkerPrefab;
        [SerializeField] private GameObject WallPrefab;
        [SerializeField] private GameObject TowerPrefab;
        [SerializeField] private GameObject HomeBasePrefab;
        [SerializeField] private GameObject GamePlanePrefab;
        [SerializeField] private GameObject DivisionPrefab;
        [SerializeField] private GameObject SpawnerPrefab;
        [SerializeField] private GameObject PathPrefab;

        private const float k_DivisionLength = .1f;

        public static bool LastWave { get; set; }
        public static bool EnemyReachedBase { get; set; }

        private GameObject m_PlaneSelectionMarker;
        private DetectedPlane m_MarkedPlane;
        public static Pose MarkedPlaneCenterPose { get; private set; }
        private Vector3[] m_BindingVectors;
        private GameObject[] m_BindingWalls;
        private GameObject[] m_BindingTowers;
        private GameObject m_GamePlane;
        private GameObject m_HomeBase;
        private Division m_HomeBaseDivision;
        private GameObject m_Spawner;
        private Division m_SpawnerDivision;

        /// <summary>
        /// Represents the current free division to which the camera center is pointing.
        /// </summary>
        private Division m_DivisionToPlaceOn;
        /// <summary>
        /// Represents the game object prefab corresponding to the current context that should be placed
        /// </summary>
        private GameObject m_GameObjectToBePlaced;
        /// <summary>
        /// Represents the previously placed game object
        /// </summary>
        private GameObject m_PlacedGameObject;
        /// <summary>
        /// Represents the division to which the previously placed game object belongs to
        /// </summary>
        private Division m_DivisionPlacedOn;
        /// <summary>
        /// Denotes whether the plane splitting has finished
        /// </summary>
        private bool m_PlaneSplit;

        /// <summary>
        /// An anchor to the center of the marked plane
        /// </summary>
        public static Transform AnchorTransform { get; private set; }
        /// <summary>
        /// A set of all divisions that contain no game object
        /// </summary>
        public static HashSet<Division> AvailableDivisions { get; private set; } // TODO: Remove
        public static HashSet<GameObject> AvailableDivisionObjects { get; private set; }
        /// <summary>
        /// A dictionary of divisions and their corresponding division game object instance
        /// </summary>
        public static Dictionary<Division, GameObject> DivisionGameObjectDictionary { get; private set; } // TODO: Remove
        /// <summary>
        /// A set of all divisions that will contain paths
        /// </summary>
        private Stack<Division> m_PathDivisions;
        /// <summary>
        /// The final path division, leading to the home base
        /// </summary>
        private Division m_PathEnd;
        /// <summary>
        /// The starting path division, leading out of the spawner 
        /// </summary>
        private Division m_PathStart;
        /// <summary>
        /// A collection of divisions used for the path generation phase.
        /// Stores divisions that are not occupied by the path or divisions that are not adjacent to a path.
        /// </summary>
        private HashSet<Division> m_AvailableDivisionsForPathGeneration;
        /// <summary>
        /// The initial desired rate of divisions not containing by paths and that are
        /// not divisions adjacent to a path 
        /// </summary>
        private float m_AvailableDivisionsThreshold = .3f;
        /// <summary>
        /// Each time this field reached a value equal to k_IncreaseThresholdCountLimit, the m_AvailableDivisionsThreshold is modified
        /// </summary>
        private float m_IncreaseThresholdCount = 1;
        /// <summary>
        /// The time iteration limit after which the threshold increases
        /// </summary>
        private const float k_IncreaseThresholdCountLimit = 2000;
        /// <summary>
        /// The percentage by which the threshold increases
        /// </summary>
        private const float k_IncreaseThresholdSize = .1f;
        /// <summary>
        /// Precision used for floating point comparison
        /// </summary>
        private const float k_Epsilon = 1e-5f;
        /// <summary>
        /// A collection of 4 vectors depicting movement forward, backward, leftward and rightward on the horizontal plane
        /// </summary>
        private Vector3[] m_Moves;

        public static Transform[] PathWaypoints { get; private set; }

        private enum GameState
        {
            STARTED,
            GRID_DETECTION,
            GAME_SPACE_INSTANTIATION,
            BASE_PLACEMENT,
            PATH_GENERATION,
            GAME_OVER,
            PAUSED,
            GAME_LOOP
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
                    GridDetectionPanel.SetActive(true);
                    _GridDetectionLogic();
                    break;
                case GameState.GAME_SPACE_INSTANTIATION:
                    GridDetectionPanel.SetActive(false);
                    _GameSpaceInitializationLogic();
                    AdvanceGameState();
                    break;
                case GameState.BASE_PLACEMENT:
                    GameInitializationPanel.SetActive(true);
                    if (!m_PlaneSplit)
                    {
                        _SplitPlane();
                    }
                    else
                    {
                        m_GameObjectToBePlaced = HomeBasePrefab;
                        _UpdatePlaceButtonState();
                        if (m_PlacedGameObject != null)
                        {
                            m_HomeBase = m_PlacedGameObject;
                            m_HomeBaseDivision = m_DivisionPlacedOn;
                            ToGameLoopButton.SetActive(true);
                        }
                    }

                    break;
                case GameState.PATH_GENERATION:
                    GameInitializationPanel.SetActive(false);
                    if (m_Moves == null)
                    {
                        _InitializeMoves();
                    }

                    if (_GeneratePath() != null)
                    {
                        _BuildPath();
                        m_HomeBase.transform.Rotate(0, 180 ,0);
                        AdvanceGameState();
                    }
                    break;
                case GameState.GAME_LOOP:
                    GameLoopPanel.SetActive(true);
                    _GameLoopLogic();
                    break;
                case GameState.PAUSED:
                    break;
                case GameState.GAME_OVER:
                    break;
                default:
                    throw new InvalidOperationException(
                        $"The session is in an undefined state. Current state: {m_GameState}");
            }
        }
        
        public void AdvanceGameState()
        {
            m_PlacedGameObject = null;
            PlacePrefabButton.SetActive(false);
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
                    m_GameState = GameState.BASE_PLACEMENT;
                    _InitializeBasePlacement();
                    break;
                case GameState.BASE_PLACEMENT:
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
                    throw new InvalidOperationException(
                        "Method should not be accessed when the session is in PAUSED state.");
                default:
                    throw new InvalidOperationException(
                        "The session is in an undefined state. Current state: {m_GameStage}");
            }
            Debug.Log($"Game stage changed to {m_GameState}");
        }

        /// <summary>
        /// Sets the state of the BuildButton (enabled or disabled).
        /// <para>
        /// Enabled: the center of the camera points to an available division (one that does not contain any child game object).
        /// </para>
        /// <para>
        /// Disabled: otherwise.
        /// </para>
        /// </summary>
        private void _UpdatePlaceButtonState()
        {
            var ray = new Ray(FirstPersonCamera.transform.position, FirstPersonCamera.transform.forward);
            var hits = Physics.RaycastAll(ray);

            if (!hits.Any())
            {
                PlacePrefabButton.SetActive(false);
                m_DivisionToPlaceOn = null;
                return;
            }

            Transform divisionTransform = hits.FirstOrDefault(h => h.collider.CompareTag("Division")).transform;
            if (divisionTransform == null)
            {
                PlacePrefabButton.SetActive(false);
                m_DivisionToPlaceOn = null;
                return;
            }

            m_DivisionToPlaceOn = AvailableDivisions.SingleOrDefault(div => div.Includes(divisionTransform.position));
            if (m_DivisionToPlaceOn != null)
            {
                PlacePrefabButton.SetActive(true);
            }
            else
            {
                PlacePrefabButton.SetActive(false);
                m_DivisionToPlaceOn = null;
            }

        }

        /// <summary>
        /// Places a game object prefab corresponding to the current context at a valid division pointed to by the camera.
        /// <para>The method is triggered on BuildButton click.</para>
        /// </summary>
        public void PlacePrefab()
        {
            if (m_PlacedGameObject != null)
            {
                Destroy(m_PlacedGameObject);
                AvailableDivisions.Add(m_DivisionPlacedOn);
            }

            GameObject newGameObject = Instantiate(m_GameObjectToBePlaced,
                m_DivisionToPlaceOn.Center, Quaternion.identity,
                DivisionGameObjectDictionary[m_DivisionToPlaceOn].transform);
            AvailableDivisions.Remove(m_DivisionToPlaceOn);
            m_PlacedGameObject = newGameObject;
            m_DivisionPlacedOn = m_DivisionToPlaceOn;
            Debug.Log("Placed game object");
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
            m_GameState = GameState.BASE_PLACEMENT;
        }

        public void ToGameLoopState()
        {
            m_GameState = GameState.BASE_PLACEMENT;
        }

        public void Pause()
        {
            m_GameState = GameState.PAUSED;
            Time.timeScale = 0;
            Crosshair.SetActive(false);
            GameLoopPanel.SetActive(false);
            GamePausedPanel.SetActive(true);
        }

        public void Unpause()
        {
            Crosshair.SetActive(true);
            GamePausedPanel.SetActive(false);
            GameLoopPanel.SetActive(true);
            Time.timeScale = 1;
            m_GameState = GameState.GAME_LOOP;
        }

        public void GameOver(bool victory)
        {
            Time.timeScale = 0;
            Crosshair.SetActive(false);
            GameLoopPanel.SetActive(false);
            m_GameState = GameState.GAME_OVER;
            GameOverPanel.SetActive(true);
            if (victory)
            {
                VictoryText.SetActive(true);
            }
            else
            {
                DefeatText.SetActive(true);
            }
        }

        private void _InitializeGameSpaceInstantiation()
        {
            Debug.Log($"Initializing {GameState.GAME_SPACE_INSTANTIATION} state");
            Destroy(m_PlaneSelectionMarker);
            List<Vector3> boundaryPolygons = new List<Vector3>();
            m_MarkedPlane.GetBoundaryPolygon(boundaryPolygons);
            m_BindingVectors = boundaryPolygons.ToArray();
            MarkedPlaneCenterPose = m_MarkedPlane.CenterPose; // TODO: REMOVE ???

            foreach (Vector3 boundaryPolygon in boundaryPolygons)
            {
                Console.WriteLine("( " + boundaryPolygon.x + ", " + boundaryPolygon.y + ", " + boundaryPolygon.z +
                                  " )");
            }

            GridGenerator.SetActive(false);
            PointCloud.SetActive(false);
            Debug.Log("Grid generation disabled");
            ToBasePlacementButton.SetActive(false);
            Debug.Log("ConfirmButton disabled");
        }

        private void _InitializeBasePlacement()
        {
            Crosshair.SetActive(true);
            ToBasePlacementButton.SetActive(false);
            Debug.Log("ConfirmButton disabled");
        }

        private void _InitializePathGeneration()
        {
            _GeneratePathEnd();
        }

        private void _InitializeGameLoop()
        {
            CoinManager.SetActive(true);
            BuildingManager.SetActive(true);
            m_GameObjectToBePlaced = null;
            Debug.Log("Game loop initialized");
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


                            Anchor anchor = hit.Trackable.CreateAnchor(plane.CenterPose);
                            m_PlaneSelectionMarker =
                                Instantiate(PlaneMarkerPrefab, hit.Pose.position, hit.Pose.rotation);
                            gameObject.transform.parent = anchor.transform;
                            m_MarkedPlane = plane;
                            //AnchorTransform = m_MarkedPlane.CreateAnchor(MarkedPlaneCenterPose).transform;
                            AnchorTransform = anchor.transform;
                            Debug.Log("New base marker placed");
                            ToBasePlacementButton.SetActive(true);
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
            m_BindingTowers = m_BindingVectors
                .Select(v => Instantiate(TowerPrefab, v, Quaternion.identity, AnchorTransform)).ToArray();

            m_BindingWalls = new GameObject[m_BindingVectors.Length];
            for (int i = 0; i < m_BindingVectors.Length; i++)
            {
                m_BindingWalls[i] = Instantiate(WallPrefab,
                    Vector3.Lerp(m_BindingVectors[i], m_BindingVectors[(i + 1) % m_BindingVectors.Length], 0.5f),
                    Quaternion.identity, AnchorTransform);
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

            m_GamePlane = Instantiate(GamePlanePrefab, new Vector3(middleX, m_BindingVectors[0].y, middleZ),
                Quaternion.identity, AnchorTransform);
            m_GamePlane.transform.localScale = new Vector3(maxDistance, maxDistance, 1);
            m_GamePlane.transform.Rotate(90, 0, 0);
            Debug.Log(m_GamePlane.transform.localScale);
        }

        // ########################################################
        // ################### PATH GENERATION  ###################
        // ########################################################

        private void _SplitPlane()
        {
            Renderer rend = m_GamePlane.GetComponent<Renderer>();
            Debug.Log("Acquired game plane renderer");

            float y = rend.bounds.min.y;

            List<Division> divisions = new List<Division>();

            for (var x = rend.bounds.min.x; x < rend.bounds.max.x; x += k_DivisionLength)
            {
                for (var z = rend.bounds.min.z; z < rend.bounds.max.z; z += k_DivisionLength)
                {
                    divisions.Add(new Division(new Vector3(x, y, z),
                        new Vector3(x + k_DivisionLength, y, z + k_DivisionLength)));
                }
            }

            _TrimDivisions(divisions);

            DivisionGameObjectDictionary = new Dictionary<Division, GameObject>(divisions.Count);
            AvailableDivisionObjects = new HashSet<GameObject>();
            Debug.Log($"Will spawn {divisions.Count} divisions.");
            foreach (var division in divisions)
            {
                var divisionObject =
                    Instantiate(DivisionPrefab, division.Center, Quaternion.identity, AnchorTransform);
                Debug.Log("Spawned division marker.");
                DivisionGameObjectDictionary.Add(division, divisionObject);
                AvailableDivisionObjects.Add(divisionObject);
            }

            AvailableDivisions =
                new HashSet<Division>(divisions); // TODO: load divisions into m_availableDivisions directly 
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

                raycastHits = Physics.RaycastAll(new Vector3(div.Point1.x, div.Point1.y, div.Point2.z), Vector3.right,
                    Mathf.Infinity);

                if (!_IsWithinBoundaries(raycastHits))
                {
                    // Debug.Log("Division removed on fourth check");
                    return true;
                }

                raycastHits = Physics.RaycastAll(new Vector3(div.Point2.x, div.Point1.y, div.Point1.z), Vector3.right,
                    Mathf.Infinity);

                if (!_IsWithinBoundaries(raycastHits))
                {
                    return true;
                }


                return false;
            });

            Debug.Log($"Division count after trimming: {divisions.Count}");
        }

        public bool _IsWithinBoundaries(RaycastHit[] hits)
        {
            if (hits.Length == 1 && hits[0].collider.CompareTag("GameWall")) return true;
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

        private void _GeneratePathEnd()
        {
            Vector3 front = m_HomeBaseDivision.Center - m_HomeBase.transform.forward * k_DivisionLength;
            m_PathEnd = DivisionGameObjectDictionary.SingleOrDefault(div => div.Key.Includes(front)).Key;
            
            if (m_PathEnd != null) return;
            
            front = m_HomeBaseDivision.Center - m_HomeBase.transform.forward * -1;
            m_PathEnd = DivisionGameObjectDictionary.SingleOrDefault(div => div.Key.Includes(front)).Key;
            // TODO: may crash if placed in a division with a single neighbor, investigate
        }

        private Division _GeneratePath()
        {
            Debug.Log("Started generating m_PathDivisions.");
            m_AvailableDivisionsForPathGeneration = new HashSet<Division>(AvailableDivisions);
            m_PathDivisions = new Stack<Division>();
            return _GenerateRandomPath(m_PathEnd);
        }

        private void _InitializeMoves()
        {
            m_Moves = new[]
            {
                new Vector3(k_DivisionLength, 0, 0),
                new Vector3(-k_DivisionLength, 0, 0),
                new Vector3(0, 0, k_DivisionLength),
                new Vector3(0, 0, -k_DivisionLength)
            };
        }

        private Division _GenerateRandomPath(Division currentDivision)
        {
            // After each 2000 calls on this method the number of divisions which were
            // not covered by paths increases in order to reduce path generation time
            if (Math.Abs(m_IncreaseThresholdCount++ % k_IncreaseThresholdCountLimit) < k_Epsilon)
            {
                m_AvailableDivisionsThreshold += k_IncreaseThresholdSize;
            }

            if (m_AvailableDivisionsForPathGeneration.Count < m_AvailableDivisionsThreshold * DivisionGameObjectDictionary.Count &&
                _CanPlaceSpawner(currentDivision))
            {
                m_PathDivisions.Push(currentDivision);
                return currentDivision;
            }

            Debug.Log("Started generating random m_PathDivisions.");

            Division previousDivision = null;

            // Remove neighbors of previous m_PathDivisions
            if (m_PathDivisions.Any())
            {
                previousDivision = m_PathDivisions.Peek();
            }

            List<Division> markedDivisions = new List<Division>();

            if (previousDivision != null)
            {
                Debug.Log("Previous division found.");
                foreach (Vector3 move in m_Moves)
                {
                    var neighborCenter = move + previousDivision.Center;
                    var neighborDivision = m_AvailableDivisionsForPathGeneration.FirstOrDefault(div => div.Includes(neighborCenter));
                    if (neighborDivision != null)
                    {
                        markedDivisions.Add(neighborDivision);
                    }
                }
            }

            m_AvailableDivisionsForPathGeneration.RemoveWhere(div => markedDivisions.Contains(div));

            IEnumerable<Division> possibleNextDivisions = _RandomizeNextDivisions(currentDivision);
            m_PathDivisions.Push(currentDivision);
            Debug.Log($"The m_PathDivisions contains {m_PathDivisions.Count} divisions.");
            foreach (Division nextDivision in possibleNextDivisions)
            {
                m_AvailableDivisionsForPathGeneration.Remove(nextDivision);
                var res = _GenerateRandomPath(nextDivision);
                if (res != null) return res;
                m_AvailableDivisionsForPathGeneration.Add(nextDivision);
            }

            foreach (Division markedDivision in markedDivisions)
            {
                m_AvailableDivisionsForPathGeneration.Add(markedDivision);
            }

            m_PathDivisions.Pop();
            return null;
        }

        private IEnumerable<Division> _RandomizeNextDivisions(Division currentDivision)
        {
            List<Vector3> centers = m_Moves.Select(mov => currentDivision.Center + mov).ToList();
            List<Division> divisions = new List<Division>();
            foreach (Vector3 center in centers)
            {
                var nextDivision = m_AvailableDivisionsForPathGeneration.FirstOrDefault(div => div.Includes(center));
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
            Division previousDivision = m_PathDivisions.Peek();
            var direction = currentDivision.Center - previousDivision.Center;
            m_SpawnerDivision =
                m_AvailableDivisionsForPathGeneration.FirstOrDefault(div => div.Includes(currentDivision.Center + direction));
            if (m_SpawnerDivision != null)
            {
                m_Spawner = Instantiate(SpawnerPrefab, m_SpawnerDivision.Center, Quaternion.identity, DivisionGameObjectDictionary[m_SpawnerDivision].transform);
                AvailableDivisions.Remove(m_SpawnerDivision);
                AvailableDivisionObjects.Remove(DivisionGameObjectDictionary[m_SpawnerDivision]);
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
            Debug.Log($"Started path building. The path contains {m_PathDivisions.Count} path divisions.");
            PathWaypoints = new Transform[m_PathDivisions.Count + 2];
            PathWaypoints[0] = DivisionGameObjectDictionary[m_SpawnerDivision].transform;
            int index = 1;
            foreach (Division pathDivision in m_PathDivisions)
            {
                var pathObject = Instantiate(PathPrefab, pathDivision.Center, Quaternion.identity);
                pathObject.transform.parent = DivisionGameObjectDictionary[pathDivision].transform;
                AvailableDivisions.Remove(pathDivision);
                AvailableDivisionObjects.Remove(DivisionGameObjectDictionary[pathDivision]);
                PathWaypoints[index++] = DivisionGameObjectDictionary[pathDivision].transform;
            }

            PathWaypoints[index] = DivisionGameObjectDictionary[m_HomeBaseDivision].transform;
        }

        // ########################################################
        // ###################### GAME LOOP #######################
        // ########################################################

        private void _GameLoopLogic()
        {
            if (EnemyReachedBase)
            {
                GameOver(false);
            }

            if (LastWave && !GameObject.FindGameObjectsWithTag("enemyBug").Any())
            {
                GameOver(true);
            }
        }

        // ########################################################
        // ###################### GAME OVER #######################
        // ########################################################

        private void _GameOverLogic()
        {
        }

        private void _UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            // Only allow the screen to sleep when not tracking.
            Screen.sleepTimeout = Session.Status != SessionStatus.Tracking
                ? SleepTimeout.SystemSetting
                : SleepTimeout.NeverSleep;

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

}
