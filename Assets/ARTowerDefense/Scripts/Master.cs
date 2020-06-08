using System;
using System.Collections.Generic;
using System.Linq;
using ARTowerDefense.AR;
using ARTowerDefense.Managers;
using GoogleARCore;
using GoogleARCore.Examples.Common;
using UnityEngine;

#if UNITY_EDITOR
using Input = GoogleARCore.InstantPreviewInput;
#endif
#pragma warning disable 649
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable ArrangeTypeMemberModifiers

namespace ARTowerDefense
{
    /// <summary></summary>
    /// <remarks></remarks>
    public class Master : MonoBehaviour
    {
        #region #References

        [SerializeField] private Camera FirstPersonCamera;
        [SerializeField] private GameObject ARCoreDevice;

        #endregion

        #region #Prefabs

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

        #region #Managers

        [SerializeField] private GameObject PlaneGenerator;
        [SerializeField] private GameObject PlaneSelectionManager;
        [SerializeField] private GameObject GameInitManager;
        [SerializeField] private GameObject BuildingManager;
        [SerializeField] private GameObject CoinManager;
        [SerializeField] private GameObject NatureManager;

        #endregion

        #region #Panels

        [SerializeField] private GameObject MainMenuPanel;
        [SerializeField] private GameObject PlaneSelectionPanel;
        [SerializeField] private GameObject BasePlacementPanel;
        [SerializeField] private GameObject GameLoopPanel;
        [SerializeField] private GameObject GamePausedPanel;
        [SerializeField] private GameObject GameOverPanel;

        #endregion

        #region #Constants

        /// <summary>
        /// The length of a division
        /// </summary>
        public const float k_DivisionLength = .1f;

        #endregion

        #region #State Holders

        public static bool LastWave { get; set; }
        public static bool EnemyReachedBase { get; set; }
        public List<Vector3> BindingVectors { get; private set; }
        private Division m_HomeBaseDivision;
        private Division m_SpawnerDivision;
        private bool m_Victory;

        /// <summary>
        /// Represents the current free division to which the camera center is pointing.
        /// </summary>
        private Division m_DivisionToPlaceOn;

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
        public static Dictionary<Division, BuildingDivision> DivisionGameObjectDictionary { get; set; }

        /// <summary>
        /// The final path division, leading to the home base
        /// </summary>
        private Division m_PathEnd;

        public static Transform[] PathWayPoints { get; private set; }

        private GameState m_GameState;

        private bool m_IsQuitting;

        #endregion

        #region #EventFunctions

        void Awake()
        {
            Application.targetFrameRate = 60;
        }
        
        void Start()
        {
            m_GameState = GameState.MAIN_MENU;
            MainMenuPanel.SetActive(true);
        }

        void Update()
        {
            _UpdateApplicationLifecycle();
            switch (m_GameState)
            {
                case GameState.MAIN_MENU:
                    // Wait for user to press PLAY
                    break;
                case GameState.PLANE_SELECTION:
                    // Wait for user to choose a plane
                    break;
                case GameState.GAME_SPACE_INSTANTIATION:
                    _GameSpaceInstantiationLogic();
                    break;
                case GameState.BASE_PLACEMENT:
                    _BasePlacementLogic();
                    break;
                case GameState.PATH_GENERATION:
                    _PathGenerationLogic();
                    break;
                case GameState.GAME_LOOP:
                    _GameLoopLogic();
                    break;
                case GameState.PAUSED:
                    // Wait for user to resume or go to main menu
                    break;
                case GameState.GAME_OVER:
                    // Wait for user to go to main menu
                    break;
                default:
                    throw new InvalidOperationException(
                        $"The session is in an undefined state. Current state: {m_GameState}");
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && m_GameState == GameState.GAME_LOOP)
            {
                Pause();
            }
        }

        #endregion

        #region #Game_Progression

        public void AdvanceGameState()
        {
            PlacePrefabButton.SetActive(false);
            switch (m_GameState)
            {
                case GameState.MAIN_MENU:
                    m_GameState = GameState.PLANE_SELECTION;
                    _InitializePlaneSelection();
                    break;
                case GameState.PLANE_SELECTION:
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
                default:
                    throw new InvalidOperationException(
                        $"Method should not be accessed when the session is in {m_GameState} state.");
            }
            Debug.Log($"Game stage changed to {m_GameState}");
        }

