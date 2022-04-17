using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace eilume.ObjectPool.Demos.Shared
{
    public class FramesPerSecondText : MonoBehaviour
    {
        public TMPro.TMP_Text text;

        private WaitForSecondsRealtime seconds;

        private float framesPerSecond;

        private float[] frameTimes;
        private int frameTimeIndex;
        private int frameTimesCount;

        private const int MAX_FRAME_TIMES = 30;

        private void Start()
        {
            frameTimes = new float[MAX_FRAME_TIMES];
            frameTimeIndex = 0;
            frameTimesCount = 0;
            seconds = new WaitForSecondsRealtime(0.5f);
            StartCoroutine("UpdateText");
        }

        private void Update()
        {
            frameTimes[frameTimeIndex] = Time.unscaledDeltaTime;
            frameTimeIndex = (frameTimeIndex + 1) % MAX_FRAME_TIMES;

            if (frameTimesCount < MAX_FRAME_TIMES) frameTimesCount++;
        }

        private IEnumerator UpdateText()
        {
            while (true)
            {
                framesPerSecond = 0;

                for (int i = 0; i < frameTimesCount; i++)
                {
                    framesPerSecond += frameTimes[i];
                }

                framesPerSecond /= frameTimesCount;
                float frameDurationInMs = (framesPerSecond * 1000);
                framesPerSecond = 1.0f / framesPerSecond;

                text.text = "FPS: " + framesPerSecond.ToString("F1") + " <size=18>" + frameDurationInMs.ToString("F2") + "ms</size>";
                yield return seconds;
            }
        }
    }
}