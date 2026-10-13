
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

using UnityEngine;
using System.Collections.Generic;
using System;
using Random = Unity.Mathematics.Random;
using System.Collections;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
#if !UNITY_6000_0_OR_NEWER
using GiantGrey.TileWorldCreator.Utilities;
#endif

namespace GiantGrey.TileWorldCreator
{
    [CreateAssetMenu(menuName = "TileWorldCreator/Configuration")]
    public class Configuration : ScriptableObject
    {
        public bool useParallel;
        public int width = 50;
        public int height = 50;

        [FormerlySerializedAs("cellSize")]
        public int cellSizeOld = 1;
        public float cellSize = 1f;
        public float lastCellSize = 1f;
        public bool mergePreviewTextures = false;

        public int clusterCellSize = 5;

        public int clusterYMultiplier { get {return 1000;}}

        public bool useGlobalRandomSeed;
        public int globalRandomSeed = 1;
        public uint currentRandomSeed;

        public bool layerChanged = false;
        public bool showGizmos = false;
        public bool showPaintGrid = false;
        public BuildLayer gizmoLayer;
        public BlueprintLayer paintLayer;
        public int brushSize;
        public string selectedPaintLayerGuid;

        // Blueprint layers
        public List<BlueprintLayer> tileMapLayers = new List<BlueprintLayer>();

        // Build layers
        public List<BlueprintLayerFolder> blueprintLayerFolders = new List<BlueprintLayerFolder>();
        public List<BuildLayerFolder> buildLayerFolders = new List<BuildLayerFolder>();

        public string selectedTileMapLayerGuid;
        public Action onAllLayersExecuted;
        public Action OnMapReady;
        public Random random;

        // Global merge settings
        public bool mergeTiles = true;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
        public LayerMask objectLayer = 0;
        public RenderingLayerMask renderingLayer = 1;
        public ColliderType colliderType = ColliderType.meshCollider;
        public float tileColliderHeight = 0f;
        public float tileColliderExtrusionHeight = 0f;
        public bool invertCollisionWalls;

        private TileWorldCreatorManager defaultManager;

        public enum ColliderType
        {
            none,
            meshCollider,
            tileCollider,
            // capsuleCollider,
        }


        public event Action OnLayerChanged;

        public void NotifyLayerChanged()
        {
            layerChanged = true;
            OnLayerChanged?.Invoke();
        }


        private void OnValidate()
        {
            if (cellSizeOld != 0)
            {
                cellSize = cellSizeOld;
                cellSizeOld = 0;
            }
        }

        public void SetManager(TileWorldCreatorManager _manager)
        {
            defaultManager = _manager;
        }

        private void OnEnable()
        {
            // Ensure duplicated assets keep correct back-references for serialization
            try
            {
                if (blueprintLayerFolders != null)
                {
                    for (int i = 0; i < blueprintLayerFolders.Count; i++)
                    {
                        var folder = blueprintLayerFolders[i];
                        if (folder == null || folder.blueprintLayers == null) continue;
                        for (int j = 0; j < folder.blueprintLayers.Count; j++)
                        {
                            var layer = folder.blueprintLayers[j];
                            if (layer == null) continue;
                            // If the layer doesn't know its owner configuration or points to another configuration, fix it
                            if (layer.GetAsset() != this)
                            {
                                layer.SetAsset(this);
                            }
                        }
                    }
                }

                if (buildLayerFolders != null)
                {
                    for (int i = 0; i < buildLayerFolders.Count; i++)
                    {
                        var folder = buildLayerFolders[i];
                        if (folder == null || folder.buildLayers == null) continue;
                        for (int j = 0; j < folder.buildLayers.Count; j++)
                        {
                            var layer = folder.buildLayers[j];
                            if (layer == null) continue;
                            if (layer.asset != this)
                            {
                                layer.asset = this;
                            }
                        }
                    }
                }
            }
            catch {}
        }

