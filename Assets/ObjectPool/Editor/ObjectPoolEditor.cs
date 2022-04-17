using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;

namespace eilume.ObjectPool
{
    [CustomEditor(typeof(ObjectPool<>), true)]
    [CanEditMultipleObjects]
    public class ObjectPoolEditor : Editor
    {
        // TODO: figure out how to store editor group visible state semi-persistently

        protected class PropertyData
        {
            public enum EnableMode
            {
                Always,
                InPlayMode,
                NotInPlayMode,
                Conditional
            }

            public string name;
            public Optional<string> header;
            public Optional<string> label;
            public EnableMode enableMode;
            public EnableModeCondition enableModeCondition;
            public bool space;

            public delegate bool EnableModeCondition();

            public PropertyData(string name) => this.name = name;

            public PropertyData AddHeader(string header)
            {
                this.header = new Optional<string>(header);
                return this;
            }

            public PropertyData OverrideLabel(string label)
            {
                this.label = new Optional<string>(label);
                return this;
            }

            public PropertyData AddSpace()
            {
                this.space = true;
                return this;
            }

            public PropertyData SetEnableMode(EnableMode enableMode, EnableModeCondition condition = null)
            {
                this.enableMode = enableMode;
                if (enableMode == EnableMode.Conditional) this.enableModeCondition = condition;
                return this;
            }

            public bool GetEnableState()
            {
                if (this.enableMode == EnableMode.Conditional)
                {
                    return enableModeCondition();
                }

                if (this.enableMode == EnableMode.Always) return true;

                if (Application.isPlaying)
                {
                    if (this.enableMode == EnableMode.InPlayMode) return true;
                    else return false;
                }
                else
                {
                    if (this.enableMode == EnableMode.NotInPlayMode) return true;
                    else return false;
                }
            }
        }

        protected class EditorGroup
        {
            public string id;
            public string label;
            public AnimBool visible;
            public List<PropertyData> properties;
            public Vector2 startPos;

            public EditorGroup(string id, string label, bool visible)
            {
                this.id = id;
                this.label = label;
                this.visible = new AnimBool(visible);
                this.properties = new List<PropertyData>();
            }

            public PropertyData Register(string name, bool isField = false)
            {
                if (isField) name = $"<{name}>k__BackingField";

                if (properties.FindIndex((x) => x.name == name) != -1)
                {
                    Debug.LogWarning("Editor group, `" + id + "` has already registered property with name, `" + name + "`!");
                    return null;
                }

                PropertyData property = new PropertyData(name);

                properties.Add(property);

                return property;
            }
        }

        protected static EditorGroup fillSettingsGroup;
        protected static EditorGroup infoGroup;
        protected static EditorGroup statsGroup;

        protected EditorGroup[] _editorGroups;
        protected int _editorGroupCount;

        protected const int MAX_EDITOR_GROUPS = 16;

        private void OnEnable()
        {
            _editorGroups = new EditorGroup[MAX_EDITOR_GROUPS];
            _editorGroupCount = 0;

            CreateEditorGroups();

            ShrinkEditorGroups();
        }

