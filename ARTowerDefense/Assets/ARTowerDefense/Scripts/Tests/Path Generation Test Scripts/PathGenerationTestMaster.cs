using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ARTowerDefense.Scripts;
using UnityEngine;
using UnityEngine.Experimental.XR;
using Random = System.Random;

public class PathGenerationTestMaster : MonoBehaviour
{
    private List<Vector3> m_BindingVectors = new List<Vector3>
    {
        //new Vector3(0f, 0f, 5f),
        //new Vector3(5f, 0f, 7.5f),
        //new Vector3(10f, 0f, 10f),
        //new Vector3(12.5f, 0f, 0f),
        //new Vector3(15f, 0f, -10f),
        //new Vector3(0f, 0f, -15f),
        //new Vector3(-5f, 0f, -10f),
        //new Vector3(-7f, 0f, -7f),
        //new Vector3(-5f, 0f, 0f),

        //new Vector3(5f, 0f, 5f),
        //new Vector3(5f, 0f, -5f),
        //new Vector3(-5f, 0f, -5f),
        //new Vector3(-5f, 0f, 5f),

        //new Vector3(5f, 0f, 5f),
        //new Vector3(6f, 0f, -6f),
        //new Vector3(-3f, 0f, -3f),
        //new Vector3(-5f, 0f, 5f),

        //new Vector3(-2f, 0f, -4f),
        //new Vector3(6f, 0f, 0f),
        //new Vector3(4f, 0f, 4f),
        //new Vector3(1f, 0f, 5f),
        //new Vector3(-1f, 0f, 4f),
        //new Vector3(-2f, 0f, 2f),
        //new Vector3(-2f, 0f, 0f),
        //new Vector3(-2f, 0f, -0.01f),
        //new Vector3(-2f, 0f, -0.03f),


        new Vector3(42f, 0f, 5f),
        new Vector3(-10f, 0f, -20f),
        new Vector3(-10f, 0f, 0f),
        new Vector3(-10f, 0f, 10f),
        new Vector3(-5f, 0f, 20f),
        new Vector3(10f, 0f, 25f),
        new Vector3(39f, 0f, 20f),
    };

    public GameObject BindingPlanePrefab;
    public GameObject Marker;
    public GameObject GamePlanePrefab;
    public GameObject TestPolePrefab;
    public GameObject BasePrefab;
    public GameObject SpawnerPrefab;
    public GameObject PathMarkerPrefab;
    public GameObject PathPrefab;
    public GameObject DivisionMarkerPrefab;

    private GameObject m_GamePlane;
    private GameObject m_Base;
    private GameObject m_Spawner;
    private List<GameObject> m_PathNodes;

    private List<Division> m_Divisions;
    private List<Division> m_PathNodeDivisions;
    private List<Division> m_AvailableDivisions;


    private enum GameState
    {
        SPLITTING_PLANE, PLACING_BASE, PLACING_SPAWN, PLACING_PATH_NODES, GENERATING_PATH, END,
    }

    private GameState state;

    // Update is called once per frame
    void Start()
    {
        _ConsolidateBoundaries();
        _GeneratePlane(m_BindingVectors);
        _GenerateBoundaries();
        state = GameState.SPLITTING_PLANE;
    }

