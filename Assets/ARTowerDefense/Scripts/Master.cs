using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.ARTowerDefense.Scripts;
using GoogleARCore;
using UnityEngine;
using Random = System.Random;
#if UNITY_EDITOR
using Input = GoogleARCore.InstantPreviewInput;
#endif

namespace ARTowerDefense
{
    public class Master : MonoBehaviour
    {
        [SerializeField] private Camera FirstPersonCamera;
        [SerializeField] private GameObject ARCoreDevice;
        
        #region Prefabs

        [SerializeField] private GameObject GridGenerator;
        [SerializeField] private GameObject PointCloud;
        [SerializeField] private GameObject ToBasePlacementButton;
        [SerializeField] private GameObject ToGameLoopButton;
        [SerializeField] private GameObject PlacePrefabButton;
        [SerializeField] private GameObject VictoryText;
        [SerializeField] private GameObject DefeatText;
        [SerializeField] private GameObject HomeBasePrefab;
        [SerializeField] private GameObject SpawnerPrefab;
        [SerializeField] private GameObject PathPrefab;
        [SerializeField] private GameObject CurvedPathPrefab;

        #endregion

        #region Managers

        [SerializeField] private GameObject GridDetectionManager;
        [SerializeField] private GameObject GameInitManager;
        [SerializeField] private GameObject BuildingManager;
        [SerializeField] private GameObject CoinManager;
        [SerializeField] private GameObject NatureManager;

        #endregion

        #region Panels

        [SerializeField] private GameObject GameInitializationPanel;
        [SerializeField] private GameObject GameLoopPanel;
        [SerializeField] private GameObject GamePausedPanel;
        [SerializeField] private GameObject GameOverPanel;

        #endregion

        #region Constants

        public const float k_DivisionLength = .1f;
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

        #endregion

        public static bool LastWave { get; set; }
        public static bool EnemyReachedBase { get; set; }
        public DetectedPlane MarkedPlane { get; set; }
        public Vector3[] BindingVectors { get; private set; }
        private Division m_HomeBaseDivision;
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
        /// Represents the division to which the previously placed game object belongs to
        /// </summary>
        private Division m_DivisionPlacedOn;
        /// <summary>
        /// An anchor to the center of the marked plane
        /// </summary>
        public Transform AnchorTransform { get; set; }
        /// <summary>
        /// A dictionary of divisions and their corresponding division game object instance
        /// </summary>
        public static Dictionary<Division, BuildingDivision> DivisionGameObjectDictionary { get; private set; }
        /// <summary>
        /// A set of all divisions that will contain paths
        /// </summary>
        private Stack<Division> m_PathDivisions;
        /// <summary>
        /// The final path division, leading to the home base
        /// </summary>
        private Division m_PathEnd;
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

        private GameState m_GameState = GameState.STARTED;

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
                    AdvanceGameState();
                    break;
                case GameState.GRID_DETECTION:
                    // TODO: Add grid detection logic
                    break;
                case GameState.GAME_SPACE_INSTANTIATION:
                    _GameSpaceInitializationLogic();
                    AdvanceGameState();
                    break;
                case GameState.BASE_PLACEMENT:
                    m_GameObjectToBePlaced = HomeBasePrefab;
                    _UpdatePlaceButtonState();
                    if (m_DivisionPlacedOn != null)
                    {
                        if (m_HomeBaseDivision != null && m_HomeBaseDivision != m_DivisionPlacedOn)
                        {
                            DivisionGameObjectDictionary[m_HomeBaseDivision].Clear();
                        }
                        m_HomeBaseDivision = m_DivisionPlacedOn;
                        ToGameLoopButton.SetActive(true);
                    }
                    break;
                case GameState.PATH_GENERATION:
                    if (m_Moves == null)
                    {
                        _InitializeMoves();
                    }

