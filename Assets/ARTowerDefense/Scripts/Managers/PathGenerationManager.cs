﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace ARTowerDefense.Managers
{
    class PathGenerationManager
    {

        #region Constants

        /// <summary>
        /// The length of a division
        /// </summary>
        public const float k_DivisionLength = .1f;
        /// <summary>
        /// The time iteration limit after which the threshold increases
        /// </summary>
        private const float k_IncreaseThresholdCountLimit = 2000;
        /// <summary>
        /// The percentage by which the threshold increases
        /// </summary>
        private const float k_IncreaseThresholdSize = .1f;
        /// <summary>
        /// Precision used for floating point comparison
        /// </summary>
        private const float k_Epsilon = 1e-5f;

        #endregion

        #region Prefabs

        private readonly GameObject m_SpawnerPrefab;
        private readonly GameObject m_PathPrefab;
        private readonly GameObject m_CurvedPathPrefab;

        #endregion

        /// <summary>
        /// A collection of 4 vectors depicting movement forward, backward, leftward and rightward on the horizontal plane
        /// </summary>
        private Vector3[] m_Moves;

        /// <summary>
        /// A dictionary of divisions and their corresponding division game object instance
        /// </summary>
        public Dictionary<Division, BuildingDivision> DivisionGameObjectDictionary { get; set; }

        /// <summary>
        /// A collection of divisions used for the path generation.
        /// Stores divisions that are not occupied by the path or divisions that are not adjacent to a path.
        /// </summary>
        private readonly HashSet<Division> m_AvailableDivisionsForPathGeneration;

        /// <summary>
        /// A set of all divisions that will contain paths
        /// </summary>
        private readonly Stack<Division> m_PathDivisions;

        /// <summary>
        /// Each time this field reached a value equal to k_IncreaseThresholdCountLimit, the m_AvailableDivisionsThreshold is modified
        /// </summary>
        private int m_IncreaseThresholdCount = 1;

        /// <summary>
        /// The initial desired rate of divisions not containing by paths and that are
        /// not divisions adjacent to a path 
        /// </summary>
        private float m_AvailableDivisionsThreshold = .3f;

        /// <summary>
        /// The division containing the home base
        /// </summary>
        private readonly Division m_HomeBaseDivision;

        /// <summary>
        /// The final path division, leading to the home base
        /// </summary>
        private readonly Division m_PathEnd;

        /// <summary>
        /// The division containing the spawner
        /// </summary>
        private Division m_SpawnerDivision;

        public PathGenerationManager(Dictionary<Division, BuildingDivision> divisionGameObjectDict, Division homeBaseDivision, 
            Division pathEnd, GameObject spawnerPrefab, GameObject pathPrefab, GameObject curvedPathPrefab)
        {
            _InitializeMoves();

            m_AvailableDivisionsForPathGeneration = new HashSet<Division>(divisionGameObjectDict
                .Where(kvp => !kvp.Value.HasBuilding)
                .Select(kvp => kvp.Key));

            m_PathDivisions = new Stack<Division>();

            m_HomeBaseDivision = homeBaseDivision;

            DivisionGameObjectDictionary = divisionGameObjectDict;

            m_SpawnerPrefab = spawnerPrefab;

            m_PathPrefab = pathPrefab;

            m_CurvedPathPrefab = curvedPathPrefab;
            m_PathEnd = pathEnd;
        }

        private void _InitializeMoves()
        {
            m_Moves = new[]
            {
                new Vector3(k_DivisionLength, 0, 0),
                new Vector3(-k_DivisionLength, 0, 0),
                new Vector3(0, 0, k_DivisionLength),
                new Vector3(0, 0, -k_DivisionLength)
            };
        }

        public Division GeneratePath()
        {
            Debug.Log("Started generating m_PathDivisions.");
            return _GenerateRandomPath(m_PathEnd);
        }

        private Division _GenerateRandomPath(Division currentDivision)
        {
            // After each 2000 calls on this method the number of divisions which were
            // not covered by paths increases in order to reduce path generation time
            if (Math.Abs(m_IncreaseThresholdCount++ % k_IncreaseThresholdCountLimit) < k_Epsilon)
            {
                m_AvailableDivisionsThreshold += k_IncreaseThresholdSize;
            }

            if (m_AvailableDivisionsForPathGeneration.Count < m_AvailableDivisionsThreshold * DivisionGameObjectDictionary.Count &&
                _TryPlaceSpawner(currentDivision))
            {
                m_PathDivisions.Push(currentDivision);
                return currentDivision;
            }

            Debug.Log("Started generating random m_PathDivisions.");

            Division previousDivision = null;

            // Remove neighbors of previous m_PathDivisions
            if (m_PathDivisions.Any())
            {
                previousDivision = m_PathDivisions.Peek();
            }

            List<Division> markedDivisions = new List<Division>();

            if (previousDivision != null)
            {
                Debug.Log("Previous division found.");
                foreach (Vector3 move in m_Moves)
                {
                    var neighborCenter = move + previousDivision.Center;
                    var neighborDivision = m_AvailableDivisionsForPathGeneration.FirstOrDefault(div => div.Includes(neighborCenter));
                    if (neighborDivision != null)
                    {
                        markedDivisions.Add(neighborDivision);
                    }
                }
            }

            m_AvailableDivisionsForPathGeneration.RemoveWhere(div => markedDivisions.Contains(div));

            IEnumerable<Division> possibleNextDivisions = _RandomizeNextDivisions(currentDivision);
            m_PathDivisions.Push(currentDivision);
            Debug.Log($"The m_PathDivisions contains {m_PathDivisions.Count} divisions.");
            foreach (Division nextDivision in possibleNextDivisions)
            {
                m_AvailableDivisionsForPathGeneration.Remove(nextDivision);
                var res = _GenerateRandomPath(nextDivision);
                if (res != null) return res;
                m_AvailableDivisionsForPathGeneration.Add(nextDivision);
            }

            foreach (Division markedDivision in markedDivisions)
            {
                m_AvailableDivisionsForPathGeneration.Add(markedDivision);
            }

            m_PathDivisions.Pop();
            return null;
        }

        private IEnumerable<Division> _RandomizeNextDivisions(Division currentDivision)
        {
            List<Vector3> centers = m_Moves.Select(mov => currentDivision.Center + mov).ToList();
            List<Division> divisions = new List<Division>();
            foreach (Vector3 center in centers)
            {
                var nextDivision = m_AvailableDivisionsForPathGeneration.FirstOrDefault(div => div.Includes(center));
                if (nextDivision != null)
                {
                    divisions.Add(nextDivision);
                }
            }

            Random random = new Random();
            return divisions.OrderBy(_ => random.Next());
        }

        private bool _TryPlaceSpawner(Division currentDivision)
        {
            var previousDivision = m_PathDivisions.Peek();
            var direction = currentDivision.Center - previousDivision.Center;
            m_SpawnerDivision =
                DivisionGameObjectDictionary.FirstOrDefault(kvp => kvp.Key.Includes(currentDivision.Center + direction))
                    .Key;
            if (m_SpawnerDivision == null) return false;
            // Sets the rotation to 90 degrees if the path reaches the spawner from its side 
            float rotation = Math.Abs(direction.z) < k_Epsilon ? 90 : 0;
            DivisionGameObjectDictionary[m_SpawnerDivision].AddBuilding(m_SpawnerPrefab, rotation);
            DivisionGameObjectDictionary[m_SpawnerDivision].Lock();
            return true;
        }

        public Transform[] BuildPath()
        {
            Debug.Log($"Started path building. The path contains {m_PathDivisions.Count} path divisions.");
            var pathWaypoints = new Transform[m_PathDivisions.Count + 2];
            pathWaypoints[0] = DivisionGameObjectDictionary[m_SpawnerDivision].transform;
            var pathDivisionsArray = m_PathDivisions.ToArray();
            int index = 1;
            var prevDiv = m_SpawnerDivision;
            for (int i = 0; i < pathDivisionsArray.Length; i++)
            {
                var nextDiv = i + 1 != pathDivisionsArray.Length ? pathDivisionsArray[i + 1] : m_HomeBaseDivision;
                var currDiv = pathDivisionsArray[i];
                var diff1 = currDiv.Center - prevDiv.Center;
                var diff2 = currDiv.Center - nextDiv.Center;
                var diff3 = prevDiv.Center - nextDiv.Center;

                if (Math.Abs(diff1.x - diff2.x) < k_Epsilon)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(m_PathPrefab, 90);
                }
                else if (Math.Abs(diff1.z - diff2.z) < k_Epsilon)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(m_PathPrefab);
                }
                else if (diff1.z < 0 && diff2.x < 0 && diff3.x < diff3.z || diff1.x < 0 && diff2.z < 0 && diff3.x > diff3.z)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(m_CurvedPathPrefab);
                }
                else if (diff3.x > 0 && diff1.z < 0 && diff2.x > 0 || diff3.x < 0 && diff1.x > 0 && diff2.z < 0)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(m_CurvedPathPrefab, -90);
                }
                else if (diff3.x > 0 && diff1.x < 0 && diff2.z > 0 || diff3.x < 0 && diff1.z > 0 && diff2.x < 0)
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(m_CurvedPathPrefab, 90);
                }
                else
                {
                    DivisionGameObjectDictionary[currDiv].AddBuilding(m_CurvedPathPrefab, 180);
                }

                DivisionGameObjectDictionary[currDiv].Lock();
                pathWaypoints[index++] = DivisionGameObjectDictionary[currDiv].transform;
                prevDiv = currDiv;
            }

            pathWaypoints[index] = DivisionGameObjectDictionary[m_HomeBaseDivision].transform;
            return pathWaypoints;
        }
    }
}