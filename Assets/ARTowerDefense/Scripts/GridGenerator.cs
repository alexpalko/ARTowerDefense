using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{

    public GameObject GridPrefab;

    /// <summary>
    /// Will be filled up with planes detected in the current frame
    /// </summary>
    private List<DetectedPlane> m_NewDetectedPlanes = new List<DetectedPlane>();

    /// <summary>
    /// Will be filled up with all the grids ever instantiated
    /// </summary>
    private List<GridVisualizer> m_GridVisualizers;

    /// <summary>
    /// Indicates whether the grids should be shown or not.
    /// </summary>
    private bool m_GridsVisible;

    void Start()
    {
        m_GridVisualizers = new List<GridVisualizer>();
        m_GridsVisible = true;
    }

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
            var gridVisualizer = grid.GetComponent<GridVisualizer>();
            gridVisualizer.Initialize(detectedPlane);
            gridVisualizer.SetMeshRendererActive(m_GridsVisible);
            m_GridVisualizers.Add(gridVisualizer);
        }

        _ClearDestroyedGridVisualizers();
    }

    /// <summary>
    /// Removes references to grid visualizers that have been destroyed.
    /// </summary>
    private void _ClearDestroyedGridVisualizers()
    {
        m_GridVisualizers.RemoveAll(x => x == null);
    }

    /// <summary>
    /// Sets the grids visibility to false and hides all grids.
    /// </summary>
    public void HideGrids()
    {
        m_GridsVisible = false;
        _SetGridsVisibility();
    }

    /// <summary>
    /// Sets the grids visibility to true and shows all grids.
    /// </summary>
    public void ShowGrids()
    {
        m_GridsVisible = true;
        _SetGridsVisibility();
    }

    /// <summary>
    /// Shows/Hides all grids in m_Grids.
    /// </summary>
    private void _SetGridsVisibility()
    {
        foreach (var gridVisualizer in m_GridVisualizers)
        {
            _SetGridVisibility(gridVisualizer);
        }
    }

    /// <summary>
    /// Shows/hides the given grid.
    /// </summary>
    /// <param name="gridVisualizer"></param>
    private void _SetGridVisibility(GridVisualizer gridVisualizer)
    {
        gridVisualizer.SetMeshRendererActive(m_GridsVisible);
    }
}