        /// <summary>
        /// Execute specific blueprint layer
        /// </summary>
        /// <param name="_layerGuid"></param>
        internal void ExecuteBlueprintLayer(string _layerGuid)
        {
            if (useGlobalRandomSeed)
            {
                random = new Random((uint)globalRandomSeed);
                UnityEngine.Random.InitState((int)globalRandomSeed);
                currentRandomSeed = (uint)globalRandomSeed;
            }
            else
            {
                currentRandomSeed = (uint)(System.DateTime.Now.Ticks % uint.MaxValue);
                UnityEngine.Random.InitState((int)currentRandomSeed);
            }

            var _layer = GetBlueprintLayerByGuid(_layerGuid);
            if (!_layer.isEnabled)
            {
                onAllLayersExecuted?.Invoke();
                return;
            }

            _layer.ExecuteLayer(this, null);

#if UNITY_EDITOR
            // if (!Application.isPlaying)
            // {
            // Update preview textures
            _layer.UpdatePreviewTexture(null);
            // }
#endif

            onAllLayersExecuted?.Invoke();
        }

        /// <summary>
        /// Execute all blueprint layers
        /// </summary>
        internal void ExecuteBlueprintLayers(TileWorldCreatorManager _manager)
        {
            if (useGlobalRandomSeed)
            {
                if (globalRandomSeed == 0) globalRandomSeed = 1;

                random = new Random((uint)globalRandomSeed);
                UnityEngine.Random.InitState((int)globalRandomSeed);
                currentRandomSeed = (uint)globalRandomSeed;
            }
            else
            {
                currentRandomSeed = (uint)(System.DateTime.Now.Ticks % uint.MaxValue);
                UnityEngine.Random.InitState((int)currentRandomSeed);
            }

            for (int i = 0; i < blueprintLayerFolders.Count; i++)
            {
                for (int j = 0; j < blueprintLayerFolders[i].blueprintLayers.Count; j++)
                {
                    if (!blueprintLayerFolders[i].blueprintLayers[j].isEnabled)
                            continue;

                    blueprintLayerFolders[i].blueprintLayers[j].ResetLayer();
                }
            }

            var _progress = 0f;
            var _totalLayers = 0;
            for (int i = 0; i < blueprintLayerFolders.Count; i++)
            {
                for (int j = 0; j < blueprintLayerFolders[i].blueprintLayers.Count; j++)
                {
                    if (!blueprintLayerFolders[i].blueprintLayers[j].isEnabled)
                            continue;

                    _totalLayers++;
                }
            }

            for (int i = 0; i < blueprintLayerFolders.Count; i++)
            {
                for (int j = 0; j < blueprintLayerFolders[i].blueprintLayers.Count; j++)
                {
                    if (!blueprintLayerFolders[i].blueprintLayers[j].isEnabled)
                            continue;

                     _progress = (float)(j) / (float)_totalLayers;
                    blueprintLayerFolders[i].blueprintLayers[j].ExecuteLayer(this, null);
                    if (_manager != null)
                    {
                        _manager.SetProgress(_progress);
                    }
                }
            }

#if UNITY_EDITOR
            if (_manager.isInspected)
            {
                // Update preview textures
                Texture2D _lastTexture = null;
                // for (int i = 0; i < tileMapLayers.Count; i++)
                // {
                //     _lastTexture = tileMapLayers[i].UpdatePreviewTexture(_lastTexture);
                // }


                for (int i = 0; i < blueprintLayerFolders.Count; i++)
                {
                    for (int j = 0; j < blueprintLayerFolders[i].blueprintLayers.Count; j++)
                    {
                        if (!blueprintLayerFolders[i].blueprintLayers[j].isEnabled)
                            continue;

                        _lastTexture = blueprintLayerFolders[i].blueprintLayers[j].UpdatePreviewTexture(_lastTexture);
                    }
                }
            }
#endif

            onAllLayersExecuted?.Invoke();

        }


