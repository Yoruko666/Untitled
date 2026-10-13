
/*

  _____ _ _    __        __         _     _  ____                _
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|

	TileWorldCreator (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using GiantGrey.TileWorldCreator.Utilities;
using GiantGrey.TileWorldCreator.Components;
using GiantGrey.TileWorldCreator.Attributes;
using GiantGrey.TileWorldCreator.UI;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;


namespace GiantGrey.TileWorldCreator
{
    [BuildLayer("Objects", "Objects.twc")]
    public class ObjectBuildLayer : BuildLayer, ISerializationCallbackReceiver
    {
        [System.Serializable]
        public class PrefabObject
        {
            public GameObject prefabObject;
            [Range(0f, 1f)]
            public float weight;
        }

        public List<PrefabObject> prefabObjects = new List<PrefabObject>();

        [System.Serializable]
        public class ChildSpawnSettings
        {
            public GameObject childPrefab;
            public float radius;
            public int count;

            public bool useRndScale;
            public bool useRndRotation;
            public bool uniformScale;
            public float uniformMinScale = 1f;
            public float uniformMaxScale = 1f;

            public Vector3 objectRNDMinScale = Vector3.one;
            public Vector3 objectRNDMaxScale = Vector3.one;

            public Vector3 objectRNDMinRotation = Vector3.zero;
            public Vector3 objectRNDMaxRotation = Vector3.zero;
        }

        public List<ChildSpawnSettings> childSpawnSettings = new List<ChildSpawnSettings>();

        [BlueprintLayer("Orientation Layer")]
        public string objectOrientationLayerGuid;
        private GameObject owner;

        public bool meshGenerationOverride = false;
        public bool mergeObjects;
        public ShadowCastingMode shadowCastingMode;
        public LayerMask objectLayer;
        public RenderingLayerMask renderingLayer;
        public bool assignMeshCollider;
        public Configuration.ColliderType colliderType;
        public float tileColliderHeight;
        public float tileColliderExtrusionHeight;
        public bool invertCollisionWalls;

        public Vector3 layerOffset = Vector3.zero;
        public Vector3 scaleOffset = Vector3.one;

        public bool useRndScale;
        public bool useRndRotation;
        public bool uniformScale;
        public float uniformMinScale = 1f;
        public float uniformMaxScale = 1f;
        public Vector3 objectRNDMinScale = Vector3.one;
        public Vector3 objectRNDMaxScale = Vector3.one;

        public Vector3 objectRNDMinRotation = Vector3.zero;
        public Vector3 objectRNDMaxRotation = Vector3.zero;
        public float objectRNDPositionOffsetRadius = 0f;

        public bool placeOnTop;
        public bool useLowest;
        public float topOffset;
        public LayerMask placeOnTopExcludedLayers;

        private Configuration configuration;
        private TileWorldCreatorManager manager;

        private List<Vector2> removePositions = new List<Vector2>();
        private List<int> modifiedClusters = new List<int>();
        private HashSet<int> modifiedClustersHashSet = new HashSet<int>();
        private Dictionary<int, Dictionary<Vector2, TileData>> tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();
        private List<TileData> newTiles = new List<TileData>();

        [SerializeField]
        private List<Vector2> serializedWorldPositions = new List<Vector2>();

        [SerializeField]
        private List<TileData> serializedTiles = new List<TileData>();

        // Orientation layer is used to rotate the object (y rotation) towards the orientation layer.
        // This is useful for separate ramp objects.
        [SerializeField]
        private bool useOrientationLayer;
        [SerializeField]
        private float yRotationOffset;
        private BlueprintLayer blueprintLayer;
        private BlueprintLayer orientationLayer;
        [SerializeField]
        private bool invertYRotationOffset;

        // Top left, top right, bottom left, bottom right
        bool[] tmpFourTileNeighbours = new bool[]
        {
            false, false, false, false
        };

        bool[] tmpEightTileNeighbours = new bool[]
        {
            false, false, false, false, false, false, false, false, false
        };

        private GameObject tmpLayerObject;


        public class SortedTiles
        {
            public int clusterID { get; set; }
            public List<TileData> tiles;

            public SortedTiles(int _cluster)
            {
                clusterID = _cluster;
                tiles = new List<TileData>();
            }
        }

        List<string> _layerGuids = new List<string>();
        List<string>  _layerNames = new List<string>();


        #region InspectorProperties
        VisualElement childSpawnSettingsContainer;
        #endregion

        public override void ResetLayer(TileWorldCreatorManager _manager)
        {
            worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
            tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();

            if (_manager == null)
            {
#if UNITY_6000_5_OR_NEWER
                var _managers = Object.FindObjectsByType<TileWorldCreatorManager>(findObjectsInactive: FindObjectsInactive.Include);
#elif UNITY_6000_0_OR_NEWER
                var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(sortMode: FindObjectsSortMode.None);
#else
                var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
#endif

                for (int i = 0; i < _managers.Length; i++)
                {
                    if (_managers[i].configuration == configuration)
                    {
                        _manager = _managers[i];
                    }
                }

            }
            else
            {
                manager = _manager;
            }

            if (_manager == null)
            {
                return;
            }

            // var _l = base.LayerObject;
            tmpLayerObject = base.GetLayerObject(_manager == null ? null : _manager.gameObject);
            if (tmpLayerObject != null)
            {
                var _clusters = tmpLayerObject.GetComponentsInChildren<ClusterIdentifier>(true);
                for (int c = 0; c < _clusters.Length; c++)
                {
                    var _cluster = _clusters[c];
                    DestroyImmediate(_cluster.gameObject);
                }
            }
        }

        public void AddPrefabObject(GameObject _prefabObject, float _weight = 1f)
        {
            prefabObjects.Add(new PrefabObject() { prefabObject = _prefabObject, weight = _weight });
        }

        public void SetBlueprintLayer(BlueprintLayer _layer)
        {
            assignedBlueprintLayerGuid = _layer.guid;
        }

        public override void ExecuteLayer(Configuration _configuration, GameObject _owner, TileWorldCreatorManager _manager, HashSet<int> _affectedClusters = null)
        {
            if (!isEnabled)
                return;

            isExecuting = true;

            owner = _owner;
            manager = _manager;
            configuration = _configuration;

            if (placeOnTop && _affectedClusters == null && !isLateExecuting)
            {
                if (manager.lateUpdateLayers == null)
                {
                    manager.lateUpdateLayers = new HashSet<BuildLayer>();
                }
                manager.lateUpdateLayers.Add(this);
                isExecuting = false;
                return;
            }

            if (configuration.cellSize != configuration.lastCellSize)
            {
                worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
                tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();

                configuration.lastCellSize = configuration.cellSize;
            }

            var _layer = _configuration.GetBlueprintLayerByGuid(assignedBlueprintLayerGuid);
            if (_layer == null)
            {
                UnityEngine.Debug.Log("layer not found: " + assignedBlueprintLayerGuid);
                isExecuting = false;
                return;
            }

            blueprintLayer = _layer;

            if (useOrientationLayer)
            {
                orientationLayer = configuration.GetBlueprintLayerByGuid(objectOrientationLayerGuid);
            }

            var _layerPositions = new HashSet<Vector2>();
            _layerPositions = _layer.GetAllCellPositions(_layerPositions);
            removePositions.Clear();

            List<Vector2> _newPositions = new List<Vector2>();

            var keys = new List<int>(worldGrid.Keys);
            foreach (var cls in keys)
            {
                var positions = new List<Vector2>(worldGrid[cls].Keys);
                foreach (var pos in positions)
                {
                    if (!_layerPositions.Contains(pos))
                    {
                        removePositions.Add(pos);
                    }
                }
            }

            // Perform removal from grid here before GenerateTilesParallel
            if (removePositions.Count > 0)
            {
                foreach (var pos in removePositions)
                {
                    int cls = GetPositionHashMapKey(pos);
                    if (worldGrid.TryGetValue(cls, out var subGrid))
                    {
                        subGrid.Remove(pos);
                        if (subGrid.Count == 0)
                        {
                            worldGrid.Remove(cls);
                        }
                    }

                    if (tileGrid.TryGetValue(cls, out var tSubGrid))
                    {
                        tSubGrid.Remove(pos);
                        if (tSubGrid.Count == 0)
                        {
                            tileGrid.Remove(cls);
                        }
                    }
                }
                //
                // #if UNITY_EDITOR
                // if (!Application.isPlaying)
                // {
                //     EditorUtility.SetDirty(this);
                // }
                // #endif
            }

            foreach (var pos in _layerPositions)
            {
                var _cluster = GetPositionHashMapKey(pos);
                if (!worldGrid.ContainsKey(_cluster))
                {
                    worldGrid.Add(_cluster, new Dictionary<Vector2, bool>());
                    _newPositions.Add(pos);
                }
                else
                {
                    if (!worldGrid[_cluster].ContainsKey(pos))
                    {
                        worldGrid[_cluster].Add(pos, true);
                        _newPositions.Add(pos);
                    }
                    else
                    {
                        worldGrid[_cluster][pos] = true;
                    }
                }
            }


            modifiedClusters.Clear();
            modifiedClustersHashSet.Clear();

            if (_affectedClusters != null && _affectedClusters.Count > 0)
            {
                foreach (var cluster in _affectedClusters)
                {
                    modifiedClustersHashSet.Add(cluster);
                }

                foreach (var pos in _newPositions)
                {
                    AddClusterNeighbors(modifiedClustersHashSet, GetPositionHashMapKey(pos));
                }

                foreach (var pos in removePositions)
                {
                    AddClusterNeighbors(modifiedClustersHashSet, GetPositionHashMapKey(pos));
                }
            }
            else if (_affectedClusters == null)
            {
                foreach (var cluster in worldGrid.Keys)
                {
                    modifiedClustersHashSet.Add(cluster);
                }
            }
            else if (_newPositions.Count > 0 || removePositions.Count > 0)
            {
                foreach (var pos in _newPositions)
                {
                    AddClusterNeighbors(modifiedClustersHashSet, GetPositionHashMapKey(pos));
                }

                foreach (var pos in removePositions)
                {
                    AddClusterNeighbors(modifiedClustersHashSet, GetPositionHashMapKey(pos));
                }
            }

            if (_newPositions.Count == 0 && removePositions.Count == 0 && modifiedClustersHashSet.Count == 0)
            {
                isExecuting = false;
                return;
            }

            modifiedClusters = modifiedClustersHashSet.Where(c => c >= 0).ToList();


            // List<Vector2> _positionsInCluster = new List<Vector2>();
            // foreach (var cluster in modifiedClusters)
            // {
            //     if (worldGrid.TryGetValue(cluster, out var clusterGrid))
            //     {
            //         // Add existing positions that are not NEWly added (to avoid double processing) and not in removePositions
            //         var existingInCluster = clusterGrid.Keys.Where(p => !_newPositions.Contains(p)).ToList();
            //         _positionsInCluster.AddRange(existingInCluster);
            //     }
            // }
            HashSet<Vector2> _positionsInCluster = new HashSet<Vector2>();
            foreach (var cluster in modifiedClusters)
            {
                if (worldGrid.TryGetValue(cluster, out var clusterGrid))
                {
                    foreach (var key in clusterGrid.Keys)
                    {
                        if (!removePositions.Contains(key) && !_newPositions.Contains(key))
                        {
                            _positionsInCluster.Add(key);
                        }
                    }
                }
            }


            _newPositions.AddRange(_positionsInCluster);

            /// Update build layers which have same assigned blueprint layer assigned as overrides.
            /// Also update any layer that has placeOnTop enabled, as it depends on the physical state of other layers.
            for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
                {
                    var _buildLayer = configuration.buildLayerFolders[i].buildLayers[j];
                    if (_buildLayer == null || !_buildLayer.isEnabled || _buildLayer == this)
                        continue;

                    bool _shouldLateUpdate = false;

                    // Check for placeOnTop
                    if (_buildLayer is TilesBuildLayer _tbl)
                    {
                        if (_tbl.placeOnTop) _shouldLateUpdate = true;
                    }
                    else if (_buildLayer is ObjectBuildLayer _obl)
                    {
                        if (_obl.placeOnTop) _shouldLateUpdate = true;
                    }

                    // Check for blueprint overrides
                    if (!_shouldLateUpdate && _buildLayer is TilesBuildLayer _tblOverride)
                    {
                        for (int m = 0; m < _tblOverride.tileLayers.Count; m++)
                        {
                            if (_tblOverride.tileLayers[m].layerOverrides == null) continue;

                            for (int t = 0; t < _tblOverride.tileLayers[m].layerOverrides.Count; t++)
                            {
                                if (_tblOverride.tileLayers[m].layerOverrides[t].blueprintOverrideLayer == assignedBlueprintLayerGuid)
                                {
                                    _shouldLateUpdate = true;
                                    break;
                                }
                            }
                            if (_shouldLateUpdate) break;
                        }
                    }

                    if (_shouldLateUpdate)
                    {
                        if (manager.lateUpdateLayers == null)
                        {
                            manager.lateUpdateLayers = new HashSet<BuildLayer>();
                        }
                        manager.lateUpdateLayers.Add(_buildLayer);
                    }
                }
            }

            if (_newPositions.Count > 0 || removePositions.Count > 0)
            {
                if (!Application.isPlaying)
                {
#if UNITY_EDITOR
                    EditorCoroutines.Execute(GenerateTiles(_newPositions));
#endif
                }
                else
                {
                    if (manager == null)
                    {
                        var _tmpManager = GameObject.FindAnyObjectByType<TileWorldCreatorManager>();
                        _tmpManager.StartCoroutine(GenerateTiles(_newPositions));
                    }
                    else
                    {
                        manager.StartCoroutine(GenerateTiles(_newPositions));
                    }
                }
            }
            else
            {
                isExecuting = false;
            }
        }



        IEnumerator GenerateTilesParallel(List<Vector2> positions)
        {
            var newTilesBag = new ConcurrentBag<TileData>();

            if (!Application.isPlaying)
            {
                // Reuse cached cluster set; rebuild only if missing
                if (availableClusters == null || availableClusters.Count == 0)
                {
                    if (tmpLayerObject == null)
                    {
                        tmpLayerObject = base.GetLayerObject(manager.gameObject);
                    }

                    ClusterIdentifier[] clusterArray = tmpLayerObject.gameObject.GetComponentsInChildren<ClusterIdentifier>(true);
                    availableClusters = new HashSet<ClusterIdentifier>(clusterArray);
                }
            }


#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayProgressBar($"Generating {configuration.width * configuration.height} tiles", "Please wait...", 0f);
            }
#endif

            // Parallelize tile processing
            Parallel.For(0, positions.Count, i =>
            {
                Vector2 worldPosition = positions[i];
                ProcessTileParallel(worldPosition, worldPosition, newTilesBag);
            });

            // Convert ConcurrentBag to List efficiently
            newTiles.Clear();
            // Pre-size to reduce reallocations
            if (newTiles.Capacity < newTilesBag.Count) newTiles.Capacity = newTilesBag.Count;
            foreach (var tile in newTilesBag) newTiles.Add(tile);

            // Update worldGrid sequentially
            if (positions.Count > 0)
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    var worldPosition = positions[i];
                    int worldGridHashmap = GetPositionHashMapKey(worldPosition);
                    if (!worldGrid.TryGetValue(worldGridHashmap, out var subGrid))
                    {
                        subGrid = new Dictionary<Vector2, bool>();
                        worldGrid[worldGridHashmap] = subGrid;
                    }
                    subGrid[worldPosition] = true;
                }

                // Merge tiles into tileGrid (single-threaded)
                for (int i = 0; i < newTiles.Count; i++)
                {
                    var t = newTiles[i];
                    int hashMapKey = GetPositionHashMapKey(t.tilePosition);
                    if (!tileGrid.TryGetValue(hashMapKey, out var subGrid))
                    {
                        subGrid = new Dictionary<Vector2, TileData>();
                        tileGrid[hashMapKey] = subGrid;
                    }
                    subGrid[t.tilePosition] = t;
                }

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(this);
                }
                #endif
            }

            yield return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.ClearProgressBar();
            }
#endif
            RefreshAndSortTiles();
        }

        void ProcessTileParallel(Vector2 cellPosition, Vector2 worldPosition, ConcurrentBag<TileData> newTilesBag)
        {
            TileData newTile = new TileData
            {
                isAssigned = true,
                tilePosition = cellPosition,
                worldMapPosition = worldPosition
            };

            newTilesBag.Add(newTile);
        }


        IEnumerator GenerateTiles(List<Vector2> _positions)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_positions.Count > 50)
                {
                    EditorUtility.DisplayProgressBar($"Generating {configuration.width * configuration.height} tiles",
                        "Please wait...", 0f);
                }
            }
            #endif

            HashSet<Vector2> existingCellPositions = new HashSet<Vector2>(_positions.Count);
            // List<TileData> newTiles = new List<TileData>();
            newTiles.Clear();
            if (newTiles.Capacity < _positions.Count) newTiles.Capacity = _positions.Count;


            // Only in editor mode
            if (!Application.isPlaying)
            {
                if (availableClusters != null)
                {
                    availableClusters.Clear();
                }
                else
                {
                    availableClusters = new HashSet<ClusterIdentifier>();
                }

                if (tmpLayerObject == null)
                {
                    if (manager == null)
                    {
#if UNITY_6000_5_OR_NEWER
                        var _managers = Object.FindObjectsByType<TileWorldCreatorManager>(findObjectsInactive: FindObjectsInactive.Include);
#elif UNITY_6000_0_OR_NEWER
                        var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
#else
                        var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
#endif
                        for (int i = 0; i < _managers.Length; i++)
                        {
                            if (_managers[i].configuration == configuration)
                            {
                                manager = _managers[i];
                                break;
                            }
                        }
                    }

                    tmpLayerObject = GetLayerObject(manager.gameObject);
                }

                ClusterIdentifier[] clusterArray = tmpLayerObject.gameObject.GetComponentsInChildren<ClusterIdentifier>(true);
                for (int c = 0; c < clusterArray.Length; c++)
                {
                    availableClusters.Add(clusterArray[c]);
                }
            }

            int posCount = _positions.Count;
            for (int i = 0; i < posCount; i ++)
            {
                Vector2 _worldPosition = _positions[i];

                ProcessSingleGridTile(_worldPosition, newTiles, existingCellPositions);
                UpdateWorldGrid(_worldPosition);

                if ((i & 511) == 0)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        if (_positions.Count > 50)
                        {
                            EditorUtility.DisplayProgressBar(
                                $"Generating {configuration.width * configuration.height} tiles", "Please wait...",
                                (float)i / _positions.Count);
                        }
                    }
                    #endif
                    yield return null;
                }
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.ClearProgressBar();
            }
            #endif
            RefreshAndSortTiles();
        }

        void UpdateWorldGrid(Vector2 worldPosition)
        {
            int worldGridHashmap = GetPositionHashMapKey(worldPosition);
            if (!worldGrid.TryGetValue(worldGridHashmap, out var subGrid))
            {
                subGrid = new Dictionary<Vector2, bool>();
                worldGrid[worldGridHashmap] = subGrid;
            }
            subGrid[worldPosition] = true;
        }

        void ProcessSingleGridTile(Vector2 worldPosition, List<TileData> newTiles, HashSet<Vector2> existingCellPositions)
        {
            AddOrUpdateTile(worldPosition, worldPosition, newTiles, existingCellPositions);
        }

        void AddOrUpdateTile(Vector2 cellPosition, Vector2 worldPosition, List<TileData> newTiles, HashSet<Vector2> existingCellPositions)
        {
            if (existingCellPositions.Contains(cellPosition)) return;


            int hashMapKey = GetPositionHashMapKey(cellPosition);
            TileData newTile = new TileData
            {
                isAssigned = true,
                // isNew = animatedPositions.Contains(worldPosition),
                tilePosition = cellPosition,
                worldMapPosition = worldPosition,
            };

            if (!tileGrid.TryGetValue(hashMapKey, out var subGrid))
            {
                subGrid = new Dictionary<Vector2, TileData>();
                tileGrid[hashMapKey] = subGrid;
            }

            if (subGrid.TryGetValue(cellPosition, out var existingTile))
            {
                existingTile.isAssigned = true;
                subGrid[cellPosition] = existingTile;
            }
            else
            {
                subGrid[cellPosition] = newTile;
            }

            newTiles.Add(newTile);
            existingCellPositions.Add(cellPosition);

        }

        void RefreshAndSortTiles()
        {
            List<SortedTiles> sortedTiles = new List<SortedTiles>();

            // *** FIX: Refresh each tile to update its configuration based on neighbors ***
            int _newTilesCount = newTiles.Count;
            for (int i = 0; i < _newTilesCount; i++)
            {
                newTiles[i] = RefreshPathCells2(newTiles[i], out int config);
            }

            foreach (var tile in newTiles)
            {
                int clusterID = GetPositionHashMapKey(tile.worldMapPosition);
                var cluster = sortedTiles.FirstOrDefault(s => s.clusterID == clusterID);
                if (cluster == null)
                {
                    cluster = new SortedTiles(clusterID);
                    cluster.tiles.Add(tile);
                    sortedTiles.Add(cluster);
                }
                else
                {
                    cluster.tiles.Add(tile);
                }
            }

            if (sortedTiles.Count > 0 || modifiedClusters.Count > 0)
            {
                if (!Application.isPlaying)
                {
                    #if UNITY_EDITOR
                    GiantGrey.TileWorldCreator.Utilities.EditorCoroutines.Execute(InstantiateByClusters(sortedTiles));
                    #endif
                }
                else
                {
                    if (manager == null)
                    {
                        var _tmpManager = GameObject.FindAnyObjectByType<TileWorldCreatorManager>();
                        _tmpManager.StartCoroutine(InstantiateByClusters(sortedTiles));
                    }
                    else
                    {
                        manager.StartCoroutine(InstantiateByClusters(sortedTiles));
                    }

                }
            }
            else
            {
                isExecuting = false;
            }
        }

//         void RefreshAndSortTiles()
//         {
//             // Profiler.BeginSample("REFRESH AND SORT");
//             Debug.Log("new tiles: " + newTiles.Count);
//             int _newTilesCount = newTiles.Count;
//             for (int i = 0; i < _newTilesCount; i++)
//             {
//                 newTiles[i] = RefreshPathCells2(newTiles[i], out int config);
//             }
//
//             // Preallocate capacity to reduce allocations
//             Dictionary<int, SortedTiles> sortedTileMap = new Dictionary<int, SortedTiles>(_newTilesCount);
//
//             for (int i = 0; i < _newTilesCount; i++)
//             {
//                 TileData tile = newTiles[i];
//                 // if (isNeighbourMasked(tile)) continue;
//
//                 int clusterID = GetClusterKeyFromPosition(tile.worldMapPosition);
//
//                 // var cluster = sortedTiles.FirstOrDefault(s => s.clusterID == clusterID);
//
//                 if (!sortedTileMap.TryGetValue(clusterID, out SortedTiles cluster))
//                 {
//                     cluster = new SortedTiles(clusterID);
//                     cluster.tiles.Add(tile);
//                     sortedTileMap[clusterID] = cluster;
//                 }
//                 else
//                 {
//                     cluster.tiles.Add(tile);
//                 }
//             }
//
//             // Profiler.EndSample();
//             if (sortedTileMap.Count > 0 || modifiedClusters.Count > 0)
//             {
//                 if (!Application.isPlaying)
//                 {
// #if UNITY_EDITOR
//                     EditorCoroutines.Execute(InstantiateByClusters(sortedTileMap.Values.ToList()));
// #endif
//                 }
//                 else
//                 {
//                     manager.StartCoroutine(InstantiateByClusters(sortedTileMap.Values.ToList()));
//                 }
//             }
//
//         }

        IEnumerator InstantiateByClusters(List<SortedTiles> _sortedTiles)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (_sortedTiles.Count > 50)
                {
                    EditorUtility.DisplayProgressBar("Instantiating tiles", "Please wait...", 0f);
                }
            }
#endif

            for (int c = 0; c < modifiedClusters.Count; c ++)
            {
                var _cluster = FindCluster(modifiedClusters[c], false);
                if (_cluster != null)
                {
                    if (availableClusters != null)
                    {
                        var comp = _cluster.GetComponent<ClusterIdentifier>();
                        if (comp != null) availableClusters.Remove(comp);
                    }
                    GameObject.DestroyImmediate(_cluster);
                }
            }

            // Also destroy clusters that are now empty because of removed positions
            if (removePositions != null)
            {
                foreach (var pos in removePositions)
                {
                    int clusterKey = GetPositionHashMapKey(pos);
                    // If the cluster is empty after removal, we might want to destroy it if it's not already in modifiedClusters
                    if (!modifiedClustersHashSet.Contains(clusterKey))
                    {
                        var _cluster = FindCluster(clusterKey, false);
                        if (_cluster != null)
                        {
                            // Check if it really has no more tiles in worldGrid
                            if (!worldGrid.ContainsKey(clusterKey) || worldGrid[clusterKey].Count == 0)
                            {
                                if (availableClusters != null)
                                {
                                    var comp = _cluster.GetComponent<ClusterIdentifier>();
                                    if (comp != null) availableClusters.Remove(comp);
                                }
                                GameObject.DestroyImmediate(_cluster);
                            }
                        }
                    }
                }
            }


            for (int i = 0; i < _sortedTiles.Count; i++)
            {
                for(int j = 0; j < _sortedTiles[i].tiles.Count; j ++)
                {
                    InstantiateTile(_sortedTiles[i].tiles[j],  _sortedTiles[i].clusterID);
                }

                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (_sortedTiles.Count > 50)
                    {
                        EditorUtility.DisplayProgressBar("Instantiating tiles", "Please wait...",
                            (float)i / (float)_sortedTiles.Count);
                    }
                }
#endif
            }

            #if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
            #endif

            yield return null;

            // Merge objects into clusters
            MergeClusters();

            isExecuting = false;

        }


        public TileData RefreshPathCells2(TileData _tileData, out int configuration)
        {
            configuration = 0;

            // Top Left
            tmpFourTileNeighbours[0] = false;
            // Top Right
            tmpFourTileNeighbours[1] = false;
            // Bottom Left
            tmpFourTileNeighbours[2] = false;
            // Bottom Right
            tmpFourTileNeighbours[3] = false;

            tmpEightTileNeighbours[0] = false;
            tmpEightTileNeighbours[1] = false;
            tmpEightTileNeighbours[2] = false;
            tmpEightTileNeighbours[3] = false;
            tmpEightTileNeighbours[4] = true; // center
            tmpEightTileNeighbours[5] = false;
            tmpEightTileNeighbours[6] = false;
            tmpEightTileNeighbours[7] = false;
            tmpEightTileNeighbours[8] = false;

            var _offset = 1f;

            for (float x = -_offset; x < _offset + _offset; x += _offset)
            {
                for (float y = -_offset; y < _offset + _offset; y += _offset)
                {
                    var _pos = new Vector2(_tileData.tilePosition.x + (x), _tileData.tilePosition.y + (y));

                    var _hashMapKey = GetClusterKeyFromPosition(_pos);
                    if (worldGrid.TryGetValue(_hashMapKey, out var _value))
                    {
                        if (worldGrid[_hashMapKey].TryGetValue(_pos, out var _value2))
                        {
                            var _tile = worldGrid[_hashMapKey][_pos];
                            if (_tile)
                            {

                                if ((x == -1f && y == 1f))
                                {
                                    // _neighbourCount ++;
                                    tmpEightTileNeighbours[0] = true;
                                }
                                if ((x == 0f && y == 1f))
                                {
                                    tmpEightTileNeighbours[1] = true;
                                }
                                if ((x == 1f && y == 1f))
                                {
                                    // _neighbourCount ++;
                                    tmpEightTileNeighbours[2] = true;
                                }
                                if (x == -1f && y == 0f)
                                {
                                    tmpEightTileNeighbours[3] = true;
                                }
                                if (x == 1f && y == 0f)
                                {
                                    tmpEightTileNeighbours[5] = true;
                                }
                                if ((x == -1f && y == -1f))
                                {
                                    // _neighbourCount ++;
                                    tmpEightTileNeighbours[6] = true;
                                }
                                if (x == 0f && y == -1f)
                                {
                                    tmpEightTileNeighbours[7] = true;
                                }
                                if ((x == 1f && y == -1f))
                                {
                                    // _neighbourCount++;
                                    tmpEightTileNeighbours[8] = true;
                                }

                            }
                        }
                    }
                }
            }



            var _configuration = GetConfigurationBitmask(tmpEightTileNeighbours);
            configuration = _configuration;

            _tileData.configuration = _configuration;


            // _tileData.tileType = GetTileType(_tileData.configuration, _tileData, out _tileData.yRotation);

            var _hashMapKey2 = GetClusterKeyFromPosition(_tileData.tilePosition);
            if (tileGrid.TryGetValue(_hashMapKey2, out var _value3))
            {
                if (tileGrid[_hashMapKey2].TryGetValue(_tileData.tilePosition, out var _value4))
                {
                    tileGrid[_hashMapKey2][_tileData.tilePosition] = _tileData;
                }
            }

            return _tileData;
        }


        // Better version by computing rotations directly instead of using
        // bitmask codes
        float GetRotationBasedOrientationLayer(TileData _tile)
        {
            Vector2Int pos = Vector2Int.RoundToInt(_tile.tilePosition);


            var _posSet = new HashSet<Vector2Int>();
            foreach (var v in orientationLayer.allPositions)
                _posSet.Add(Vector2Int.RoundToInt(v));


             // Prefer cardinal neighbors (N, E, S, W)
            (Vector2Int offset, float rotation)[] cardinals = {
                (new Vector2Int(0,  1), 270),   // North -> 270
                (new Vector2Int(1,  0), 0),    // East  -> 0°
                (new Vector2Int(0, -1), 90),  // South -> 90
                (new Vector2Int(-1, 0), 180)   // West  -> 180°
            };

            if (invertYRotationOffset)
            {
                cardinals = new (Vector2Int offset, float rotation)[] {
                    (new Vector2Int(0,  1), 90f),   // North -> 90°
                    (new Vector2Int(1,  0), 0f),    // East  -> 0°
                    (new Vector2Int(0, -1), 270f),  // South -> 270°
                    (new Vector2Int(-1, 0), 180f)   // West  -> 180°
                };
            }

            foreach (var kv in cardinals)
            {
                if (_posSet.Contains(pos + kv.offset))
                    return kv.rotation;
            }

            // If no cardinal found, check diagonals and snap their angle to nearest 90°
            Vector2Int[] diagonals = {
                new Vector2Int(1, 1),   // NE
                new Vector2Int(1, -1),  // SE
                new Vector2Int(-1, -1), // SW
                new Vector2Int(-1, 1)   // NW
            };

            foreach (var d in diagonals)
            {
                if (_posSet.Contains(pos + d))
                {
                    float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;
                    // Snap to nearest multiple of 90 (0, 90, 180, 270)
                    return Mathf.Round(angle / 90f) * 90f;
                }
            }

            return 0f;
        }

        void InstantiateTile(TileData _tileData, int _clusterKey)
        {

            var _cluster = FindCluster(_clusterKey);

            uint _seed;

            if (configuration.useGlobalRandomSeed)
            {
                _seed = (uint)configuration.globalRandomSeed + (uint)_tileData.tilePosition.x + ((uint)configuration.globalRandomSeed * (uint)_tileData.tilePosition.y);
                if (_seed == 0) _seed = 1; // force non-zero seed
            }
            else
            {
                // Better hash to ensure distinct seeds from float positions
                int x = Mathf.FloorToInt(_tileData.tilePosition.x);
                int y = Mathf.FloorToInt(_tileData.tilePosition.y);
                _seed = (uint)(x * 73856093 ^ y * 19349663); // simple 2D hash
                if (_seed == 0) _seed = 1; // force non-zero seed
            }


            random = new Unity.Mathematics.Random(_seed);

            var _prefabObject = GetRandomPrefabObject(_tileData.tilePosition);

            if (_prefabObject != null)
            {
                // var _rndPosition = random.NextFloat2Direction() * objectRNDPositionOffsetRadius;
                // var _position = new Vector3((_tileData.tilePosition.x ) * configuration.cellSize + _rndPosition.x + owner.transform.position.x + layerOffset.x, blueprintLayer.defaultLayerHeight + layerOffset.y + owner.transform.position.y, (_tileData.tilePosition.y) * configuration.cellSize + _rndPosition.y + owner.transform.position.z + layerOffset.z);

                // Get rotation based on orientation layer
                var _yRotation = 0f;
                Quaternion _rotation = Quaternion.identity;
                if (useRndRotation)
                {
                    _rotation = Quaternion.Euler(new Vector3(random.NextFloat(objectRNDMinRotation.x, objectRNDMaxRotation.x), random.NextFloat(objectRNDMinRotation.y, objectRNDMaxRotation.y), random.NextFloat(objectRNDMinRotation.z, objectRNDMaxRotation.z)));
                }

                if (orientationLayer != null && useOrientationLayer)
                {
                    _yRotation = GetRotationBasedOrientationLayer(_tileData);
                    _rotation = Quaternion.Euler(new Vector3(random.NextFloat(objectRNDMinRotation.x, objectRNDMaxRotation.x), _yRotation + yRotationOffset, random.NextFloat(objectRNDMinRotation.z, objectRNDMaxRotation.z)));
                }

                var  _newObject = GameObject.Instantiate(_prefabObject, Vector3.zero, _rotation);

                if (useRndScale)
                {
                    if (!uniformScale)
                    {
                        _newObject.transform.localScale = new Vector3(random.NextFloat(objectRNDMinScale.x, objectRNDMaxScale.x), random.NextFloat(objectRNDMinScale.y, objectRNDMaxScale.y), random.NextFloat(objectRNDMinScale.z, objectRNDMaxScale.z));
                    }
                    else
                    {
                        var _rndScale = random.NextFloat(uniformMinScale, uniformMaxScale);
                        _newObject.transform.localScale = new Vector3(_rndScale, _rndScale, _rndScale);
                    }
                }

                _newObject.transform.SetParent(_cluster.transform, false);

                // Correct position
                var _newTilePosition = new Vector3((_tileData.tilePosition.x) * configuration.cellSize, blueprintLayer.defaultLayerHeight + layerOffset.y, (_tileData.tilePosition.y) * configuration.cellSize);
                _newObject.transform.localPosition = _newTilePosition;


                // Child spawn
                for (int c = 0; c < childSpawnSettings.Count; c++)
                {
                    for (int a = 0; a < childSpawnSettings[c].count; a++)
                    {
                        var _rndPos = random.NextFloat2Direction() * childSpawnSettings[c].radius;
                        var _finPos = _newTilePosition + new Vector3(_rndPos.x, 0, _rndPos.y);
                        var _childRotation = Quaternion.Euler(new Vector3(random.NextFloat(childSpawnSettings[c].objectRNDMinRotation.x, childSpawnSettings[c].objectRNDMaxRotation.x), random.NextFloat(childSpawnSettings[c].objectRNDMinRotation.y, childSpawnSettings[c].objectRNDMaxRotation.y), random.NextFloat(childSpawnSettings[c].objectRNDMinRotation.z, childSpawnSettings[c].objectRNDMaxRotation.z)));
                        var _childObject = GameObject.Instantiate(childSpawnSettings[c].childPrefab, _finPos, _childRotation);

                        if (!childSpawnSettings[c].uniformScale)
                        {
                            _childObject.transform.localScale = new Vector3(random.NextFloat(childSpawnSettings[c].objectRNDMinScale.x, childSpawnSettings[c].objectRNDMaxScale.x), random.NextFloat(childSpawnSettings[c].objectRNDMinScale.y, childSpawnSettings[c].objectRNDMaxScale.y), random.NextFloat(childSpawnSettings[c].objectRNDMinScale.z, childSpawnSettings[c].objectRNDMaxScale.z));
                        }
                        else
                        {
                            var _rndScale = random.NextFloat(childSpawnSettings[c].uniformMinScale, childSpawnSettings[c].uniformMaxScale);
                            _childObject.transform.localScale = new Vector3(_rndScale, _rndScale, _rndScale);
                        }


                        _childObject.transform.SetParent(_cluster.transform, false);
                    }
                }
            }

            int _hashMapKey = GetPositionHashMapKey(new Vector2(_tileData.tilePosition.x, _tileData.tilePosition.y));
            if (tileGrid.ContainsKey(_hashMapKey))
            {
                if (tileGrid[_hashMapKey].ContainsKey(_tileData.tilePosition))
                {
                    var _td = tileGrid[_hashMapKey][_tileData.tilePosition];
                    tileGrid[_hashMapKey][_tileData.tilePosition] = _td;
                }
            }
        }

        public override void PostExecuteLayer(Configuration _configuration, GameObject _owner, TileWorldCreatorManager _manager)
        {
            if (placeOnTop)
            {
                AdjustHeightBeforeMerge();
            }

            base.PostExecuteLayer(_configuration, _owner, _manager);

            // Merge clusters after adjustment
            // We need to bypass the placeOnTop check in MergeClusters
            var _oldPlaceOnTop = placeOnTop;
            placeOnTop = false;
            MergeClusters();
            placeOnTop = _oldPlaceOnTop;
        }

        private void AdjustHeightBeforeMerge()
        {
            if (modifiedClusters == null) return;
            foreach (var clusterKey in modifiedClusters)
            {
                var cluster = FindCluster(clusterKey, false);
                if (cluster == null) continue;
                // Adjust each tile in the cluster
                for (int i = 0; i < cluster.transform.childCount; i++)
                {
                    var tile = cluster.transform.GetChild(i);
                    var worldPos = tile.position;

                    // Raycast down to find the highest point
                    float targetY = useLowest ? Mathf.Infinity : -Mathf.Infinity;
                    bool found = false;

                    // Invert the excluded layers to get the layers we WANT to hit
                    int layerMask = ~placeOnTopExcludedLayers;

                    // We use RaycastAll to find all objects below a high point.
                    RaycastHit[] hits = Physics.RaycastAll(new Vector3(worldPos.x, 2000f, worldPos.z), Vector3.down, 4000f, layerMask);
                    foreach (var hit in hits)
                    {
                        // Ignore current cluster/tiles
                        if (hit.transform.IsChildOf(cluster.transform) || hit.transform == cluster.transform) continue;

                        if (useLowest)
                        {
                            if (hit.point.y < targetY)
                            {
                                targetY = hit.point.y;
                                found = true;
                            }
                        }
                        else
                        {
                            if (hit.point.y > targetY)
                            {
                                targetY = hit.point.y;
                                found = true;
                            }
                        }
                    }

                    if (found)
                    {
                        tile.position = new Vector3(worldPos.x, targetY + topOffset, worldPos.z);
                    }
                }
            }
        }

        public virtual void MergeClusters()
        {
            // If we are placing on top, we delay merging until PostExecuteLayer
            if (placeOnTop) return;

             // Apply settings with mesh generation override
            var _mergeObjects = meshGenerationOverride ? mergeObjects : configuration.mergeTiles;
            var _colliderType = meshGenerationOverride ? colliderType : configuration.colliderType;
            var _shadowCastingMode = meshGenerationOverride ? shadowCastingMode : configuration.shadowCastingMode;
            var _objectLayer = meshGenerationOverride ? objectLayer : configuration.objectLayer;
            var _renderingLayer = meshGenerationOverride ? renderingLayer : configuration.renderingLayer;
            var _tileColliderExtrusion = meshGenerationOverride ? tileColliderExtrusionHeight : configuration.tileColliderExtrusionHeight;
            var _tileColliderHeight = meshGenerationOverride ? tileColliderHeight : configuration.tileColliderHeight;
            var _invertWalls = meshGenerationOverride ? invertCollisionWalls : configuration.invertCollisionWalls;

            if (_mergeObjects)
            {
                for (int i = 0; i < modifiedClusters.Count; i++)
                {
                    var _cluster = FindCluster(modifiedClusters[i], false);

                    if (_cluster == null)
                        continue;

                    MeshCombiner _combiner;
                    if (!_cluster.TryGetComponent<MeshCombiner>(out _combiner))
                    {
                        _combiner = _cluster.AddComponent<MeshCombiner>();
                    }

                    _combiner.CreateMultiMaterialMesh = true;
                    _combiner.DestroyCombinedChildren = true;

                    _combiner.CombineMeshes(false);
                    var _meshFilter = _cluster.GetComponent<MeshFilter>();
                    if (_meshFilter != null && _meshFilter.sharedMesh != null && _meshFilter.sharedMesh.vertexCount > 0)
                    {
                        switch (_colliderType)
                        {
                            case Configuration.ColliderType.meshCollider:

                                if (_meshFilter.sharedMesh.vertexCount > 0)
                                {
                                    if (!_cluster.TryGetComponent<MeshCollider>(out var _meshCollider))
                                    {
                                        _meshCollider = _cluster.AddComponent<MeshCollider>();
                                    }
                                    _meshCollider.sharedMesh = _cluster.GetComponent<MeshFilter>().sharedMesh;
                                    _meshCollider.cookingOptions = MeshColliderCookingOptions.None;
                                }
                                break;
                            case Configuration.ColliderType.tileCollider:
                                var _layer = configuration.GetBlueprintLayerByGuid(assignedBlueprintLayerGuid);
                                if (_layer != null)
                                {
                                    HashSet<Vector2> _allPositions = new HashSet<Vector2>();
                                    _layer.GetAllCellPositions(_allPositions);

                                    var _clusterPositions = _layer.GetPositionsInCluster(modifiedClusters[i]);
                                    var _collisionMesh = GridMeshGenerator.GenerateMesh(_clusterPositions, _allPositions, configuration.cellSize, _layer.defaultLayerHeight + layerOffset.y + _tileColliderHeight, _tileColliderExtrusion, _invertWalls);
                                    if (_meshFilter.sharedMesh.vertexCount > 0)
                                    {
                                        if (!_cluster.TryGetComponent<MeshCollider>(out var _meshCollider2))
                                        {
                                            _meshCollider2 = _cluster.AddComponent<MeshCollider>();
                                        }
                                        _meshCollider2.cookingOptions = MeshColliderCookingOptions.None;
                                        _meshCollider2.sharedMesh = _collisionMesh;
                                    }
                                }
                                break;
                        }


                        var _meshRenderer = _cluster.GetComponent<MeshRenderer>();
                        _meshRenderer.shadowCastingMode = _shadowCastingMode;
                        if (_renderingLayer == 0)
                        {
                            int _layerIndex = GetRenderingLayerIndex("Default");
                            _renderingLayer = (uint)(1 << _layerIndex);
                        }
                        _meshRenderer.renderingLayerMask = _renderingLayer;
                        _cluster.gameObject.layer = _objectLayer.value;

                    }
                }
            }
        }

        int GetConfigurationBitmask(bool[] connectionGrid)
        {
            int config = 0;
            for (int i = 0; i < connectionGrid.Length; i++)
            {
                if (connectionGrid[i])
                {
                    config += 1 << i;
                }
            }
            return config;
        }

        int GetRenderingLayerIndex(string name)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null)
            {
                string[] layerNames = RenderingLayerMask.GetDefinedRenderingLayerNames();
                for (int i = 0; i < layerNames.Length; i++)
                {
                    if (layerNames[i] == name)
                    {
                        return i;
                    }
                }
            }
            return -1; // Not found
        }


        GameObject FindCluster(int _clusterKey, bool _createIfNotFound = true)
        {

            ClusterIdentifier _cluster = null;

            if (availableClusters != null)
            {
                foreach (var _cls in availableClusters)
                {
                    if (_cls.clusterID == _clusterKey)
                    {
                        _cluster = _cls;
                    }
                }
            }

            if (_cluster == null && _createIfNotFound)
            {
                return CreateCluster(_clusterKey);
            }

            return _cluster != null ? _cluster.gameObject : null;
        }

        GameObject CreateCluster(int _clusterKey)
        {
            var _key = layerName + "_Cluster_" + _clusterKey;
            GameObject _obj = null;

            _obj = new GameObject(_key);

            if (tmpLayerObject == null)
            {
                tmpLayerObject = base.GetLayerObject(manager.gameObject);
            }

            _obj.transform.SetParent(tmpLayerObject.transform, false);
            _obj.transform.localRotation = Quaternion.identity;

            var _clusterComponent = _obj.AddComponent<ClusterIdentifier>();

            _clusterComponent.clusterID = _clusterKey;
            _clusterComponent.layerGuid = guid;

            if (availableClusters == null)
            {
                availableClusters = new HashSet<ClusterIdentifier>();
            }

            availableClusters.Add(_clusterComponent);

            return _obj;
        }

        private int GetPositionHashMapKey(Vector2 _position)
        {
            if (configuration == null) return 0;
            int clusterX = Mathf.FloorToInt(_position.x / configuration.clusterCellSize);
            int clusterY = Mathf.FloorToInt(_position.y / configuration.clusterCellSize);
            return clusterX + (configuration.clusterYMultiplier * clusterY);
        }


        private void AddClusterNeighbors(HashSet<int> clusterSet, int clusterKey)
        {
            if (clusterKey < 0) return;

            clusterSet.Add(clusterKey);

            int clusterX = clusterKey % configuration.clusterYMultiplier;
            int clusterY = clusterKey / configuration.clusterYMultiplier;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    int nx = clusterX + x;
                    int ny = clusterY + y;

                    if (nx >= 0 && ny >= 0)
                    {
                        clusterSet.Add(nx + (configuration.clusterYMultiplier * ny));
                    }
                }
            }
        }

        private int GetClusterKeyFromPosition(Vector2 _position)
        {
            return GetPositionHashMapKey(_position);
        }


        GameObject GetRandomPrefabObject(Vector2 _cellPosition)
        {
            if (prefabObjects.Count > 1)
            {
                // Get random preset based on their weight value
                var _totalWeight = prefabObjects.Select(x => x.weight).Sum();

                return prefabObjects[RandomWeighted(_totalWeight)].prefabObject;
            }
            if (prefabObjects.Count > 0)
            {
                return prefabObjects[0].prefabObject;
            }
            else
            {
                return null;
            }
        }

        int RandomWeighted(float _total)
        {
            // UnityEngine.Random.InitState(System.Environment.TickCount);
            var randVal = UnityEngine.Random.Range(0f, _total);

            // float randVal = random.NextFloat(_total);

            float cumulative = 0f;
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                cumulative += prefabObjects[i].weight;
                if (randVal < cumulative)
                    return i;
            }

            return prefabObjects.Count - 1; // fallback (should rarely hit)
        }


        public void OnBeforeSerialize()
        {
            if (worldGrid == null) worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
            if (tileGrid == null) tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();

            serializedWorldPositions = worldGrid.SelectMany(x => x.Value.Keys).ToList();
            serializedTiles = tileGrid.SelectMany(x => x.Value.Values).ToList();
        }

        public void OnAfterDeserialize()
        {
            worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
            tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();

            if (serializedWorldPositions == null) serializedWorldPositions = new List<Vector2>();
            if (serializedTiles == null) serializedTiles = new List<TileData>();

            for (int i = 0; i < serializedWorldPositions.Count; i++)
            {
                var pos = serializedWorldPositions[i];
                var _key = GetPositionHashMapKey(pos);
                if (worldGrid.ContainsKey(_key))
                {
                    if (!worldGrid[_key].ContainsKey(pos))
                    {
                        worldGrid[_key].Add(pos, true);
                    }
                }
                else
                {
                    worldGrid.Add(_key, new Dictionary<Vector2, bool>());
                    worldGrid[_key].Add(pos, true);
                }
            }

            for (int i = 0; i < serializedTiles.Count; i++)
            {
                var tile = serializedTiles[i];
                var _key = GetPositionHashMapKey(tile.tilePosition);
                if (tileGrid.ContainsKey(_key))
                {
                    if (!tileGrid[_key].ContainsKey(tile.tilePosition))
                    {
                        tileGrid[_key].Add(tile.tilePosition, tile);
                    }
                }
                else
                {
                    tileGrid.Add(_key, new Dictionary<Vector2, TileData>());
                    tileGrid[_key].Add(tile.tilePosition, tile);
                }
            }
        }


#if UNITY_EDITOR
#region Inspector
        public override VisualElement CreateInspectorGUI(Configuration _asset, Editor _editor, LayerFoldoutElement _foldout)
        {
            childSpawnSettingsContainer = new VisualElement();
            var _layerItem = new VisualElement();
            var _assetEditor = _editor as ConfigurationEditor;
            var _layerSerializedObject = new SerializedObject(this);

            _layerItem.style.flexGrow = 1;

            // var _prefab = new ObjectField();
            // _prefab.allowSceneObjects = false;
            // _prefab.label = "Prefab";
            // _prefab.BindProperty(_layerSerializedObject.FindProperty("prefabObject"));

            var _prefabObjects = new PropertyField();
            _prefabObjects.BindProperty(_layerSerializedObject.FindProperty("prefabObjects"));

            var _textField = new TextField();
            _textField.label = "Layer Name";
            _textField.BindProperty(_layerSerializedObject.FindProperty("layerName"));
            _textField.RegisterValueChangedCallback((evt) =>
            {
                _foldout.SetLabel(evt.newValue);
            });


            // var _defaultIndex = 0;
            var _orientationLayerIndex = 0;
            GetChoices(_asset);

            // if (!string.IsNullOrEmpty(assignedBlueprintLayerGuid))
            // {
            //     // find index of layerGuid
            //     for (int i = 0; i < _layerGuids.Count; i ++)
            //     {
            //         if (_layerGuids[i] == assignedBlueprintLayerGuid)
            //         {
            //             _defaultIndex = i;
            //         }
            //     }
            // }
            if (!string.IsNullOrEmpty(objectOrientationLayerGuid))
            {
                for (int i = 0; i < _layerGuids.Count; i ++)
                {
                    if (_layerGuids[i] == objectOrientationLayerGuid)
                    {
                        _orientationLayerIndex = i;
                    }
                }
            }

            var _shadowCastingMode = new PropertyField();
            _shadowCastingMode.BindProperty(_layerSerializedObject.FindProperty("shadowCastingMode"));

            var _objectLayer = new LayerField();
            _objectLayer.label = "Object Layer";
            _objectLayer.BindProperty(_layerSerializedObject.FindProperty("objectLayer"));


#if !UNITY_6000_0_OR_NEWER

            var _renderingLayer = new RenderingLayerMaskField("Rendering LayerMask", defaultMask: renderingLayer.value);
            _renderingLayer.RegisterValueChangedCallback(evt =>
            {
                _layerSerializedObject.Update();
                renderingLayer.value = _renderingLayer.value;
                _layerSerializedObject.ApplyModifiedProperties();
            });
#else

            var _renderingLayer = new RenderingLayerMaskField();
            _renderingLayer.label = "Rendering LayerMask";
            _renderingLayer.BindProperty(_layerSerializedObject.FindProperty("renderingLayer"));

#endif

            var _collider = new PropertyField();
            _collider.BindProperty(_layerSerializedObject.FindProperty("colliderType"));

            var _tileColliderHeight = new PropertyField();
            _tileColliderHeight.BindProperty(_layerSerializedObject.FindProperty("tileColliderHeight"));
            var _tileColliderExtrusion = new PropertyField();
            _tileColliderExtrusion.BindProperty(_layerSerializedObject.FindProperty("tileColliderExtrusionHeight"));
            var _invertCollisionWalls = new PropertyField();
            _invertCollisionWalls.BindProperty(_layerSerializedObject.FindProperty("invertCollisionWalls"));

            var _mergeTiles = new Toggle();
            _mergeTiles.label = "Merge Objects";
            _mergeTiles.BindProperty(_layerSerializedObject.FindProperty("mergeObjects"));
            _mergeTiles.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _shadowCastingMode.SetEnabled(evt.newValue);
                _objectLayer.SetEnabled(evt.newValue);
                _renderingLayer.SetEnabled(evt.newValue);
                _collider.SetEnabled(evt.newValue);
            });


             _collider.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                if (colliderType == Configuration.ColliderType.tileCollider)
                {
                    _tileColliderHeight.SetEnabled(true);
                    _tileColliderExtrusion.SetEnabled(true);
                    _invertCollisionWalls.SetEnabled(true);
                }
                else
                {
                    _tileColliderHeight.SetEnabled(false);
                    _tileColliderExtrusion.SetEnabled(false);
                    _invertCollisionWalls.SetEnabled(false);
                }
            });

            var _meshGenerationOverride = new Toggle();
            _meshGenerationOverride.label = "Mesh Generation Override";
            _meshGenerationOverride.tooltip = "Use this to override global mesh generation for this layer";
            _meshGenerationOverride.BindProperty(_layerSerializedObject.FindProperty("meshGenerationOverride"));
            _meshGenerationOverride.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (evt.newValue)
                {
                    _mergeTiles.SetEnabled(evt.newValue);

                    _shadowCastingMode.SetEnabled(_mergeTiles.value);
                    _objectLayer.SetEnabled(_mergeTiles.value);
                    _renderingLayer.SetEnabled(_mergeTiles.value);
                    _collider.SetEnabled(_mergeTiles.value);
                    _tileColliderHeight.SetEnabled(_mergeTiles.value);
                    _tileColliderExtrusion.SetEnabled(_mergeTiles.value);
                    _invertCollisionWalls.SetEnabled(_mergeTiles.value);

                }
                else
                {
                    _mergeTiles.SetEnabled(evt.newValue);
                    _shadowCastingMode.SetEnabled(evt.newValue);
                    _objectLayer.SetEnabled(evt.newValue);
                    _renderingLayer.SetEnabled(evt.newValue);
                    _collider.SetEnabled(evt.newValue);
                    _tileColliderHeight.SetEnabled(evt.newValue);
                    _tileColliderExtrusion.SetEnabled(evt.newValue);
                    _invertCollisionWalls.SetEnabled(evt.newValue);
                }

            });


            var _layerDropdown = new PropertyField();
            _layerDropdown.label = "Blueprint Layer";
            _layerDropdown.BindProperty(_layerSerializedObject.FindProperty("assignedBlueprintLayerGuid"));
            // var _layerDropdown = new LayerSelectDropdownElement(_asset, assignedBlueprintLayerGuid, SelectedLayer, "Blueprint Layer");


            var _orientationLayerDropdown = new PropertyField();
            _orientationLayerDropdown.BindProperty(_layerSerializedObject.FindProperty("objectOrientationLayerGuid"));

            // var _orientationLayerDropdown = new DropdownField(_layerNames, _orientationLayerIndex);
            // _orientationLayerDropdown.label = "Orientation Layer";
            // _orientationLayerDropdown.RegisterCallback<GeometryChangedEvent>(evt =>
            // {
            //    GetChoices(_asset);
            //    _orientationLayerDropdown.choices = _layerNames;
            //    objectOrientationLayerGuid = _layerGuids[_orientationLayerDropdown.index];
            // });
            //
            // _orientationLayerDropdown.RegisterValueChangedCallback(evt =>
            // {
            //     objectOrientationLayerGuid = _layerGuids[_orientationLayerDropdown.index];
            // });

            var _yRotationOffset = new PropertyField();
            _yRotationOffset.BindProperty(_layerSerializedObject.FindProperty("yRotationOffset"));

            var _invertYRotationOffset = new PropertyField();
            _invertYRotationOffset.BindProperty(_layerSerializedObject.FindProperty("invertYRotationOffset"));

            var _useOrientationLayer = new PropertyField();
            _useOrientationLayer.BindProperty(_layerSerializedObject.FindProperty("useOrientationLayer"));
            _useOrientationLayer.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _orientationLayerDropdown.SetEnabled(evt.newValue);
                _yRotationOffset.SetEnabled(evt.newValue);
                _invertYRotationOffset.SetEnabled(evt.newValue);
            });

            var _orientationLayerHelp = new HelpBox("An orientation layer is used to rotate the object so that it faces the tiles of the orientation layer.", HelpBoxMessageType.Info);

            var _layerOffset = new PropertyField();
            _layerOffset.BindProperty(_layerSerializedObject.FindProperty("layerOffset"));
            _layerOffset.label = "Global Layer Position Offset";
            // var _scaleOffset = new PropertyField();
            // _scaleOffset.BindProperty(_layerSerializedObject.FindProperty("scaleOffset"));

            var _uniformScaleMin = new PropertyField();
            _uniformScaleMin.BindProperty(_layerSerializedObject.FindProperty(nameof(uniformMinScale)));
            _uniformScaleMin.label = "Min Scale";

            var _uniformScaleMax = new PropertyField();
            _uniformScaleMax.BindProperty(_layerSerializedObject.FindProperty(nameof(uniformMaxScale)));
            _uniformScaleMax.label = "Max Scale";

            var _rndScaleMinOffset = new PropertyField();
            _rndScaleMinOffset.BindProperty(_layerSerializedObject.FindProperty(nameof(objectRNDMinScale)));
            _rndScaleMinOffset.label = "Min Scale";

            var _rndScaleMaxOffset = new PropertyField();
            _rndScaleMaxOffset.BindProperty(_layerSerializedObject.FindProperty(nameof(objectRNDMaxScale)));
            _rndScaleMaxOffset.label = "Max Scale";

            var _uniformScale = new PropertyField();
            _uniformScale.BindProperty(_layerSerializedObject.FindProperty(nameof(uniformScale)));
            _uniformScale.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (evt.newValue)
                {
                    _uniformScaleMin.style.display = DisplayStyle.Flex;
                    _uniformScaleMax.style.display = DisplayStyle.Flex;

                    _rndScaleMinOffset.style.display = DisplayStyle.None;
                    _rndScaleMaxOffset.style.display = DisplayStyle.None;
                }
                else
                {
                    _uniformScaleMin.style.display = DisplayStyle.None;
                    _uniformScaleMax.style.display = DisplayStyle.None;

                    _rndScaleMinOffset.style.display = DisplayStyle.Flex;
                    _rndScaleMaxOffset.style.display = DisplayStyle.Flex;
                }
            });

            var _useRNDScale = new PropertyField();
            _useRNDScale.BindProperty(_layerSerializedObject.FindProperty(nameof(useRndScale)));
            _useRNDScale.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _uniformScale.SetEnabled(evt.newValue);
                _rndScaleMinOffset.SetEnabled(evt.newValue);
                _rndScaleMaxOffset.SetEnabled(evt.newValue);
                _uniformScaleMin.SetEnabled(evt.newValue);
                _uniformScaleMax.SetEnabled(evt.newValue);
            });

              var _rndRotationMinOffset = new PropertyField();
            _rndRotationMinOffset.BindProperty(_layerSerializedObject.FindProperty(nameof(objectRNDMinRotation)));
            _rndRotationMinOffset.label = "Min Rotation";

              var _rndRotationMaxOffset = new PropertyField();
            _rndRotationMaxOffset.BindProperty(_layerSerializedObject.FindProperty(nameof(objectRNDMaxRotation)));
            _rndRotationMaxOffset.label = "Max Rotation";

            var _useRNDRotation = new PropertyField();
            _useRNDRotation.BindProperty(_layerSerializedObject.FindProperty(nameof(useRndRotation)));
            _useRNDRotation.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _rndRotationMinOffset.SetEnabled(evt.newValue);
                _rndRotationMaxOffset.SetEnabled(evt.newValue);
            });

            var _rndPositionOffsetRadius = new PropertyField();
            _rndPositionOffsetRadius.BindProperty(_layerSerializedObject.FindProperty(nameof(objectRNDPositionOffsetRadius)));
            _rndPositionOffsetRadius.label = "Position Offset Radius";

            var _childObjects = new PropertyField();
            _childObjects.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)));

            var _placeOnTopToggle = new PropertyField();
            _placeOnTopToggle.BindProperty(_layerSerializedObject.FindProperty("placeOnTop"));

            var _useLowestToggle = new PropertyField();
            _useLowestToggle.label = "Use Lowest Collider";
            _useLowestToggle.BindProperty(_layerSerializedObject.FindProperty("useLowest"));

            var _topOffsetField = new PropertyField();
            _topOffsetField.BindProperty(_layerSerializedObject.FindProperty("topOffset"));

            var _placeOnTopExcludedLayers = new PropertyField();
            _placeOnTopExcludedLayers.BindProperty(_layerSerializedObject.FindProperty("placeOnTopExcludedLayers"));
            _placeOnTopExcludedLayers.label = "Excluded Layers";

            var _addChildObject = new Button();
            _addChildObject.text = "Add Child Object";
            _addChildObject.RegisterCallback<ClickEvent>(evt =>
            {
                childSpawnSettings.Add(new ChildSpawnSettings());
                _layerSerializedObject.Update();
                _layerSerializedObject.ApplyModifiedProperties();

                BuildChildSpawnSettings(_layerSerializedObject);
            });

            _layerItem.Add(TileWorldCreatorUIElements.Separator("Settings"));
            _layerItem.Add(_textField);
            // _layerItem.Add(_prefab);
            _layerItem.Add(_prefabObjects);
            _layerItem.Add(_layerDropdown);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Object Orientation"));
            _layerItem.Add(_useOrientationLayer);
            _layerItem.Add(_orientationLayerHelp);
            _layerItem.Add(_orientationLayerDropdown);
            _layerItem.Add(_yRotationOffset);
            _layerItem.Add(_invertYRotationOffset);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Mesh Generation"));
            _layerItem.Add(_meshGenerationOverride);
            _layerItem.Add(_mergeTiles);
            _layerItem.Add(_shadowCastingMode);
            _layerItem.Add(_objectLayer);
            _layerItem.Add(_renderingLayer);
            _layerItem.Add(_collider);
            _layerItem.Add(_tileColliderHeight);
            _layerItem.Add(_tileColliderExtrusion);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Offset"));
            _layerItem.Add(_layerOffset);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Place On Top"));
            _layerItem.Add(_placeOnTopToggle);
            _layerItem.Add(_useLowestToggle);
            _layerItem.Add(_topOffsetField);
            _layerItem.Add(_placeOnTopExcludedLayers);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Randomization"));
            _layerItem.Add(_useRNDScale);
            _layerItem.Add(_uniformScale);
            _layerItem.Add(_uniformScaleMin);
            _layerItem.Add(_uniformScaleMax);
            _layerItem.Add(_rndScaleMinOffset);
            _layerItem.Add(_rndScaleMaxOffset);
            _layerItem.Add(_useRNDRotation);
            _layerItem.Add(_rndRotationMinOffset);
            _layerItem.Add(_rndRotationMaxOffset);
            _layerItem.Add(_rndPositionOffsetRadius);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Child Objects"));
            _layerItem.Add(_addChildObject);
            _layerItem.Add(childSpawnSettingsContainer);

            BuildChildSpawnSettings(_layerSerializedObject);

            return _layerItem;
        }

        void SelectedLayer(string _layerName, string _layerGuid)
        {
            assignedBlueprintLayerGuid = _layerGuid;
        }

        void BuildChildSpawnSettings(SerializedObject _layerSerializedObject)
        {
            childSpawnSettingsContainer.Clear();

            for (int c = 0; c < childSpawnSettings.Count; c ++)
            {
                var _index = c;
                var _itemContainer = new VisualElement();
                _itemContainer.SetPadding (4, 4, 4, 4);
                _itemContainer.style.flexDirection = FlexDirection.Row;
                _itemContainer.style.flexGrow = 1;

                var _item = new Foldout();
                _item.value = false;
                _item.style.flexGrow = 1;
                if (childSpawnSettings[c].childPrefab != null)
                {
                    _item.text = childSpawnSettings[c].childPrefab.name;
                }

                _itemContainer.Add(_item);

                var _removeButton = new Button();
                _removeButton.text = "-";
                _removeButton.style.maxHeight = 20;
                _removeButton.RegisterCallback<ClickEvent>(evt =>
                {
                    childSpawnSettings.RemoveAt(_index);
                    _layerSerializedObject.Update();
                    _layerSerializedObject.ApplyModifiedProperties();

                    BuildChildSpawnSettings(_layerSerializedObject);
                });

                _itemContainer.Add(_removeButton);

                var _prefab = new PropertyField();
                _prefab.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("childPrefab"));

                var _radius = new PropertyField();
                _radius.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("radius"));

                var _count = new PropertyField();
                _count.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("count"));


                var _uniformMinScale = new PropertyField();
                _uniformMinScale.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("uniformMinScale"));
                _uniformMinScale.label = "Min Scale";

                var _uniformMaxScale = new PropertyField();
                _uniformMaxScale.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("uniformMaxScale"));
                _uniformMaxScale.label = "Max Scale";

                var _minScale = new PropertyField();
                _minScale.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("objectRNDMinScale"));
                _minScale.label = "Min Scale";

                var _maxScale = new PropertyField();
                _maxScale.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("objectRNDMaxScale"));
                _maxScale.label = "Max Scale";

                var _uniformScale = new PropertyField();
                _uniformScale.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("uniformScale"));
                _uniformScale.RegisterCallback<ChangeEvent<bool>>(evt =>
                {
                    if (evt.newValue)
                    {
                        _uniformMinScale.style.display = DisplayStyle.Flex;
                        _uniformMaxScale.style.display = DisplayStyle.Flex;

                        _minScale.style.display = DisplayStyle.None;
                        _maxScale.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        _uniformMinScale.style.display = DisplayStyle.None;
                        _uniformMaxScale.style.display = DisplayStyle.None;

                        _minScale.style.display = DisplayStyle.Flex;
                        _maxScale.style.display = DisplayStyle.Flex;
                    }
                });

                var _minRotation = new PropertyField();
                _minRotation.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings)).GetArrayElementAtIndex(c).FindPropertyRelative("objectRNDMinRotation"));
                _minRotation.label = "Min Rotation";

                var _maxRotation = new PropertyField();
                _maxRotation.BindProperty(_layerSerializedObject.FindProperty(nameof(childSpawnSettings) ).GetArrayElementAtIndex(c).FindPropertyRelative("objectRNDMaxRotation"));
                _maxRotation.label = "Max Rotation";

                _item.Add(_prefab);
                _item.Add(_count);
                _item.Add(_radius);

                _item.Add(_uniformScale);
                _item.Add(_uniformMinScale);
                _item.Add(_uniformMaxScale);
                _item.Add(_minScale);
                _item.Add(_maxScale);
                _item.Add(_minRotation);
                _item.Add(_maxRotation);

                childSpawnSettingsContainer.Add(_itemContainer);
            }

        }
#endregion

        void GetChoices(Configuration _asset)
        {
            _layerGuids = new List<string>();
            _layerNames = new List<string>();
            // Get all available layers and cache layer name and layer guid
            for (int i = 0; i < _asset.blueprintLayerFolders.Count; i ++)
            {
                for (int j = 0; j < _asset.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                {
                    _layerNames.Add(_asset.blueprintLayerFolders[i].blueprintLayers[j].layerName);
                    _layerGuids.Add(_asset.blueprintLayerFolders[i].blueprintLayers[j].guid);
                }
            }
        }
#endif
    }
}
