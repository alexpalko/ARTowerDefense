using System.Collections.Generic;
using GoogleARCore;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    private DetectedPlane m_DetectedPlane;

    // Keep previous frame's mesh polygon to avoid mesh update every frame.
    private List<Vector3> m_PreviousFrameMeshVertices = new List<Vector3>();
    private List<Vector3> m_MeshVertices = new List<Vector3>();
    private Vector3 m_PlaceCenter;

    private List<Color> m_MeshColors = new List<Color>();

    private List<int> m_MeshIndices = new List<int>();

    private Mesh m_Mesh;

    private MeshRenderer m_MeshRenderer;

    public void Awake()
    {
        m_Mesh = GetComponent<MeshFilter>().mesh;
        m_MeshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_DetectedPlane == null)
        {
            return;
        }

        // If the detected plane is already encompassed by another plane, then the detected plane should be ignored.
        if (m_DetectedPlane.SubsumedBy != null)
        {
            Destroy(gameObject);
            return;
        }

        if (m_DetectedPlane.TrackingState != TrackingState.Tracking)
        {
            m_MeshRenderer.enabled = false;
        }

        //m_MeshRenderer.enabled = true;

        _UpdateMeshIfNeeded();
    }

    /// <summary>
    /// Initializes the DetectedPlaneVisualizer with a DetectedPlane.
    /// </summary>
    /// <param name="plane">The plane to visualize.</param>
    public void Initialize(DetectedPlane plane)
    {
        m_DetectedPlane = plane;
        m_MeshRenderer.material.SetColor("_GridColor", Color.white);
        m_MeshRenderer.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));

        Update();
    }

    /// <summary>
    /// Update the mesh with a list of Vector3 and plane's center position.
    /// </summary>
    private void _UpdateMeshIfNeeded()
    {
        m_DetectedPlane.GetBoundaryPolygon(m_MeshVertices);

        if (_AreVerticesListsEqual(m_PreviousFrameMeshVertices, m_MeshVertices))
        {
            return;
        }

        m_PreviousFrameMeshVertices.Clear();
        m_PreviousFrameMeshVertices.AddRange(m_MeshVertices);

        m_PlaceCenter = m_DetectedPlane.CenterPose.position;

        Vector3 planeNormal = m_DetectedPlane.CenterPose.rotation * Vector3.up;

        m_MeshRenderer.material.SetVector("_PlaneNormal", planeNormal);

        int planePolygonCount = m_MeshVertices.Count;

        // The following code converts a polygon to a mesh with two polygons, inner polygon
        // renders with 100% opacity and fade out to outter polygon with opacity 0%, as shown
        // below.  The indices shown in the diagram are used in comments below.
        // _______________     0_______________1
        // |             |      |4___________5|
        // |             |      | |         | |
        // |             | =>   | |         | |
        // |             |      | |         | |
        // |             |      |7-----------6|
        // ---------------     3---------------2
        m_MeshColors.Clear();

        // Make vertices 0 to 3 transparent
        for (int i = 0; i < planePolygonCount; i++)
        {
            m_MeshColors.Add(Color.clear);
        }

        // Feather distance 0.2m.
        const float featherLength = 0.2f;

        // Feather scale over the distance between plane and vertices.
        const float featherScale = 0.2f;

        for (int i = 0; i < planePolygonCount; i++)
        {
            Vector3 v = m_MeshVertices[i];

            // Vector from plane center to current point
            Vector3 d = v - m_PlaceCenter;

            float scale = 1.0f - Mathf.Min(featherLength / d.magnitude, featherScale);
            m_MeshVertices.Add((scale * d) + m_PlaceCenter);

            m_MeshColors.Add(Color.white);
        }

        m_MeshIndices.Clear();
        int firstOuterVertex = 0;
        int firstInnerVertex = planePolygonCount;

        // Generate triangle (4, 5, 6) and (4, 6, 7).
        for (int i = 0; i < planePolygonCount - 2; ++i)
        {
            m_MeshIndices.Add(firstInnerVertex);
            m_MeshIndices.Add(firstInnerVertex + i + 1);
            m_MeshIndices.Add(firstInnerVertex + i + 2);
        }

        // Generate triangle (0, 1, 4), (4, 1, 5), (5, 1, 2), (5, 2, 6), (6, 2, 3), (6, 3, 7)
        // (7, 3, 0), (7, 0, 4)
        for (int i = 0; i < planePolygonCount; ++i)
        {
            int outerVertex1 = firstOuterVertex + i;
            int outerVertex2 = firstOuterVertex + ((i + 1) % planePolygonCount);
            int innerVertex1 = firstInnerVertex + i;
            int innerVertex2 = firstInnerVertex + ((i + 1) % planePolygonCount);

            m_MeshIndices.Add(outerVertex1);
            m_MeshIndices.Add(outerVertex2);
            m_MeshIndices.Add(innerVertex1);

            m_MeshIndices.Add(innerVertex1);
            m_MeshIndices.Add(outerVertex2);
            m_MeshIndices.Add(innerVertex2);
        }

        m_Mesh.Clear();
        m_Mesh.SetVertices(m_MeshVertices);
        m_Mesh.SetTriangles(m_MeshIndices, 0);
        m_Mesh.SetColors(m_MeshColors);
    }

    private bool _AreVerticesListsEqual(List<Vector3> firstList, List<Vector3> secondList)
    {
        if (firstList.Count != secondList.Count)
        {
            return false;
        }

        for (int i = 0; i < firstList.Count; i++)
        {
            if (firstList[i] != secondList[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Shows/hides the grid's mesh.
    /// </summary>
    /// <param name="active"></param>
    public void SetMeshRendererActive(bool active)
    {
        m_MeshRenderer.enabled = active;
    }
}