        internal void ExecuteBuildLayer(string _layerGuid, TileWorldCreatorManager _manager)
        {
            HashSet<int> _affectedClusters = new HashSet<int>();

            if (_manager == null)
            {
                _manager = defaultManager;
            }
            if (_manager.lastChangedCells != null && _manager.lastChangedCells.Count > 0)
            {
                foreach (var cell in _manager.lastChangedCells)
                {
                    Vector2 adjustedPos = cell + new Vector2(0.5f, 0.5f);
                    int clusterX = Mathf.FloorToInt(adjustedPos.x / clusterCellSize);
                    int clusterY = Mathf.FloorToInt(adjustedPos.y / clusterCellSize);
                    int clusterKey = clusterX + (clusterYMultiplier * clusterY);

                    AddClusterNeighbors(_affectedClusters, clusterKey);
                }
            }

            if (useGlobalRandomSeed)
            {
                random = new Random((uint)globalRandomSeed);
                UnityEngine.Random.InitState((int)globalRandomSeed);
            }
            else
            {
                currentRandomSeed = (uint)(System.DateTime.Now.Ticks % uint.MaxValue);
                UnityEngine.Random.InitState((int)currentRandomSeed);
            }

            for (int i = 0; i < buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                {
                    if (buildLayerFolders[i].buildLayers[j].guid == _layerGuid &&
                        buildLayerFolders[i].buildLayers[j].isEnabled)
                    {
                        buildLayerFolders[i].buildLayers[j].ResetLayer(_manager);
                        buildLayerFolders[i].buildLayers[j].ExecuteLayer(this, GameObject.FindAnyObjectByType<TileWorldCreatorManager>().gameObject, _manager, _affectedClusters);

                        if (Application.isPlaying)
                        {
                            _manager.StartCoroutine(WaitForSingleLayerAndPostExecute(buildLayerFolders[i].buildLayers[j], _manager));
                        }
                        else
                        {
                            #if UNITY_EDITOR
                            GiantGrey.TileWorldCreator.Utilities.EditorCoroutines.Execute(WaitForSingleLayerAndPostExecute(buildLayerFolders[i].buildLayers[j], _manager));
                            #endif
                        }
                    }
                    else if  (buildLayerFolders[i].buildLayers[j].guid == _layerGuid &&
                        !buildLayerFolders[i].buildLayers[j].isEnabled)
                    {
                        buildLayerFolders[i].buildLayers[j].ResetLayer(_manager);
                    }
                }
            }
        }

