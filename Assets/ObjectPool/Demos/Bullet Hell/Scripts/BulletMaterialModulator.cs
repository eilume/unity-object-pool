using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace eilume.ObjectPool.Demos.BulletHell
{
    public class BulletMaterialModulator : MonoBehaviour
    {
        public Material material;

        [Space(12)]
        public Color baseColor;

        private void Update()
        {
            Color newColor = baseColor;
            newColor.r *= (Mathf.Cos(Time.time * 0.25f) + 1) * 0.375f + 0.25f;
            newColor.g *= (Mathf.Sin(Time.time * 0.2f) + 1) * 0.25f + 0.5f;
            newColor.b *= (Mathf.Sin(Time.time * 0.35f) + 1) * 0.5f;
            material.SetColor("_Color", newColor);
        }
    }
}