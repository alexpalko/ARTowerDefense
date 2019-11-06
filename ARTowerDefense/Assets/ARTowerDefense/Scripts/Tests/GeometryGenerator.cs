using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeometryGenerator : MonoBehaviour
{
    private List<Vector3> m_BindingVectors = new List<Vector3>
    {
        new Vector3(0f, 0f, 10f),
        new Vector3(10f, 0f, 15f),
        new Vector3(20f, 0f, 20f),
        new Vector3(25, 0f, 0f),
        new Vector3(30f, 0f, -20f),
        new Vector3(0f, 0f, -30f),
        new Vector3(-10f, 0f, -20f),
        new Vector3(-14f, 0f, -14f),
        new Vector3(-10f, 0f, 0f),
        new Vector3(-30f, 0f, 0f),
    };

    public GameObject BindingPlanePrefab;
    public GameObject Marker;
    public GameObject GamePlanePrefab;

    private Mesh m_mesh;
    
    // Update is called once per frame
    void Start()
    {
        _GeneratePlane(m_BindingVectors);   
        _GenerateBoundaries();
    }

    private void _GenerateBoundaries()
    {
        List<GameObject> markers = new List<GameObject>();

        foreach (Vector3 bindingVector in m_BindingVectors)
        {
            var marker = Instantiate(Marker, bindingVector, Quaternion.identity);
            markers.Add(marker);
        }

        List<GameObject> fields = new List<GameObject>();
        GameObject field;

        for (int i = 0; i < m_BindingVectors.Count - 1; i++)
        {
            field = Instantiate(BindingPlanePrefab, Vector3.Lerp(m_BindingVectors[i], m_BindingVectors[i + 1], 0.5f), Quaternion.identity);
            //field.transform.Rotate(0, Vector3.Angle(m_BindingVectors[i], m_BindingVectors[i + 1]), 0);
            field.transform.localScale +=
                new Vector3(Vector3.Distance(m_BindingVectors[i], m_BindingVectors[i + 1]), 0, 0);
            fields.Add(field);
        }
        field = Instantiate(BindingPlanePrefab, Vector3.Lerp(m_BindingVectors.First(), m_BindingVectors.Last(), 0.5f), Quaternion.identity);
        //field.transform.Rotate(0, Vector3.Angle(m_BindingVectors[i], m_BindingVectors[i + 1]), 0);
        field.transform.localScale +=
            new Vector3(Vector3.Distance(m_BindingVectors.First(), m_BindingVectors.Last()), 0, 0);
        fields.Add(field);


        for (int i = 0; i < m_BindingVectors.Count; i++)
        {
            Vector3 point = m_BindingVectors[i];
            Vector3 midPoint = fields[i].transform.position;

            Vector3 pointProjectionOntoX = Vector3.Project(point, Vector3.right);
            Vector3 midPointProjectionOntoX = Vector3.Project(midPoint, Vector3.right);
            Vector3 pointProjectionOntoZ = Vector3.Project(point, Vector3.forward);
            Vector3 midPointProjectionOntoZ = Vector3.Project(midPoint, Vector3.forward);

            var cath1 = Vector3.Distance(pointProjectionOntoX, midPointProjectionOntoX);
            var cath2 = Vector3.Distance(pointProjectionOntoZ, midPointProjectionOntoZ);
            var angle = Mathf.Atan2(cath2, cath1) * Mathf.Rad2Deg;

            fields[i].transform.RotateAround(fields[i].transform.position, Vector3.up, angle);
            Collider fieldCollider = fields[i].GetComponent<Collider>();
            if (_ColliderContainsPoint(fieldCollider.transform, point)) continue;
            fields[i].transform.RotateAround(fields[i].transform.position, Vector3.up, -angle * 2);
        }
    }
    
    private bool _ColliderContainsPoint(Transform colliderTransform, Vector3 point)
    {
        Vector3 localPos = colliderTransform.InverseTransformPoint(point);
        return Mathf.Abs(localPos.x) < 0.5f && Mathf.Abs(localPos.y) < 0.5f && Mathf.Abs(localPos.z) < 0.5f;
    }


    private void _GeneratePlane(List<Vector3> points)
    {
        var plane = Instantiate(GamePlanePrefab, Vector3.zero, Quaternion.identity);

        Mesh mesh = new Mesh();
        plane.GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = points.ToArray();
        List<int> indexesOrder = new List<int>();
        int pointsCount = points.Count;

        for (int inc = 2; ; inc *= 2)
        {
            if (pointsCount <= inc) break;

            for (int i = inc / 2; i < points.Count; i += inc)
            {
                indexesOrder.Add(i - inc / 2);
                indexesOrder.Add(i);
                indexesOrder.Add((i + inc / 2) % points.Count);
            }
        }

        mesh.triangles = indexesOrder.ToArray();

       // GetComponent<MeshFilter>().mesh = mesh;
    }
}
