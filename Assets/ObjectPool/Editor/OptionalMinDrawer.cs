using UnityEngine;
using UnityEditor;
using System;

namespace eilume.ObjectPool
{
    [CustomPropertyDrawer(typeof(OptionalMinAttribute))]
    public class OptionalMinDrawer : OptionalDrawer
    {
        private OptionalMinAttribute minAttribute
        {
            get => attribute as OptionalMinAttribute;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            base.OnGUI(position, property, label);

            if (EditorGUI.EndChangeCheck())
            {
                if (valueProperty.propertyType == SerializedPropertyType.Float)
                {
                    valueProperty.floatValue = Mathf.Max(minAttribute.min, valueProperty.floatValue);
                }
                else if (valueProperty.propertyType == SerializedPropertyType.Integer)
                {
                    valueProperty.intValue = Mathf.Max((int)minAttribute.min, valueProperty.intValue);
                }
            }
        }
    }
}