using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace eilume.ObjectPool.Demos.Shared
{
    public class UsePoolUI : MonoBehaviour
    {
        public TMPro.TMP_Text text;

        [Space(12)]
        public AUsePool usePoolScript;

        private void OnEnable()
        {
            usePoolScript.onUsePoolChange += OnUsePoolChange;
        }

        private void OnDisable()
        {
            usePoolScript.onUsePoolChange -= OnUsePoolChange;
        }

        private void Start() => OnUsePoolChange();

        public void OnUsePoolChange()
        {
            text.text = "Pool Enabled: " + (usePoolScript.UsePool ? "True" : "False");
        }
    }
}