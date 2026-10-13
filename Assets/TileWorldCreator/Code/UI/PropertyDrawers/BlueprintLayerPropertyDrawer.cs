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

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GiantGrey.TileWorldCreator.Attributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    [CustomPropertyDrawer(typeof(BlueprintLayerAttribute))]
    public class BlueprintLayerPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var _activeGameObject = Selection.activeGameObject;

            var _attribute = attribute as BlueprintLayerAttribute;

            TileWorldCreatorManager _manager = null;
            Configuration _config = null;

            if (property.serializedObject.targetObject != null)
            {
                if (property.serializedObject.targetObject is BlueprintLayer blueprintLayer)
                {
                    _config = blueprintLayer.GetAsset();
                }
                else if (property.serializedObject.targetObject is BuildLayer buildLayer)
                {
                    _config = buildLayer.asset;
                }
                else if (property.serializedObject.targetObject is BlueprintModifier modifier)
                {
                    _config = modifier.asset;
                }
                else if (property.serializedObject.targetObject is Configuration config)
                {
                    _config = config;
                }
            }

            if (_config == null)
            {
                if (_activeGameObject != null)
                {
                    _manager = _activeGameObject.GetComponent<TileWorldCreatorManager>();
                }

                if (_manager == null)
                {
                    _manager = Object.FindAnyObjectByType<TileWorldCreatorManager>();
                }

                if (_manager != null)
                {
                    _config = _manager.configuration;
                }
            }

            if (_config == null)
            {
                return new Label("Select a TileWorldCreatorManager or Configuration to see layers");
            }

            var _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Row;

            var _isList = property.propertyType == SerializedPropertyType.Generic && property.isArray;
            var _isString = property.propertyType == SerializedPropertyType.String;

            if (!_isList && !_isString)
            {
                return new Label("BlueprintLayerAttribute can only be used on string or List<string>");
            }

            var _dropdown = new Button();
            _dropdown.style.flexGrow = 1;
            _dropdown.style.unityTextAlign = TextAnchor.MiddleLeft;

            UpdateDropdownText(_dropdown, property, _config);

            _dropdown.RegisterCallback<ClickEvent>(evt =>
            {
                List<string> _assignedGuids = new List<string>();
                List<string> _assignedNames = new List<string>();

                if (_isString)
                {
                    if (!string.IsNullOrEmpty(property.stringValue))
                    {
                        _assignedGuids.Add(property.stringValue);
                        var _layer = _config.GetBlueprintLayerByGuid(property.stringValue);
                        _assignedNames.Add(_layer != null ? _layer.layerName : "Unknown Layer");
                    }
                }
                else
                {
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var _guid = property.GetArrayElementAtIndex(i).stringValue;
                        _assignedGuids.Add(_guid);
                        var _layer = _config.GetBlueprintLayerByGuid(_guid);
                        _assignedNames.Add(_layer != null ? _layer.layerName : "Unknown Layer");
                    }
                }

                BlueprintLayerPopupSelector.ShowPanel(
                    _dropdown.worldBound.center,//GUIUtility.GUIToScreenPoint(evt.localPosition),
                    _config,
                    ref _assignedGuids,
                    ref _assignedNames,
                    new BlueprintLayerPopupSelector(),
                    () =>
                    {
                        if (_isString)
                        {
                            property.stringValue = _assignedGuids.FirstOrDefault() ?? "";
                        }
                        else
                        {
                            property.arraySize = _assignedGuids.Count;
                            for (int i = 0; i < _assignedGuids.Count; i++)
                            {
                                property.GetArrayElementAtIndex(i).stringValue = _assignedGuids[i];
                            }
                        }
                        property.serializedObject.ApplyModifiedProperties();
                        UpdateDropdownText(_dropdown, property, _config);
                    },
                    _isList
                );
            });

            var _label = new Label(_attribute.propertyName);
            _label.style.flexGrow = 1; // _label.style.width = 120; // Standard Unity label width approx
            _label.style.unityTextAlign = TextAnchor.MiddleLeft;

            _root.Add(_label);
            _root.Add(_dropdown);

            return _root;
        }

        private void UpdateDropdownText(Button _dropdown, SerializedProperty property, Configuration _config)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                if (string.IsNullOrEmpty(property.stringValue))
                {
                    _dropdown.text = "None";
                }
                else
                {
                    var _layer = _config.GetBlueprintLayerByGuid(property.stringValue);
                    _dropdown.text = _layer != null ? _layer.layerName : "Unknown Layer";
                }
            }
            else
            {
                if (property.arraySize == 0)
                {
                    _dropdown.text = "None";
                }
                else
                {
                    List<string> _names = new List<string>();
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var _guid = property.GetArrayElementAtIndex(i).stringValue;
                        var _layer = _config.GetBlueprintLayerByGuid(_guid);
                        _names.Add(_layer != null ? _layer.layerName : "Unknown Layer");
                    }
                    _dropdown.text = string.Join(", ", _names);
                }
            }
        }
    }
}
#endif
