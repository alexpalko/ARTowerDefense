using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using Assets.ARTowerDefense.Scripts;
using UnityEngine;

public class GameInitManager : MonoBehaviour
{
    public GameObject BindingWallPrefab;
    public GameObject BindingTowerPrefab;
    public GameObject GamePlanePrefab;
    public GameObject DivisionPrefab;

    /// <summary>
    /// The minimum allowed distance between boundaries not to require consolidation
    /// </summary>
    private readonly float m_BoundaryConsolidationThreshold = .2f;

    [SerializeField] private Master Master;

    public Vector3[] BindingVectors { get; private set; }
    public  GameObject[] BindingWalls { get; private set; }
    public  GameObject[] BindingTowers { get; private set; }
    public GameObject GamePlane { get; private set; }
    public Dictionary<Division, BuildingDivision> DivisionsDictionary { get; set; }

    void OnEnable()
    {
        List<Vector3> bindingVectorsList = Master.BindingVectors.ToList();
        _ConsolidateBoundaries(bindingVectorsList);
        BindingVectors = bindingVectorsList.ToArray();
        _SpawnBoundaries();
        _SpawnGamePlane();
        _SplitPlane();
    }

    private void _ConsolidateBoundaries(List<Vector3> vectors)
    {
        for (int i = 0; i < vectors.Count; i++)
        {
            var closeByVectorsIndexes = new List<int>();
            for (int j = i + 1; j < vectors.Count; j++)
            {
                if (Vector3.Distance(vectors[i], vectors[j]) < m_BoundaryConsolidationThreshold)
                {
                    closeByVectorsIndexes.Add(j);
                }
            }

            if (closeByVectorsIndexes.Count != 0)
            {
                var newVector = vectors[i];
                foreach (int index in closeByVectorsIndexes)
                {
                    newVector += vectors[index];
                }

                newVector /= closeByVectorsIndexes.Count + 1;

                vectors.Remove(vectors[i]);
                vectors.Insert(i, newVector);
                vectors.RemoveAll(vect =>
                    closeByVectorsIndexes.Select(idx => vectors[idx]).Contains(vect));
                _ConsolidateBoundaries(vectors);
                return;
            }
        }
    }

    private void _SpawnBoundaries()
    {
        BindingTowers = BindingVectors
            .Select(v => Instantiate(BindingTowerPrefab, v, Quaternion.identity, Master.AnchorTransform)).ToArray();

        BindingWalls = new GameObject[BindingVectors.Length];
        for (int i = 0; i < BindingVectors.Length; i++)
        {
            BindingWalls[i] = Instantiate(BindingWallPrefab,
                Vector3.Lerp(BindingVectors[i], BindingVectors[(i + 1) % BindingVectors.Length], 0.5f),
                Quaternion.identity, Master.AnchorTransform);
            BindingWalls[i].transform.localScale += new Vector3(
                Vector3.Distance(BindingVectors[i], BindingVectors[(i + 1) % BindingVectors.Length]), 0, 0);
        }

        for (int i = 0; i < BindingVectors.Length; i++)
        {
            Vector3 point = BindingVectors[i];
            Vector3 midPoint = BindingWalls[i].transform.position;

            Vector3 pointProjectionOntoX = Vector3.Project(point, Vector3.right);
            Vector3 midPointProjectionOntoX = Vector3.Project(midPoint, Vector3.right);
            Vector3 pointProjectionOntoZ = Vector3.Project(point, Vector3.forward);
            Vector3 midPointProjectionOntoZ = Vector3.Project(midPoint, Vector3.forward);

            float cath1 = Vector3.Distance(pointProjectionOntoX, midPointProjectionOntoX);
            float cath2 = Vector3.Distance(pointProjectionOntoZ, midPointProjectionOntoZ);
            float angle = Mathf.Atan2(cath2, cath1) * Mathf.Rad2Deg;

            BindingWalls[i].transform.RotateAround(BindingWalls[i].transform.position, Vector3.up, angle);
            Collider fieldCollider = BindingWalls[i].GetComponent<Collider>();
            if (ColliderContainsPoint(fieldCollider.transform, point)) continue;
            BindingWalls[i].transform.RotateAround(BindingWalls[i].transform.position, Vector3.up, -angle * 2);
        }
    }

    private bool ColliderContainsPoint(Transform colliderTransform, Vector3 point)
    {
        Vector3 localPos = colliderTransform.InverseTransformPoint(point);
        return Mathf.Abs(localPos.x) < 0.5f && Mathf.Abs(localPos.y) < 0.5f && Mathf.Abs(localPos.z) < 0.5f;
    }

    private void _SpawnGamePlane()
    {

        float maxX = BindingVectors.Select(v => v.x).Max();
        float minX = BindingVectors.Select(v => v.x).Min();
        float maxZ = BindingVectors.Select(v => v.z).Max();
        float minZ = BindingVectors.Select(v => v.z).Min();

        float distanceX = maxX - minX;
        float distanceZ = maxZ - minZ;
        float maxDistance = distanceX > distanceZ ? distanceX : distanceZ;

        float middleX = (maxX + minX) / 2;
        float middleZ = (maxZ + minZ) / 2;

        GamePlane = Instantiate(GamePlanePrefab, new Vector3(middleX, BindingVectors[0].y, middleZ),
            Quaternion.identity, Master.AnchorTransform);
        GamePlane.transform.localScale = new Vector3(maxDistance, maxDistance, 1);
        GamePlane.transform.Rotate(90, 0, 0);
        Debug.Log(GamePlane.transform.localScale);
    }

    private void _SplitPlane()
    {
        Renderer rend = GamePlane.GetComponent<Renderer>();
        Debug.Log("Acquired game plane renderer");

        float y = rend.bounds.min.y;

        List<Division> divisions = new List<Division>();

        for (var x = rend.bounds.min.x; x < rend.bounds.max.x; x += Master.k_DivisionLength)
        {
            for (var z = rend.bounds.min.z; z < rend.bounds.max.z; z += Master.k_DivisionLength)
            {
                divisions.Add(new Division(new Vector3(x, y, z),
                    new Vector3(x + Master.k_DivisionLength, y, z + Master.k_DivisionLength)));
            }
        }

        _TrimDivisions(divisions);

        DivisionsDictionary = new Dictionary<Division, BuildingDivision>(divisions.Count);
        Debug.Log($"Will spawn {divisions.Count} divisions.");
        foreach (var division in divisions)
        {
            var divisionObject =
                Instantiate(DivisionPrefab, division.Center, Quaternion.identity, Master.AnchorTransform);
            Debug.Log("Spawned division marker.");
            DivisionsDictionary.Add(division, divisionObject.GetComponent<BuildingDivision>());
        }

    }

    private void _TrimDivisions(List<Division> divisions)
    {
        Debug.Log("Started trim division.");
        Debug.Log($"Original divisions count: {divisions.Count}");

        divisions.RemoveAll(div =>
        {
            var polyPoints = BindingVectors.Select(x => new Vector2(x.x, x.z)).ToArray();

            return !(Poly.ContainsPoint(polyPoints, new Vector2(div.Point1.x, div.Point1.z)) &&
                     Poly.ContainsPoint(polyPoints, new Vector2(div.Point2.x, div.Point2.z)) &&
                     Poly.ContainsPoint(polyPoints, new Vector2(div.Point1.x, div.Point2.z)) &&
                     Poly.ContainsPoint(polyPoints, new Vector2(div.Point2.x, div.Point1.z)));
        });

        Debug.Log($"Division count after trimming: {divisions.Count}");
    }
}
