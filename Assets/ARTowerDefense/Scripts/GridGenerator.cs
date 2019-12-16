using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{

    public GameObject GridPrefab;

    // Will be filled up with planes detected in the current frame
    private List<DetectedPlane> m_NewDetectedPlanes = new List<DetectedPlane>();



    // Update is called once per frame
    void Update()
    {
        // Check that ARCore session status is 'Tracking'
        if (Session.Status != SessionStatus.Tracking)
        {
            return;
        }

        // Populate m_NewDetectedPlanes with planes detected in the current frame
        Session.GetTrackables(m_NewDetectedPlanes, TrackableQueryFilter.New);

        // Create a new grid for each DetectedPlane in m_NewDetectedPlanes
        foreach (DetectedPlane detectedPlane in m_NewDetectedPlanes)
        {
            // Create a clone of the GridPrefab
            GameObject grid = Instantiate(GridPrefab, Vector3.zero, Quaternion.identity, transform);

            // Set the position of the grid and modify the vertices of the attached mesh
            grid.GetComponent<GridVisualizer>().Initialize(detectedPlane);
        }
    }
}