        #endregion

        #region #MAIN_MENU

        public void QuitApplication()
        {
            Application.Quit();
        }

        #endregion

        #region #PLANE_SELECTION

        private void _InitializePlaneSelection()
        {
            MainMenuPanel.SetActive(false);
            _SetARCoreElementsVisibility(true);
            PlaneSelectionPanel.SetActive(true);
            PlaneSelectionManager.SetActive(true);
        }

        #endregion

        #region #GAME_SPACE_INSTANTIATION

        private void _InitializeGameSpaceInstantiation()
        {
            Debug.Log($"Initializing {GameState.GAME_SPACE_INSTANTIATION} state");
            PlaneSelectionManager.SetActive(false);
            PlaneSelectionPanel.SetActive(false);
            _GetPlaneData();
            _SetARCoreElementsVisibility(false);
            Debug.Log("Plane generation manager disabled");
            ToBasePlacementButton.SetActive(false);
            Debug.Log("ConfirmButton disabled");
            GameInitManager.SetActive(true);
        }

        private void _GetPlaneData()
        {
            List<Vector3> boundaryPolygons = new List<Vector3>();
            var manager = PlaneSelectionManager.GetComponent<PlaneSelectionManager>();
            AnchorTransform = manager.AnchorTransform;
            var selectedPlane = manager.MarkedPlane;
            selectedPlane.GetBoundaryPolygon(boundaryPolygons);
            BindingVectors = boundaryPolygons;
            foreach (Vector3 boundaryPolygon in boundaryPolygons)
            {
                Console.WriteLine("( " + boundaryPolygon.x + ", " + boundaryPolygon.y + ", " + boundaryPolygon.z +
                                  " )");
            }
        }

        private void _GameSpaceInstantiationLogic()
        {
            // Wait for the game init manager to finish creating divisions and assigning them to Master
            // and advance to next state
            if (AnchorTransform != null && DivisionGameObjectDictionary != null)
            {
                AdvanceGameState();
            }
        }

        #endregion

        #region #BASE_PLACEMENT

        private void _InitializeBasePlacement()
        {
            GameInitManager.SetActive(false);
            BasePlacementPanel.SetActive(true);
        }

