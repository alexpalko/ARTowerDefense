using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using GoogleARCore;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Input = GoogleARCore.InstantPreviewInput;
#endif

namespace Assets.ARTowerDefense.Scripts
{
    class GridDetectionManager : MonoBehaviour
    {
        [SerializeField] private Camera FirstPersonCamera;
        [SerializeField] private Master Master;
        [SerializeField] private GameObject HelperMessage;
        [SerializeField] private GameObject ConfirmButton;
        [SerializeField] private GameObject HelperAnimation;
        [SerializeField] private GameObject GridDetectionPanel;

        [SerializeField] private GameObject MarkerPrefab;

        private GameObject m_PlaneSelectionMarker;
        public DetectedPlane MarkedPlane;
        
        public Transform AnchorTransform { get; private set; }

        private void Update()
        {
            List<DetectedPlane> detectedPlanes = new List<DetectedPlane>();
            Session.GetTrackables(detectedPlanes);
            if (detectedPlanes.Any())
            {
                HelperAnimation.SetActive(false);
                HelperMessage.GetComponent<TextMeshProUGUI>().text =
                    "Once you are happy with one of the detected planes, touch it to place a marker and hit CONFIRM.";
            }

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
                                Instantiate(MarkerPrefab, hit.Pose.position, hit.Pose.rotation);
                            MarkedPlane = plane;
                            AnchorTransform = anchor.transform;
                            Debug.Log("New base marker placed");
                            ConfirmButton.SetActive(true);
                            Debug.Log("ConfirmButton activated");
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            GridDetectionPanel.SetActive(true);
        }

        private void OnDisable()
        {
            //Master.AnchorTransform = AnchorTransform;
            //Master.MarkedPlane = MarkedPlane;
            Destroy(m_PlaneSelectionMarker);
            GridDetectionPanel.SetActive(false);
            ConfirmButton.SetActive(false);
        }

    }
}