        private void OnDisable()
        {
            for (int i = 0; i < _editorGroupCount; i++)
            {
                _editorGroups[i].visible.valueChanged.RemoveListener(Repaint);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RenderGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected bool IsMonoFill()
        {
            return !Application.isPlaying && serializedObject.FindProperty("fillTrigger").enumValueIndex != (int)ObjectPool<int>.FillTrigger.Manual;
        }

        protected bool IsFillAsync()
        {
            return serializedObject.FindProperty("fillMethod").enumValueIndex == (int)ObjectPool<int>.FillMethod.Async;
        }

        protected virtual void CreateEditorGroups()
        {
            fillSettingsGroup = CreateEditorGroup("fillSettings", "Fill Settings", true);
            infoGroup = CreateEditorGroup("info", "Info", false, MAX_EDITOR_GROUPS - 2);
            statsGroup = CreateEditorGroup("stats", "Stats", false, MAX_EDITOR_GROUPS - 1);

            fillSettingsGroup.Register("fillTrigger").AddHeader("Fill Settings");
            fillSettingsGroup.Register("initialPoolSize").SetEnableMode(PropertyData.EnableMode.Conditional, IsMonoFill);
            fillSettingsGroup.Register("fillMethod");
            fillSettingsGroup.Register("_asyncTiming").AddHeader("Async Fill Settings").OverrideLabel("Fill Timing").SetEnableMode(PropertyData.EnableMode.Conditional, IsFillAsync);
            fillSettingsGroup.Register("asyncTarget").OverrideLabel("Fill Rate Target").SetEnableMode(PropertyData.EnableMode.Conditional, IsFillAsync);
            // TODO: change label depending on fillAsyncTarget
            fillSettingsGroup.Register("_targetFill").AddHeader("Async Fill Amount Settings").OverrideLabel("Target Fill Rate").SetEnableMode(PropertyData.EnableMode.Conditional, IsFillAsync);
            // TODO: change label depending on fillAsyncTarget
            fillSettingsGroup.Register("_firstFrameFill").OverrideLabel("First Frame Fill Rate").SetEnableMode(PropertyData.EnableMode.Conditional, IsFillAsync);
            // TODO: change label depending on fillAsyncTarget
            fillSettingsGroup.Register("_minFill").OverrideLabel("Minimum Fill Rate").SetEnableMode(PropertyData.EnableMode.Conditional, IsFillAsync);
            fillSettingsGroup.Register("_targetFrameRate").AddSpace().SetEnableMode(PropertyData.EnableMode.Conditional, IsFillAsync);

            infoGroup.Register("PoolCapacity", true);
            infoGroup.Register("PooledCount", true).AddHeader("Count Values");
            infoGroup.Register("ActiveCount", true);
            infoGroup.Register("InactiveCount", true);
            infoGroup.Register("IsSetup", true).AddHeader("Bool Values");
            infoGroup.Register("IsEmpty", true);
            infoGroup.Register("IsFull", true);
            infoGroup.Register("IsAsyncCurrentlyFilling", true).AddHeader("Async Fill Values");
            infoGroup.Register("CurrentFill", true);

            statsGroup.Register("FillTimeTotal", true);
            statsGroup.Register("AsyncFillTicks", true);
            statsGroup.Register("AsyncFillTimePerTick", true);
        }

        protected EditorGroup CreateEditorGroup(string id, string label, bool visible, int index = -1)
        {
            if (index == -1)
            {
                // No index given, find first free index
                for (int i = 0; i < _editorGroups.Length; i++)
                {
                    if (_editorGroups[i] == null)
                    {
                        index = i;
                        break;
                    }
                }
            }

            if (index == -1 || _editorGroupCount == MAX_EDITOR_GROUPS || index >= MAX_EDITOR_GROUPS)
            {
                Debug.LogError("Too many editor groups! Increase the `MAX_EDITOR_GROUPS` value or lower the index to insert the editor group into.");
                return null;
            }

            EditorGroup group = new EditorGroup(id, label, visible);
            group.visible.valueChanged.AddListener(Repaint);

            _editorGroups[index] = group;
            _editorGroupCount++;

            return group;
        }

        protected void ShrinkEditorGroups()
        {
            int lastFilledIndex = -1;
            for (int i = 0; i < _editorGroups.Length; i++)
            {
                if (_editorGroups[i] != null)
                {
                    _editorGroups[++lastFilledIndex] = _editorGroups[i];
                }
            }
        }

        protected void RenderGUI()
        {
            for (int i = 0; i < _editorGroupCount; i++)
            {
                if (i != 0) EditorGUILayout.Space();

                _editorGroups[i].visible.target = EditorGUILayout.ToggleLeft("Show " + _editorGroups[i].label, _editorGroups[i].visible.target);

                _editorGroups[i].startPos = GUILayoutUtility.GetLastRect().position;

                if (EditorGUILayout.BeginFadeGroup(_editorGroups[i].visible.faded))
                {
                    EditorGUI.indentLevel++;

                    // Properties
                    for (int j = 0; j < _editorGroups[i].properties.Count; j++)
                    {
                        PropertyData property = _editorGroups[i].properties[j];
                        GUI.enabled = property.GetEnableState();

                        if (j == 0 && !property.header.enabled)
                        {
                            GUILayout.Space(6);
                        }
                        else if (property.header.enabled)
                        {
                            Rect position;

                            // If first element, we have to use the `startPos` with an offset
                            if (j == 0)
                            {
                                // Checks if expanding/collapsing since the `startPos` is then different
                                if (_editorGroups[i].visible.faded != 1)
                                {
                                    position = new Rect(_editorGroups[i].startPos.x - 18, _editorGroups[i].startPos.y - 44 - (i * 28), 250, 32);
                                }
                                else
                                {
                                    position = new Rect(_editorGroups[i].startPos.x, _editorGroups[i].startPos.y - 20, 250, 32);
                                }
                            }
                            else
                            {
                                position = GUILayoutUtility.GetLastRect();
                            }

                            // Tries to emulate the style of Unity's `HeaderAttribute`
                            position.y += position.size.y;
                            position.y += 8;
                            position = EditorGUI.IndentedRect(position);
                            GUI.Label(position, property.header.value, EditorStyles.boldLabel);
                            GUILayout.Space(24);
                        }

                        if (property.space) GUILayout.Space(12);

                        if (property.label.enabled)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(property.name), new GUIContent(property.label.value), true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(property.name), true);
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFadeGroup();
            }
        }
    }
}