        private void _BasePlacementLogic()
        {
            _UpdatePlaceButtonState();
            if (m_DivisionPlacedOn != null)
            {
                //if (m_HomeBaseDivision != null && m_HomeBaseDivision != m_DivisionPlacedOn)
                //{
                //    DivisionGameObjectDictionary[m_HomeBaseDivision].Clear();
                //}

                m_HomeBaseDivision = m_DivisionPlacedOn;
                ToGameLoopButton.SetActive(true);
                // Advancement to next stage will happen when player confirms base location
            }
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
        /// <para>The method is triggered on BuildButton.</para>
        /// </summary>
        public void PlaceHomeBase()
        {
            if (m_DivisionPlacedOn != null)
            {
                DivisionGameObjectDictionary[m_DivisionPlacedOn].Clear();
            }

            DivisionGameObjectDictionary[m_DivisionToPlaceOn].AddBuilding(HomeBasePrefab);
            m_DivisionPlacedOn = m_DivisionToPlaceOn;
            Debug.Log("Placed game object");
        }

        #endregion

        #region #PATH_GENERATION

        private void _InitializePathGeneration()
        {
            DivisionGameObjectDictionary[m_HomeBaseDivision].Lock();
            BasePlacementPanel.SetActive(false);
            _GeneratePathEnd();
        }

        private void _GeneratePathEnd()
        {
            Vector3 front = m_HomeBaseDivision.Center - DivisionGameObjectDictionary[m_HomeBaseDivision].transform.forward * k_DivisionLength;
            m_PathEnd = DivisionGameObjectDictionary.SingleOrDefault(div => div.Key.Includes(front)).Key;

            if (m_PathEnd != null) return;

            m_PathEnd = DivisionGameObjectDictionary.SingleOrDefault(div => div.Key.Includes(front)).Key;
            // TODO: may crash if placed in a division with a single neighbor! Also, fix rotation of base to always face path
        }

        private void _PathGenerationLogic()
        {
            var manager = new PathGenerationManager(DivisionGameObjectDictionary, m_HomeBaseDivision, m_PathEnd,
                SpawnerPrefab, PathPrefab, CurvedPathPrefab);

            while (m_SpawnerDivision == null)
            {
                m_SpawnerDivision = manager.GeneratePath();
            }

            PathWayPoints = manager.BuildPath();

            AdvanceGameState();
        }

        #endregion

        #region #GAME_LOOP

        private void _InitializeGameLoop()
        {
            ToGameLoopButton.SetActive(false);
            NatureManager.SetActive(true);
            CoinManager.SetActive(true);
            BuildingManager.SetActive(true);
            GameLoopPanel.SetActive(true);
            Debug.Log("Game loop initialized");
        }

        private void _GameLoopLogic()
        {
            if (EnemyReachedBase)
            {
                AdvanceGameState();
            }

            if (LastWave && !GameObject.FindGameObjectsWithTag("EnemyHealth").Any())
            {
                m_Victory = true;
                AdvanceGameState();
            }
        }

        #endregion

        #region #GAME_OVER

        private void _InitializeGameOver()
        {
            _StopTime();
            GameLoopPanel.SetActive(false);
            GameOverPanel.SetActive(true);
            VictoryText.SetActive(m_Victory);
            DefeatText.SetActive(!m_Victory);
        }

        #endregion

        #region #PAUSED

        public void Pause()
        {
            m_GameState = GameState.PAUSED;
            _StopTime();
            GameLoopPanel.SetActive(false);
            GamePausedPanel.SetActive(true);
        }

        public void Unpause()
        {
            GamePausedPanel.SetActive(false);
            GameLoopPanel.SetActive(true);
            _StartTime();
            m_GameState = GameState.GAME_LOOP;
        }

        #endregion

        #region #RESTART

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
            }

            _StartTime();
            Destroy(AnchorTransform.gameObject);
            _Reset();
            m_GameState = GameState.MAIN_MENU;
            MainMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Sets all state holders to their default value and disables all GAME_LOOP managers
        /// </summary>
        private void _Reset()
        {
            m_HomeBaseDivision = default;
            m_DivisionPlacedOn = default;
            m_DivisionToPlaceOn = default;
            m_IsQuitting = default;
            m_PathEnd = default;
            m_SpawnerDivision = default;
            EnemyReachedBase = default;
            LastWave = default;
            m_Victory = default;
            BuildingManager.SetActive(false);
            CoinManager.SetActive(false);
            NatureManager.SetActive(false);
        }

        #endregion

        #region #Time

        private void _StopTime()
        {
            Time.timeScale = 0;
        }

        private void _StartTime()
        {
            Time.timeScale = 1;
        }

        #endregion

        #region #ARElementsFunctions

        private void _SetARCoreElementsVisibility(bool active)
        {
            _SetPointCloudVisibility(active);
            _SetPlanesVisibility(active);
        }

        private void _SetPointCloudVisibility(bool active)
        {
            PointCloud.GetComponent<PointCloudVisualizer>().SetMeshRendererActive(active);
        }

        private void _SetPlanesVisibility(bool active)
        {
            if (active)
            {
                PlaneGenerator.GetComponent<PlaneGenerator>().ShowPlanes();
            }
            else
            {
                PlaneGenerator.GetComponent<PlaneGenerator>().HidePlanes();
            }
        }

        #endregion

        #region #Android

        private void _UpdateApplicationLifecycle()
        {
            //// Exit the app when the 'back' button is pressed.
            //if (Input.GetKey(KeyCode.Escape))
            //{
            //    Application.Quit();
            //}

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
                Invoke("QuitApplication", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                _ShowAndroidToastMessage(
                    "ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("QuitApplication", 0.5f);
            }

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

        #endregion

        #region #Typedefs

        private enum GameState
        {
            MAIN_MENU,
            PLANE_SELECTION,
            GAME_SPACE_INSTANTIATION,
            BASE_PLACEMENT,
            PATH_GENERATION,
            GAME_LOOP,
            GAME_OVER,
            PAUSED
        }

        #endregion

    }

}
