using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.ARTowerDefense.Scripts;
using Assets.ARTowerDefense.Scripts.Managers;
using GoogleARCore;
using GoogleARCore.Examples.Common;
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

        [SerializeField] private GameObject GridGenerator;
        [SerializeField] private GameObject GridDetectionManager;
        [SerializeField] private GameObject GameInitManager;
        [SerializeField] private GameObject BuildingManager;
        [SerializeField] private GameObject CoinManager;
        [SerializeField] private GameObject NatureManager;

        #endregion

        #region Panels

        [SerializeField] private GameObject MainMenuPanel;
        [SerializeField] private GameObject GameInitializationPanel;
        [SerializeField] private GameObject GameLoopPanel;
        [SerializeField] private GameObject GamePausedPanel;
        [SerializeField] private GameObject GameOverPanel;

        #endregion

        #region Constants

        /// <summary>
        /// The length of a division
        /// </summary>
        public const float k_DivisionLength = .1f;

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


        public static Transform[] PathWaypoints { get; private set; }

        private enum GameState
        {
            MAIN_MENU,
            GRID_DETECTION,
            GAME_SPACE_INSTANTIATION,
            BASE_PLACEMENT,
            PATH_GENERATION,
            GAME_OVER,
            PAUSED,
            GAME_LOOP
        }

        private GameState m_GameState;

        private bool m_IsQuitting;

        void Awake()
        {
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            m_GameState = GameState.MAIN_MENU;
            MainMenuPanel.SetActive(true);
        }

        // Update is called once per frame
        void Update()
        {
            _UpdateApplicationLifecycle();

            switch (m_GameState)
            {
                case GameState.MAIN_MENU:
                    // Wait for user to press PLAY
                    break;
                case GameState.GRID_DETECTION:
                    // TODO: Add grid detection logic
                    break;
                case GameState.GAME_SPACE_INSTANTIATION:
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
                    var pathGenMan = new PathGenerationManager(DivisionGameObjectDictionary, m_HomeBaseDivision, m_PathEnd, SpawnerPrefab, PathPrefab, CurvedPathPrefab);

                    while (m_SpawnerDivision == null)
                    {
                        m_SpawnerDivision = pathGenMan.GeneratePath();
                    }

                    PathWaypoints = pathGenMan.BuildPath();

                    AdvanceGameState();
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
                case GameState.MAIN_MENU:
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
                default:
                    throw new InvalidOperationException(
                        $"Method should not be accessed when the session is in {m_GameState} state.");
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
                DivisionGameObjectDictionary[m_DivisionPlacedOn].Clear();
            }

            DivisionGameObjectDictionary[m_DivisionToPlaceOn].AddBuilding(m_GameObjectToBePlaced);
            m_DivisionPlacedOn = m_DivisionToPlaceOn;
            Debug.Log("Placed game object");
        }

        public void ToStartState()
        {
            m_GameState = GameState.MAIN_MENU;
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

        private void _InitializeGridDetection()
        {
            MainMenuPanel.SetActive(false);
            _SetGridsVisibility(true);
            _SetPointCloudVisibility(true);
            GridDetectionManager.SetActive(true);
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

            _SetGridsVisibility(false);
            _SetPointCloudVisibility(false);
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
            ToGameLoopButton.SetActive(false);
            NatureManager.SetActive(true);
            CoinManager.SetActive(true);
            BuildingManager.SetActive(true);
            m_GameObjectToBePlaced = null;
            Debug.Log("Game loop initialized");
        }

        private void _InitializeGameOver(bool victory)
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

        public void InitializeMainMenu()
        {
            if (m_GameState == GameState.GAME_OVER)
            {
                GameOverPanel.SetActive(false);
            }
            else if (m_GameState == GameState.PAUSED)
            {
                GamePausedPanel.SetActive(false);
            }
            else
            {
                throw new InvalidOperationException(
                    $"A new game should only be initialized if the game state is GameState.PAUSED or GameState.GAME_OVER. Current state: {m_GameState}.");
                // TODO: Force Reset to main menu
            }
            
            Time.timeScale = 1;
            Destroy(AnchorTransform.gameObject);
            BuildingManager.SetActive(false);
            CoinManager.SetActive(false);
            NatureManager.SetActive(false);
            ResetFields();
            BuildingManager.GetComponent<BuildingManager>().ResetManager();
            m_GameState = GameState.MAIN_MENU;
            MainMenuPanel.SetActive(true);
        }

        private void ResetFields()
        {
            m_HomeBaseDivision = default;
            m_DivisionPlacedOn = default;
            m_DivisionToPlaceOn = default;
            m_GameObjectToBePlaced = default;
            m_IsQuitting = default;
            m_PathDivisions = default;
            m_PathEnd = default;
            m_SpawnerDivision = default;
            EnemyReachedBase = default;
            LastWave = default;
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

        #region GAME LOOP

        private void _GameLoopLogic()
        {
            if (EnemyReachedBase)
            {
                _InitializeGameOver(false);
            }

            if (LastWave && !GameObject.FindGameObjectsWithTag("enemyBug").Any())
            {
                _InitializeGameOver(true);
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

        void OnApplicationPause(bool paused)
        {
            if (paused && m_GameState == GameState.GAME_LOOP)
            {
                Pause();
            }
        }

        private void _SetPointCloudVisibility(bool active)
        {
            PointCloud.GetComponent<PointcloudVisualizer>().SetMeshRendererActive(active);
        }

        private void _SetGridsVisibility(bool active)
        {
            if (active)
            {
                GridGenerator.GetComponent<GridGenerator>().ShowGrids();
            }
            else
            {
                GridGenerator.GetComponent<GridGenerator>().HideGrids();
            }
        }
    }

}
