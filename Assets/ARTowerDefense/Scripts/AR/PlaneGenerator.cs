using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

namespace ARTowerDefense.AR
{
    public class PlaneGenerator : MonoBehaviour
    {

        public GameObject PlaneVisualizerPrefab;

        /// <summary>
        /// Will be filled up with all the planes ever instantiated
        /// </summary>
        private List<PlaneVisualizer> m_PlaneVisualizers;

        /// <summary>
        /// Indicates whether the planes should be shown or not.
        /// </summary>
        private bool m_PlanesVisible;

        void Start()
        {
            m_PlaneVisualizers = new List<PlaneVisualizer>();
            m_PlanesVisible = true;
        }

        void Update()
        {
            // Check that ARCore session status is 'Tracking'
            if (Session.Status != SessionStatus.Tracking)
            {
                return;
            }

            List<DetectedPlane> newDetectedPlanes = new List<DetectedPlane>();

            // Populate m_NewDetectedPlanes with planes detected in the current frame
            Session.GetTrackables(newDetectedPlanes, TrackableQueryFilter.New);

            // Create a new plane for each new DetectedPlane
            foreach (DetectedPlane detectedPlane in newDetectedPlanes)
            {
                // Create a clone of the PlaneVisualizerPrefab
                GameObject plane = Instantiate(PlaneVisualizerPrefab, Vector3.zero, Quaternion.identity, transform);

                // Set the position of the plane and modify the vertices of the attached mesh
                var planeVisualizer = plane.GetComponent<PlaneVisualizer>();
                planeVisualizer.Initialize(detectedPlane);
                _SetPlaneVisibility(planeVisualizer);
                m_PlaneVisualizers.Add(planeVisualizer);
            }

            _ClearDestroyedPlaneVisualizers();
        }

        /// <summary>
        /// Removes references to plane visualizers that have been destroyed.
        /// </summary>
        private void _ClearDestroyedPlaneVisualizers()
        {
            m_PlaneVisualizers.RemoveAll(x => x == null);
        }

        /// <summary>
        /// Sets the planes visibility to false and hides all planes.
        /// </summary>
        public void HidePlanes()
        {
            m_PlanesVisible = false;
            _SetPlaneVisibility();
        }

        /// <summary>
        /// Sets the planes visibility to true and shows all planes.
        /// </summary>
        public void ShowPlanes()
        {
            m_PlanesVisible = true;
            _SetPlaneVisibility();
        }

        /// <summary>
        /// Shows/Hides all planes in m_Planes.
        /// </summary>
        private void _SetPlaneVisibility()
        {
            foreach (var planeVisualizer in m_PlaneVisualizers)
            {
                _SetPlaneVisibility(planeVisualizer);
            }
        }

        /// <summary>
        /// Shows/hides the given plane.
        /// </summary>
        /// <param name="planeVisualizer"></param>
        private void _SetPlaneVisibility(PlaneVisualizer planeVisualizer)
        {
            planeVisualizer.SetMeshRendererActive(m_PlanesVisible);
        }
    }

    public class CopyOfPlaneGenerator : MonoBehaviour
    {

        public GameObject PlaneVisualizerPrefab;

        /// <summary>
        /// Will be filled up with all the planes ever instantiated
        /// </summary>
        private List<PlaneVisualizer> m_PlaneVisualizers;

        /// <summary>
        /// Indicates whether the planes should be shown or not.
        /// </summary>
        private bool m_PlanesVisible;

        void Start()
        {
            m_PlaneVisualizers = new List<PlaneVisualizer>();
            m_PlanesVisible = true;
        }

        void Update()
        {
            // Check that ARCore session status is 'Tracking'
            if (Session.Status != SessionStatus.Tracking)
            {
                return;
            }

            List<DetectedPlane> newDetectedPlanes = new List<DetectedPlane>();

            // Populate m_NewDetectedPlanes with planes detected in the current frame
            Session.GetTrackables(newDetectedPlanes, TrackableQueryFilter.New);

            // Create a new plane for each new DetectedPlane
            foreach (DetectedPlane detectedPlane in newDetectedPlanes)
            {
                // Create a clone of the PlaneVisualizerPrefab
                GameObject plane = Instantiate(PlaneVisualizerPrefab, Vector3.zero, Quaternion.identity, transform);

                // Set the position of the plane and modify the vertices of the attached mesh
                var planeVisualizer = plane.GetComponent<PlaneVisualizer>();
                planeVisualizer.Initialize(detectedPlane);
                _SetPlaneVisibility(planeVisualizer);
                m_PlaneVisualizers.Add(planeVisualizer);
            }

            _ClearDestroyedPlaneVisualizers();
        }

        /// <summary>
        /// Removes references to plane visualizers that have been destroyed.
        /// </summary>
        private void _ClearDestroyedPlaneVisualizers()
        {
            m_PlaneVisualizers.RemoveAll(x => x == null);
        }

        /// <summary>
        /// Sets the planes visibility to false and hides all planes.
        /// </summary>
        public void HidePlanes()
        {
            m_PlanesVisible = false;
            _SetPlaneVisibility();
        }

        /// <summary>
        /// Sets the planes visibility to true and shows all planes.
        /// </summary>
        public void ShowPlanes()
        {
            m_PlanesVisible = true;
            _SetPlaneVisibility();
        }

        /// <summary>
        /// Shows/Hides all planes in m_Planes.
        /// </summary>
        private void _SetPlaneVisibility()
        {
            foreach (var planeVisualizer in m_PlaneVisualizers)
            {
                _SetPlaneVisibility(planeVisualizer);
            }
        }

        /// <summary>
        /// Shows/hides the given plane.
        /// </summary>
        /// <param name="planeVisualizer"></param>
        private void _SetPlaneVisibility(PlaneVisualizer planeVisualizer)
        {
            planeVisualizer.SetMeshRendererActive(m_PlanesVisible);
        }
    }
}