    //TODO: don't forget to check if available divisions, after base and spawn placement, allow at least 2 path nodes to be placed,
    //TODO: Before pave placement, remove 18 available divisions and check above
    void Update()
    {
        switch (state)
        {
            case GameState.SPLITTING_PLANE:
                m_BindingVectors = m_BindingVectors.ToList();
                _SplitPlane();
                break;
            case GameState.PLACING_BASE:
                _PlaceBase();
                break;
            case GameState.PLACING_SPAWN:
                _PlaceSpawn();
                break;
            case GameState.PLACING_PATH_NODES:
                //_PlacePathNodes();
                state = GameState.GENERATING_PATH;
                break;
            case GameState.GENERATING_PATH:
                if (_GeneratePath() != null)
                {
                    _BuildPath();
                    state = GameState.END;
                }

                break;
            case GameState.END:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    private void _ConsolidateBoundaries()
    {
        for (int i = 0; i < m_BindingVectors.Count; i++)
        {
            var closeByVectorsIndexes = new List<int>();
            for (int j = i + 1; j < m_BindingVectors.Count; j++)
            {
                if (Vector3.Distance(m_BindingVectors[i], m_BindingVectors[j]) < .1f)
                {
                    closeByVectorsIndexes.Add(j);
                }
            }

            if (closeByVectorsIndexes.Count != 0)
            {
                var newVector = m_BindingVectors[i];
                foreach (int index in closeByVectorsIndexes)
                {
                    newVector += m_BindingVectors[index];
                }

                newVector /= closeByVectorsIndexes.Count + 1;

                m_BindingVectors.Remove(m_BindingVectors[i]);
                //m_BindingVectors[i] = newVector;
                m_BindingVectors.Insert(i, newVector);
                m_BindingVectors.RemoveAll(vect =>
                    closeByVectorsIndexes.Select(idx => m_BindingVectors[idx]).Contains(vect));
                _ConsolidateBoundaries();
                return;
            }
        }
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
            field.transform.localScale +=
                new Vector3(Vector3.Distance(m_BindingVectors[i], m_BindingVectors[i + 1]), 0, 0);
            fields.Add(field);
        }
        field = Instantiate(BindingPlanePrefab, Vector3.Lerp(m_BindingVectors.First(), m_BindingVectors.Last(), 0.5f), Quaternion.identity);
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

    private Vector3 GetBarrycenter()
    {
        Vector3 v3 = m_BindingVectors.Aggregate(Vector3.zero, (current, bindingVector) => current + bindingVector);
        v3 /= m_BindingVectors.Count;
        return v3;
    }

    private void _GeneratePlane(List<Vector3> points)
    {

        float maxX = m_BindingVectors.Select(v => v.x).Max();
        float minX = m_BindingVectors.Select(v => v.x).Min();
        float maxZ = m_BindingVectors.Select(v => v.z).Max();
        float minZ = m_BindingVectors.Select(v => v.z).Min();

        float distanceX = maxX - minX;
        float distanceZ = maxZ - minZ;

        float maxDistance = distanceX > distanceZ ? distanceX : distanceZ;

        float middleX = (maxX + minX) / 2;
        float middleZ = (maxZ + minZ) / 2;

        m_GamePlane = Instantiate(GamePlanePrefab, new Vector3(middleX, m_BindingVectors[0].y, middleZ), Quaternion.identity);
        
        m_GamePlane.transform.localScale = new Vector3(maxDistance, maxDistance, 1);
        m_GamePlane.transform.Rotate(90, 0, 0);
    }

    private const float m_DivisionLength = 1f;

    private void _SplitPlane()
    {
        Renderer rend = m_GamePlane.GetComponent<Renderer>();

        float y = rend.bounds.min.y;

        Vector3[] planeCorners = {
            rend.bounds.max,
            new Vector3(rend.bounds.max.x, y, rend.bounds.min.z),
            rend.bounds.min,
            new Vector3(rend.bounds.min.x, y, rend.bounds.max.z), 
        };

        foreach (Vector3 planeCorner in planeCorners)
        {
            Instantiate(TestPolePrefab, planeCorner, Quaternion.identity);
        }

        List<Division> divisions = new List<Division>();

        for (float x = rend.bounds.min.x; x < rend.bounds.max.x; x+= m_DivisionLength)
        {
            for (float z = rend.bounds.min.z; z < rend.bounds.max.z; z+= m_DivisionLength)
            {
                divisions.Add(new Division(new Vector3(x, y, z), new Vector3(x + m_DivisionLength, y, z + m_DivisionLength)));
            }
        }

        _TrimDivisions(divisions);

        //foreach (Division division in divisions)
        //{
        //    Instantiate(TestPolePrefab, division.Center, Quaternion.identity);
        //}

        m_Divisions = divisions;
        foreach (Division division in divisions)
        {
            Instantiate(DivisionMarkerPrefab, division.Center, Quaternion.identity);
        }
        availableDivisions = new HashSet<Division>(divisions);
        state = GameState.PLACING_BASE;
    }

    private void _TrimDivisions(List<Division> divisions)
    {
        divisions.RemoveAll(div =>
        {
            var raycastHits = Physics.RaycastAll(div.Point1, Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
                return true;
            }

            raycastHits = Physics.RaycastAll(div.Point2, Vector3.right, Mathf.Infinity);
            
            if (!_IsWithinBoundaries(raycastHits))
            {
                return true;
            }

            raycastHits = Physics.RaycastAll(new Vector3(div.Point1.x, div.Point1.y, div.Point2.z), Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
                return true;
            }

            raycastHits = Physics.RaycastAll(new Vector3(div.Point2.x, div.Point1.y, div.Point1.z), Vector3.right, Mathf.Infinity);

            if (!_IsWithinBoundaries(raycastHits))
            {
                return true;
            }

            return false;
        });
    }

    private bool _IsWithinBoundaries(RaycastHit[] hits)
    {
        if (hits.Length == 1) return true;
        if (hits.Length != 3) return false;
        return hits.Any(hit => hit.collider.tag.Equals("GameTower"));
    }

    private void _PlaceBase()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(
                Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0)),
                out var hitInfo))
            {
                var hitPoint = hitInfo.point;
                var baseDivision = m_Divisions.FirstOrDefault(div => div.Includes(hitPoint));

                if (baseDivision == null) return;

                m_Base = Instantiate(BasePrefab, baseDivision.Center, Quaternion.identity);

                Vector3 front = baseDivision.Center - m_Base.transform.forward;
                m_PathEnd = m_Divisions.FirstOrDefault(div => div.Includes(front));
                if (m_PathEnd == null)
                {
                    front = baseDivision.Center - m_Base.transform.forward * -1;
                    m_PathEnd = m_Divisions.FirstOrDefault(div => div.Includes(front));
                }

                m_AvailableDivisions = m_Divisions.Where(div => !_CheckProximity(div, baseDivision)).ToList();
                m_PathNodeDivisions = new List<Division> {baseDivision};
                state = GameState.PLACING_SPAWN;
            }
        }
    }

    private static bool _CheckProximity(Division division1, Division division2)
    {
        List<Vector3> division1Points = new List<Vector3>
        {
            division1.Point1, division1.Point2,
            new Vector3(division1.Point1.x, division1.Point2.y, division1.Point2.z),
            new Vector3(division1.Point2.x, division1.Point1.y, division1.Point1.z),
        };

        List<Vector3> division2Points = new List<Vector3>
        {
            division2.Point1, division2.Point2,
            new Vector3(division2.Point1.x, division2.Point1.y, division2.Point2.z),
            new Vector3(division2.Point2.x, division2.Point1.y, division2.Point1.z),
        };

        return division2Points.Any(div => division1Points.Contains(div));
    }

    private void _PlaceSpawn()
    {
        //Random random = new Random();
        //int index = random.Next(m_AvailableDivisions.Count);
        //Division division = m_AvailableDivisions[index];
        //m_Spawner = Instantiate(SpawnerPrefab, division.Center, Quaternion.identity);
        //Vector3 front = division.Center - m_Spawner.transform.forward;
        //m_PathStart = m_Divisions.First(div => div.Includes(front));

        //m_AvailableDivisions.RemoveAll(div => _CheckProximity(div, division));
        state = GameState.PLACING_PATH_NODES;
    }

    private void _PlacePathNodes()
    {
        Random random = new Random();
        m_PathNodes = new List<GameObject>();
        for (int i = 0; i < Mathf.Round(m_AvailableDivisions.Count * .1f); i++)
        {
            int index = random.Next(m_AvailableDivisions.Count);
            Division division = m_AvailableDivisions[index];
            m_PathNodeDivisions.Add(division);
            m_PathNodes.Add(Instantiate(PathMarkerPrefab, division.Center, Quaternion.identity));
            //m_AvailableDivisions.RemoveAll(div => _CheckProximity(div, division));
            m_AvailableDivisions.Remove(division);
        }

        //availableNodeDivisions = m_PathNodeDivisions.ToList();

        state = GameState.GENERATING_PATH;
    }

    private Vector3[] m_Moves = 
    {
        new Vector3(1, 0, 0),
        new Vector3(-1, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(0, 0, -1)
    };

    private Division m_PathStart;
    private Division m_PathEnd;

    private Stack<Division> m_UnavailableDivisions = new Stack<Division>();
    private Stack<Division> m_PathDivisions = new Stack<Division>();

    private bool _GenerateShortestPath(Division currentDivision, Division end)
    {
        if (currentDivision == null) return false;

        if (m_UnavailableDivisions.Contains(currentDivision)) return false;

        Division previousDivision = m_PathDivisions.Any() ? m_PathDivisions.Peek() : null;

        var moves = _GetNextMovePriority(currentDivision, end);

        int countToPop = 1;
        if (previousDivision != null)
        {
            foreach (Vector3 move in moves)
            {
                var neighborDivision = m_Divisions.FirstOrDefault(div => div.Includes(previousDivision.Center + move));
                if (neighborDivision != null)
                {
                    m_UnavailableDivisions.Push(neighborDivision);
                    countToPop++;
                }
            }
        }

        path.Push(currentDivision);
        m_UnavailableDivisions.Push(currentDivision);

        if (currentDivision == end)
        {
            return true;
        }

        foreach (Vector3 move in moves)
        {
            var nextDivision = m_Divisions.FirstOrDefault(div => div.Includes(currentDivision.Center + move));
            if (_GenerateShortestPath(nextDivision, end)) return true;
        }

        path.Pop();
        while (countToPop != 0)
        {
            m_UnavailableDivisions.Pop();
            countToPop--;
        }
        return false;
    }

    private HashSet<Division> availableNodeDivisions;
    private HashSet<Division> availableDivisions;
    private Stack<Division> path;

    private Division _GeneratePath()
    {
        availableNodeDivisions = new HashSet<Division>(m_PathNodeDivisions);
        availableDivisions = new HashSet<Division>(m_AvailableDivisions);
        path = new Stack<Division>();
        //path.Push(m_PathStart);
        return _GenerateRandomPath(m_PathEnd);
    }

    private float availableDivisionsThreshold = .3f;
    private float increaseThresholdCount = 1;

    private Division _GenerateRandomPath(Division currentDivision)
    {
        if (Math.Abs(increaseThresholdCount++ % 2000) < 1e-5)
        {
            availableDivisionsThreshold += .1f;
        }

        if (availableDivisions.Count < availableDivisionsThreshold * m_Divisions.Count && _CanPlaceSpawner(currentDivision))
        {
            path.Push(currentDivision);
            return currentDivision;
        }

        Division previousDivision = null;

        // Remove neighbors of previous path
        if (path.Any())
        {
            previousDivision = path.Peek();
        }

        List<Division> markedDivisions = new List<Division>();

        if (previousDivision != null)
        {
            foreach (Vector3 move in m_Moves)
            {
                var neighborCenter = move + previousDivision.Center;
                var neighborDivision = availableDivisions.FirstOrDefault(div => div.Includes(neighborCenter));
                if (neighborDivision != null)
                {
                    markedDivisions.Add(neighborDivision);
                }
            }
        }
        availableDivisions.RemoveWhere(div => markedDivisions.Contains(div));

        IEnumerable<Division> possibleNextDivisions = _RandomizeNextDivisions(currentDivision);
        path.Push(currentDivision);
        foreach (Division nextDivision in possibleNextDivisions)
        {
            availableDivisions.Remove(nextDivision);
            var res = _GenerateRandomPath(nextDivision);
            if (res != null) return res;
            availableDivisions.Add(nextDivision);
        }

        foreach (Division markedDivision in markedDivisions)
        {
            availableDivisions.Add(markedDivision);
        }

        path.Pop();
        return null;
    }

    private bool _CanPlaceSpawner(Division currentDivision)
    {
        Division previousDivision = path.Peek();
        var direction = currentDivision.Center - previousDivision.Center;
        Division spawnerPosition = availableDivisions.FirstOrDefault(div => div.Includes(currentDivision.Center + direction));
        if (spawnerPosition != null)
        {
            Instantiate(SpawnerPrefab, spawnerPosition.Center, Quaternion.identity);
            return true;
        }

        return false;
    }

    private bool _GeneratePath(Division currentDivision)
    {
        if (currentDivision == null) return false;

        if (currentDivision == m_PathEnd) return true; // TODO: also check for minimum path coverage

        if (!availableNodeDivisions.Any())
        {
            return _BackTrackToDivision(currentDivision, m_PathEnd);
        }

        IEnumerable<Division> nextDivisions = _GetNextDivisionsPriority(currentDivision, availableNodeDivisions);

        foreach (Division nextDivision in nextDivisions)
        {
            availableNodeDivisions.Remove(nextDivision);// if can't reach maybe give up on node

            if (_GenerateShortestPath(currentDivision, nextDivision))
            {
                if (_GeneratePath(nextDivision)) return true;
            }

            availableNodeDivisions.Add(nextDivision);
        }

        return false;
    }

    private bool _BackTrackToDivision(Division currentDivision, Division end)
    {
        if (currentDivision == null) return false;

        //var obj = Instantiate(PathPrefab, currentDivision.Center, Quaternion.identity);

        if (currentDivision == end) return true;

        //Division previousDivision = null;

        //// Remove neighbors of previous path
        //if (path.Any())
        //{
        //    previousDivision = path.Peek();
        //}

        //List<Division> markedDivisions = new List<Division>();

        //if (previousDivision != null)
        //{
        //    foreach (Vector3 move in m_Moves)
        //    {
        //        var neighborCenter = move + previousDivision.Center;
        //        var neighborDivision = availableDivisions.FirstOrDefault(div => div.Includes(neighborCenter));
        //        if (neighborDivision != null)
        //        {
        //            markedDivisions.Add(neighborDivision);
        //        }
        //    }
        //}

        //availableDivisions.RemoveWhere(div => markedDivisions.Contains(div));
        path.Push(currentDivision);
        IEnumerable<Vector3> moves = _GetNextMovePriority(currentDivision, end);
        foreach (Vector3 move in moves)
        {
            var nextCenter = currentDivision.Center + move;
            Division nextDivision = availableNodeDivisions.SingleOrDefault(div => div.Includes(nextCenter));
            if (nextDivision != end)
            {
                nextDivision = availableDivisions.SingleOrDefault(div => div.Includes(nextCenter));
                if (nextDivision == null)
                {
                    continue;
                }
            }
            //path.Push(nextDivision);
            availableDivisions.Remove(nextDivision);
            if (_BackTrackToDivision(nextDivision, end)) return true;
            //path.Pop();
            availableDivisions.Add(nextDivision);
        }

        //Destroy(obj);
        //foreach (Division markedDivision in markedDivisions)
        //{
        //    availableDivisions.Add(markedDivision);
        //}

        path.Pop();
        return false;
    }

    private IEnumerable<Division> _GetNextDivisionsPriority(Division current, HashSet<Division> availableDivisions)
    {
        return availableDivisions.OrderBy(div => Vector3.Distance(div.Center, current.Center));
    }

    private IEnumerable<Vector3> _GetNextMovePriority(Division start, Division end)
    {
        Vector3 startCenter = start.Center;
        Vector3 endCenter = end.Center;
        return m_Moves.OrderBy(mov => Vector3.Distance(startCenter + mov, endCenter));
    }

    private bool _IsPathValid(Vector3 position)
    {
        if (_TryGetContainingDivision(position, out Division division))
        {
            return true;
        }

        return false;
    }

    private IEnumerable<Division> _RandomizeNextDivisions(Division currentDivision)
    {
        List<Vector3> centers = m_Moves.Select(mov => currentDivision.Center + mov).ToList();
        List<Division> divisions = new List<Division>();
        foreach (Vector3 center in centers)
        {
            var nextDivision = availableDivisions.FirstOrDefault(div => div.Includes(center));
            if (nextDivision != null)
            { 
                divisions.Add(nextDivision);
            }
        }
        Random random = new Random();
        return divisions.OrderBy(_ => random.Next());
    }

    private bool _TryGetContainingDivision(Vector3 position, out Division division)
    {
        foreach (Division availableDivision in m_AvailableDivisions)
        {
            if (availableDivision.Includes(position)) continue;
            division = availableDivision;
            return true;
        }

        division = default;
        return false;
    }

    private void _BuildPath()
    {
        foreach (Division pathDivision in path)
        {
            Instantiate(PathPrefab, pathDivision.Center, Quaternion.identity);
        }

        GameObject[] visualizers = GameObject.FindGameObjectsWithTag("DivisionVisualizer");

        foreach (GameObject visualizer in visualizers)
        {
            Destroy(visualizer);
        }
    }
}