                    if (_GeneratePath() != null)
                    {
                        _BuildPath();
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
            PlacePrefabButton.SetActive(false);
            switch (m_GameState)
            {
                case GameState.STARTED:
                    m_GameState = GameState.GRID_DETECTION;
                    _InitializeGridDetection();
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
                    //_InitializeGridDetection();
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

            m_DivisionToPlaceOn = DivisionGameObjectDictionary.Keys.SingleOrDefault(div => div.Includes(divisionTransform.position));
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
            if (m_DivisionPlacedOn != null)
            {
                //Destroy(m_PlacedGameObject);
                DivisionGameObjectDictionary[m_DivisionPlacedOn].Clear();
                //AvailableDivisions.Add(m_DivisionPlacedOn);
            }

            //GameObject newGameObject = Instantiate(m_GameObjectToBePlaced,
            //    m_DivisionToPlaceOn.Center, Quaternion.identity,
            //    DivisionGameObjectDictionary[m_DivisionToPlaceOn].transform);
            DivisionGameObjectDictionary[m_DivisionToPlaceOn].AddBuilding(m_GameObjectToBePlaced);
            //m_PlacedGameObject = newGameObject;
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
            GameLoopPanel.SetActive(false);
            GamePausedPanel.SetActive(true);
        }

        public void Unpause()
        {
            GamePausedPanel.SetActive(false);
            GameLoopPanel.SetActive(true);
            Time.timeScale = 1;
            m_GameState = GameState.GAME_LOOP;
        }

        private void _InitializeGameSpaceInstantiation()
        {
            Debug.Log($"Initializing {GameState.GAME_SPACE_INSTANTIATION} state");
            GridDetectionManager.SetActive(false);
            List<Vector3> boundaryPolygons = new List<Vector3>();
            var script = GridDetectionManager.GetComponent<GridDetectionManager>();
            AnchorTransform = script.AnchorTransform;
            MarkedPlane = script.MarkedPlane;
            MarkedPlane.GetBoundaryPolygon(boundaryPolygons);
            BindingVectors = boundaryPolygons.ToArray();

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
            GameInitManager.SetActive(true);
        }

        private void _InitializeBasePlacement()
        {
            ToBasePlacementButton.SetActive(false);
            var script = GameInitManager.GetComponent<GameInitManager>();
            DivisionGameObjectDictionary = script.DivisionsDictionary;
            GameInitManager.SetActive(false);
            GameInitializationPanel.SetActive(true);
            Debug.Log("ConfirmButton disabled");
        }

        private void _InitializePathGeneration()
        {
            DivisionGameObjectDictionary[m_HomeBaseDivision].Lock();
            GameInitializationPanel.SetActive(false);
            _GeneratePathEnd();
        }

        private void _InitializeGameLoop()
        {
            NatureManager.SetActive(true);
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
            GridDetectionManager.SetActive(true);
        }

        private void _GameSpaceInitializationLogic()
        {
            //List<Vector3> bindingVectorsList = BindingVectors.ToList();
            //_ConsolidateBoundaries(bindingVectorsList);
            //BindingVectors = bindingVectorsList.ToArray();

            //if (BindingWalls == null)
            //{
            //    _SpawnBoundaries();
            //}

            //if (GamePlane == null)
            //{
            //    _SpawnGamePlane();
            //}

            //GameInitManager.SetActive(true);
            //GameInitManager script = GameInitManager.GetComponent<GameInitManager>();
            // Binding vectors may be consolidated by the game init manager
            //BindingVectors = script.BindingVectors;
            //BindingTowers = script.BindingTowers;
            //BindingWalls = script.BindingWalls;
            //GamePlane = script.GamePlane;

            //_PlaceBaseMarker();
        }

        private void _GeneratePathEnd()
        {
            Vector3 front = m_HomeBaseDivision.Center - DivisionGameObjectDictionary[m_HomeBaseDivision].transform.forward * k_DivisionLength;
            m_PathEnd = DivisionGameObjectDictionary.SingleOrDefault(div => div.Key.Includes(front)).Key;
            
            if (m_PathEnd != null) return;
            
            //front = m_HomeBaseDivision.Center - m_HomeBase.transform.forward * -1;
            m_PathEnd = DivisionGameObjectDictionary.SingleOrDefault(div => div.Key.Includes(front)).Key;
            // TODO: may crash if placed in a division with a single neighbor! Also, fix rotation of base to always face path
        }

        private Division _GeneratePath()
        {
            Debug.Log("Started generating m_PathDivisions.");
            m_AvailableDivisionsForPathGeneration = new HashSet<Division>(DivisionGameObjectDictionary
                .Where(kvp => !kvp.Value.HasBuilding)
                .Select(kvp => kvp.Key));
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
                _TryPlaceSpawner(currentDivision))
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

        private bool _TryPlaceSpawner(Division currentDivision)
        {
            var previousDivision = m_PathDivisions.Peek();
            var direction = currentDivision.Center - previousDivision.Center;
            m_SpawnerDivision =
                DivisionGameObjectDictionary.FirstOrDefault(kvp => kvp.Key.Includes(currentDivision.Center + direction))
                    .Key;
            if (m_SpawnerDivision == null) return false;
            // Sets the rotation to 90 degrees if the path reaches the spawner from its side 
            float rotation = Math.Abs(direction.z) < k_Epsilon ? 90 : 0; 
            DivisionGameObjectDictionary[m_SpawnerDivision].AddBuilding(SpawnerPrefab, rotation);
            DivisionGameObjectDictionary[m_SpawnerDivision].Lock();
            return true;

        }

        //private bool ColliderContainsPoint(Transform colliderTransform, Vector3 point)
        //{
        //    Vector3 localPos = colliderTransform.InverseTransformPoint(point);
        //    return Mathf.Abs(localPos.x) < 0.5f && Mathf.Abs(localPos.y) < 0.5f && Mathf.Abs(localPos.z) < 0.5f;
        //}

        private void _BuildPath()
        {
            Debug.Log($"Started path building. The path contains {m_PathDivisions.Count} path divisions.");
            PathWaypoints = new Transform[m_PathDivisions.Count + 2];
            PathWaypoints[0] = DivisionGameObjectDictionary[m_SpawnerDivision].transform;
            var pathDivisionsArray = m_PathDivisions.ToArray();
            int index = 1;
            var prevDiv = m_SpawnerDivision;
            for(int i = 0; i < pathDivisionsArray.Length; i++)
            {
                var nextDiv = i + 1 != pathDivisionsArray.Length ? pathDivisionsArray[i+1] : m_HomeBaseDivision;
                var currDiv = pathDivisionsArray[i];
                var diff1 = currDiv.Center - prevDiv.Center;
                var diff2 = currDiv.Center - nextDiv.Center;
                var diff3 = prevDiv.Center - nextDiv.Center;

                if (Math.Abs(diff1.x - diff2.x) < k_Epsilon)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(PathPrefab, 90);
                }
                else if (Math.Abs(diff1.z - diff2.z) < k_Epsilon)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(PathPrefab);
                }
                else if (diff1.z < 0 && diff2.x < 0 && diff3.x < diff3.z || diff1.x < 0 && diff2.z < 0 && diff3.x > diff3.z)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(CurvedPathPrefab);
                }
                else if (diff3.x > 0 && diff1.z < 0 && diff2.x > 0 || diff3.x < 0 && diff1.x > 0 && diff2.z < 0)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(CurvedPathPrefab, -90);
                }
                else if (diff3.x > 0 && diff1.x < 0 && diff2.z > 0 || diff3.x < 0 && diff1.z > 0 && diff2.x < 0)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(CurvedPathPrefab, 90);
                }
                else
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(CurvedPathPrefab, 180);
                }

                DivisionGameObjectDictionary[currDiv].Lock();
                PathWaypoints[index++] = DivisionGameObjectDictionary[currDiv].transform;
                prevDiv = currDiv;
            }

            PathWaypoints[index] = DivisionGameObjectDictionary[m_HomeBaseDivision].transform; // TODO: Refactor
        }

        #region GAME LOOP

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

        #endregion

        #region GAME OVER
        private void _GameOverLogic()
        {
        }

        public void GameOver(bool victory)
        {
            Time.timeScale = 0;
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

        #endregion
        
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
