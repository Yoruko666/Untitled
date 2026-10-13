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
using GiantGrey.TileWorldCreator.Attributes;

using UnityEditor;
using UnityEngine.UIElements;


namespace GiantGrey.TileWorldCreator.UI
{
    [CustomPropertyDrawer(typeof(BlueprintLayerDropdownAttribute))]
    public class BlueprintLayerDropdown : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (BlueprintLayerDropdownAttribute)attribute;

            // Get Configuration reference via SerializedProperty

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
                var _activeGameObject = Selection.activeGameObject;
                if (_activeGameObject != null)
                {
                    var _manager = _activeGameObject.GetComponent<TileWorldCreatorManager>();
                    if (_manager != null)
                    {
                        _config = _manager.configuration;
                    }
                }
            }

            if (_config == null)
            {
                return new Label("Select a TileWorldCreatorManager or Configuration to see layers");
            }

            var container = new VisualElement();
            var dropdown = new LayerSelectDropdownElement(_config, property.stringValue, (name, guid) =>
            {
                try
                {
                    property.stringValue = guid;
                    property.serializedObject.ApplyModifiedProperties();
                }
                catch
                {
                    property = null;
                }
            }, property.displayName);

            container.Add(dropdown);
            return container;
        }
    }
}
#endif