        private void AddClusterNeighbors(HashSet<int> clusterSet, int clusterKey)
        {
            if (clusterKey < 0) return;

            clusterSet.Add(clusterKey);

            int clusterX = clusterKey % clusterYMultiplier;
            int clusterY = clusterKey / clusterYMultiplier;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    int nx = clusterX + x;
                    int ny = clusterY + y;

                    if (nx >= 0 && ny >= 0)
                    {
                        clusterSet.Add(nx + (clusterYMultiplier * ny));
                    }
                }
            }
        }

        internal void ExecuteBuildLayers(TileWorldCreatorManager _manager, bool _reset = false)
        {
            HashSet<int> _affectedClusters = new HashSet<int>();

            if (_manager == null)
            {
                _manager = defaultManager;
            }

            if (_manager.lastChangedCells != null && _manager.lastChangedCells.Count > 0)
            {
                foreach (var cell in _manager.lastChangedCells)
                {
                    int clusterX = Mathf.FloorToInt(cell.x / clusterCellSize);
                    int clusterY = Mathf.FloorToInt(cell.y / clusterCellSize);
                    int clusterKey = clusterX + (clusterYMultiplier * clusterY);

                    AddClusterNeighbors(_affectedClusters, clusterKey);
                }
            }

            if (useGlobalRandomSeed)
            {
                random = new Random((uint)globalRandomSeed);
                UnityEngine.Random.InitState((int)globalRandomSeed);
            }
            else
            {
                currentRandomSeed = (uint)(System.DateTime.Now.Ticks % uint.MaxValue);
                UnityEngine.Random.InitState((int)(System.DateTime.Now.Ticks % uint.MaxValue));
            }

            if (cellSize != lastCellSize)
            {
                _reset = true;
                lastCellSize = cellSize;
            }

            if (_reset)
            {
                for (int i = 0; i < buildLayerFolders.Count; i ++)
                {
                    for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j ++)
                    {
                        if (!buildLayerFolders[i].buildLayers[j].isEnabled)
                            continue;


                        buildLayerFolders[i].buildLayers[j].ResetLayer(_manager);
                    }
                }
            }

            for (int i = 0; i < buildLayerFolders.Count; i ++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                {
                    if (!buildLayerFolders[i].buildLayers[j].isEnabled)
                    {
                        buildLayerFolders[i].buildLayers[j].ResetLayer(_manager);
                    }
                }
            }

            var _progress = 0f;
            var _totalLayers = 0;
            for (int i = 0; i < buildLayerFolders.Count; i ++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j ++)
                {
                    if (!buildLayerFolders[i].buildLayers[j].isEnabled)
                        continue;

                    _totalLayers ++;
                }
            }

            for (int i = 0; i < buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                {
                    if (!buildLayerFolders[i].buildLayers[j].isEnabled)
                        continue;

                    if (_manager == null)
                        continue;

                    _progress = (float)(j) / (float)_totalLayers;
                    buildLayerFolders[i].buildLayers[j].ExecuteLayer(this, _manager.gameObject, _manager, _affectedClusters);

                    _manager.SetProgress(_progress);
                }
            }

            if (Application.isPlaying)
            {
                if (_manager == null)
                {
                    return;
                }
                _manager.StartCoroutine(WaitForLayersAndPostExecute(_manager));
            }
            else
            {
                #if UNITY_EDITOR
                GiantGrey.TileWorldCreator.Utilities.EditorCoroutines.Execute(WaitForLayersAndPostExecute(_manager));
                #endif
            }

            if (Application.isPlaying)
            {
                if (_manager == null)
                {
                    return;
                }
                _manager.StartCoroutine(LateExecution(_manager));
            }
            else
            {
                #if UNITY_EDITOR
                GiantGrey.TileWorldCreator.Utilities.EditorCoroutines.Execute(LateExecution(_manager));
                #endif
            }
        }

        IEnumerator WaitForSingleLayerAndPostExecute(BuildLayer _layer, TileWorldCreatorManager _manager)
        {
            while (_layer.isExecuting)
            {
                yield return null;
            }

            if (_manager != null)
            {
                _layer.PostExecuteLayer(this, _manager.gameObject, _manager);
            }
        }

        IEnumerator WaitForLayersAndPostExecute(TileWorldCreatorManager _manager)
        {
            bool anyExecuting = true;
            while (anyExecuting)
            {
                anyExecuting = false;
                for (int i = 0; i < buildLayerFolders.Count; i++)
                {
                    for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                    {
                        if (buildLayerFolders[i].buildLayers[j].isEnabled && buildLayerFolders[i].buildLayers[j].isExecuting)
                        {
                            anyExecuting = true;
                            break;
                        }
                    }
                    if (anyExecuting) break;
                }
                yield return null;
            }

            for (int i = 0; i < buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                {
                    var layer = buildLayerFolders[i].buildLayers[j];
                    if (!layer.isEnabled)
                        continue;
                    if (_manager == null)
                        continue;

                    layer.PostExecuteLayer(this, _manager.gameObject, _manager);

                    // After each PostExecuteLayer, wait one frame to ensure
                    // Physics bakes if necessary (e.g. MeshColliders)
                    yield return null;
                }
            }
        }

        IEnumerator LateExecution(TileWorldCreatorManager _manager)
        {
            if (_manager == null)
                yield break;

            yield return new WaitForSeconds(0.01f);
            if (_manager.lateUpdateLayers != null)
            {
                var _layersToUpdate = new List<BuildLayer>(_manager.lateUpdateLayers);

                HashSet<int> _lateAffectedClusters = null;
                if (_manager.lastChangedCells != null && _manager.lastChangedCells.Count > 0)
                {
                    _lateAffectedClusters = new HashSet<int>();
                    foreach (var cell in _manager.lastChangedCells)
                    {
                        Vector2 adjustedPos = cell + new Vector2(0.5f, 0.5f);
                        int clusterX = Mathf.FloorToInt(adjustedPos.x / clusterCellSize);
                        int clusterY = Mathf.FloorToInt(adjustedPos.y / clusterCellSize);
                        int clusterKey = clusterX + (clusterYMultiplier * clusterY);

                        AddClusterNeighbors(_lateAffectedClusters, clusterKey);
                    }
                }

                foreach(var _layer in _layersToUpdate)
                {
                    _layer.isLateExecuting = true;
                    _layer.ExecuteLayer(this, _manager.gameObject, _manager, _lateAffectedClusters);

                    // Wait for each late layer to finish execution
                    while (_layer.isExecuting)
                    {
                        yield return null;
                    }
                    _layer.isLateExecuting = false;
                }

                foreach (var _layer in _layersToUpdate)
                {
                    _layer.PostExecuteLayer(this, _manager.gameObject, _manager);
                    yield return null;
                }

                _manager.lateUpdateLayers.Clear();
                _manager.lastChangedCells.Clear();
            }
        }

        /// <summary>
        /// Return layer guid by its layer name
        /// </summary>
        /// <param name="_layerName"></param>
        /// <returns></returns>
        public string GetBlueprintLayerGuid(string _layerName)
        {
            for (int i = 0; i < blueprintLayerFolders.Count; i++)
            {
                for (int j = 0; j < blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                {
                    if (blueprintLayerFolders[i].blueprintLayers[j].layerName == _layerName)
                    {
                        return blueprintLayerFolders[i].blueprintLayers[j].guid;
                    }
                }
            }


            return string.Empty;
        }

        public string GetBuildLayerGuid(string _layerName)
        {
            for (int i = 0; i < buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                {
                    if (buildLayerFolders[i].buildLayers[j].layerName == _layerName)
                    {
                        return buildLayerFolders[i].buildLayers[j].guid;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Get blueprint layer by its layer guid
        /// </summary>
        /// <param name="_layerGuid"></param>
        /// <returns></returns>
        public BlueprintLayer GetBlueprintLayerByGuid(string _layerGuid)
        {
            for (int i = 0; i < blueprintLayerFolders.Count; i++)
            {
                for (int j = 0; j < blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                {
                    if (blueprintLayerFolders[i].blueprintLayers[j].guid == _layerGuid)
                    {
                        return blueprintLayerFolders[i].blueprintLayers[j];
                    }
                }
            }
            return null;
        }


        public BuildLayer GetBuildLayerByGuid(string _layerGuid)
        {
            for (int i = 0; i < buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < buildLayerFolders[i].buildLayers.Count; j++)
                {
                    if (buildLayerFolders[i].buildLayers[j].guid == _layerGuid)
                    {
                        return buildLayerFolders[i].buildLayers[j];
                    }
                }
            }
            return null;
        }

        public List<BlueprintLayer> GetBlueprintLayerFolder(string _folderName)
        {
            for (int i = 0; i < blueprintLayerFolders.Count; i++)
            {
                if (blueprintLayerFolders[i].folderName == _folderName)
                {
                    return blueprintLayerFolders[i].blueprintLayers;
                }
            }
            return null;
        }
    }
}
