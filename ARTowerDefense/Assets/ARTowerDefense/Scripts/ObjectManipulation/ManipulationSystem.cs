using Assets.ARTowerDefense.Scripts.ObjectManipulation.Gestures;
using UnityEngine;

namespace Assets.ARTowerDefense.Scripts.GameSpaceDetection
{
    class ManipulationSystem : MonoBehaviour
    {
        private static ManipulationSystem s_Instance = null;

        private DragGestureRecognizer m_DragGestureRecognizer = new DragGestureRecognizer();

        private TapGestureRecognizer m_TapGestureRecognizer = new TapGestureRecognizer();

        /// <summary>
        /// Gets the ManipulationSystem instance.
        /// </summary>
        public static ManipulationSystem Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var manipulationSystems = FindObjectsOfType<ManipulationSystem>();
                    if (manipulationSystems.Length > 0)
                    {
                        s_Instance = manipulationSystems[0];
                    }
                    else
                    {
                        Debug.LogError("No instance of ManipulationSystem exists in the scene.");
                    }
                }

                return s_Instance;
            }
        }

        /// <summary>
        /// Gets the Drag gesture recognizer.
        /// </summary>
        public DragGestureRecognizer DragGestureRecognizer
        {
            get
            {
                return m_DragGestureRecognizer;
            }
        }

        /// <summary>
        /// Gets the Tap gesture recognizer.
        /// </summary>
        public TapGestureRecognizer TapGestureRecognizer
        {
            get
            {
                return m_TapGestureRecognizer;
            }
        }

        /// <summary>
        /// Gets the current selected object.
        /// </summary>
        public GameObject SelectedObject { get; private set; }

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            if (Instance != this)
            {
                Debug.LogWarning("Multiple instances of ManipulationSystem detected in the scene." +
                                 " Only one instance can exist at a time. The duplicate instances" +
                                 " will be destroyed.");
                DestroyImmediate(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            DragGestureRecognizer.Update();
            TapGestureRecognizer.Update();
        }

        /// <summary>
        /// Deselects the selected object if any.
        /// </summary>
        internal void Deselect()
        {
            SelectedObject = null;
        }

        /// <summary>
        /// Select an object.
        /// </summary>
        /// <param name="target">The object to select.</param>
        internal void Select(GameObject target)
        {
            if (SelectedObject == target)
            {
                return;
            }

            Deselect();
            SelectedObject = target;
        }
    }
}
