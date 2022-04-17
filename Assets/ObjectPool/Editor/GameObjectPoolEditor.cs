using UnityEditor;

namespace eilume.ObjectPool
{
    [CustomEditor(typeof(GameObjectPool))]
    [CanEditMultipleObjects]
    public class GameObjectPoolEditor : ObjectPoolEditor
    {
        protected EditorGroup gameObjectSettingsGroup;

        protected bool IsMonoHook()
        {
            return serializedObject.FindProperty("useMonoHook").boolValue;
        }

        protected override void CreateEditorGroups()
        {
            base.CreateEditorGroups();

            gameObjectSettingsGroup = CreateEditorGroup("gameObjectSettings", "Game Object Settings", true);

            gameObjectSettingsGroup.Register("_objectToPool").AddHeader("Game Object Pool Settings");
            gameObjectSettingsGroup.Register("objectInitialActiveState");
            gameObjectSettingsGroup.Register("nodeEnablesGameObject").AddSpace();
            gameObjectSettingsGroup.Register("nodeDisablesGameObject");
            gameObjectSettingsGroup.Register("useMonoHook").AddHeader("Mono Hook Settings");
            gameObjectSettingsGroup.Register("monoDisablesNode").SetEnableMode(PropertyData.EnableMode.Conditional, IsMonoHook);

            infoGroup.Register("CurrentlyPooledObject", true).AddHeader("Game Object Pool Values");
        }

        // TODO: add object to pool preview
    }
}
