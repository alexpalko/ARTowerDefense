using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using UnityEngine;

public class GameInitManager : MonoBehaviour
{
    public GameObject BindingWallPrefab;
    public GameObject BindingTowerPrefab;
    public GameObject GamePlanePrefab;

    [SerializeField] private Master Master;
    [SerializeField] private GameObject GameInitializationPanel;

    public Vector3[] BindingVectors { get; private set; }
    public  GameObject[] BindingWalls { get; private set; }
    public  GameObject[] BindingTowers { get; private set; }
    public GameObject GamePlane { get; private set; }

    void OnEnable()
    {
        GameInitializationPanel.SetActive(true);
        List<Vector3> bindingVectorsList = Master.BindingVectors.ToList();
        _ConsolidateBoundaries(bindingVectorsList);
        BindingVectors = bindingVectorsList.ToArray();
        _SpawnBoundaries();
        _SpawnGamePlane();
    }

    void OnDisable()
    {
        //GameInitializationPanel.SetActive(false);
    }
    private void _ConsolidateBoundaries(List<Vector3> vectors)
    {
        for (int i = 0; i < vectors.Count; i++)
        {
            var closeByVectorsIndexes = new List<int>();
            for (int j = i + 1; j < vectors.Count; j++)
            {
                if (Vector3.Distance(vectors[i], vectors[j]) < .5f)
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
