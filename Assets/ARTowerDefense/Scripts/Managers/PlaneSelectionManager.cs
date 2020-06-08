using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using GoogleARCore;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Input = GoogleARCore.InstantPreviewInput;
#endif

namespace ARTowerDefense.Managers
{
    class PlaneSelectionManager : MonoBehaviour
    {
        [SerializeField] private Camera FirstPersonCamera;
        [SerializeField] private Master Master;
        [SerializeField] private GameObject HelperMessage;
        [SerializeField] private GameObject ConfirmButton;
        [SerializeField] private GameObject HelperAnimation;

        [SerializeField] private GameObject MarkerPrefab;

        private GameObject m_PlaneSelectionMarker;
        //public DetectedPlane MarkedPlane;

        private const TrackableHitFlags k_RaycastFilter = TrackableHitFlags.PlaneWithinPolygon;

        public Transform AnchorTransform { get; private set; }
        public DetectedPlane MarkedPlane { get; private set; }

        private void Update()
        {
            var detectedPlanes = new List<DetectedPlane>();
            Session.GetTrackables(detectedPlanes);
            _UpdateUIGuides(detectedPlanes);

            Touch touch;

            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            if (!Frame.Raycast(touch.position.x, touch.position.y, k_RaycastFilter, out var hit)) return;
            Debug.Log("Plane intersection found");

            // Check that the hit trackable is a plane
            if (!(hit.Trackable is DetectedPlane detectedPlane))
            {
                Debug.LogError($"Hit a trackable of type {hit.Trackable.GetType().Name}");
                return;
            }

            // Check that the hit did not happen at the back of the plane
            if (Vector3.Dot(
                     FirstPersonCamera.transform.position - hit.Pose.position,
                    hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.LogError("Hit at back of current DetectedPlane");
                return;
            }

            Debug.Log("The raycast hit a plane");

            if (m_PlaneSelectionMarker != null)
            {
                Debug.Log("Old marker was removed.");
                Destroy(m_PlaneSelectionMarker);
            }

            if (AnchorTransform != null)
            {
                Debug.Log("Old plane anchor was destroyed.");
                Destroy(AnchorTransform.gameObject);
            }


            Anchor anchor = detectedPlane.CreateAnchor(detectedPlane.CenterPose);
            m_PlaneSelectionMarker =
                Instantiate(MarkerPrefab, hit.Pose.position, hit.Pose.rotation);
            MarkedPlane = detectedPlane;
            AnchorTransform = anchor.transform;
            Debug.Log("A new plane was selected and an anchor was created.");
            ConfirmButton.SetActive(true);
            Debug.Log("ConfirmButton was activated");
        
        }

        private void OnDisable()
        {
            Destroy(m_PlaneSelectionMarker);
            ConfirmButton.SetActive(false);
        }

        /// <summary>
        /// Updates the status of the helper animation and the messages displayed to help the user
        /// </summary>
        /// <param name="detectedPlanes"></param>
        private void _UpdateUIGuides(List<DetectedPlane> detectedPlanes)
        {
            if (detectedPlanes.Any())
            {
                HelperAnimation.SetActive(false);
                HelperMessage.GetComponent<TextMeshProUGUI>().text =
                    "Once you are happy with one of the detected planes, " +
                    "touch it to place a marker and hit CONFIRM.";
            }
            else
            {
                HelperAnimation.SetActive(true);
                HelperMessage.GetComponent<TextMeshProUGUI>().text =
                    "Make sure your surrounding is well lit and the surface has a distinguishable texture. " +
                    "Slowly move your device around to help the app detect flat surfaces.";
            }
        }


    }
}
