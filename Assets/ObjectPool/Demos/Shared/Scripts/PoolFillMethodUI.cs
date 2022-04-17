using UnityEngine;

namespace eilume.ObjectPool.Demos.Shared
{
    public class PoolFillMethodUI : MonoBehaviour
    {
        public TMPro.TMP_Text text;

        [Space(12)]
        public APooledVersion poolUser;

        private void OnEnable()
        {
            poolUser.OnFillMethodChange += OnUpdateMethodChange;
        }

        private void OnDisable()
        {
            poolUser.OnFillMethodChange -= OnUpdateMethodChange;
        }

        private void Start() => OnUpdateMethodChange();

        public void OnUpdateMethodChange()
        {
            text.text = "Pool Fill Method: " + poolUser.FillMethod.ToString();
        }
    }
}