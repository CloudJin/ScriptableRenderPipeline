using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public class FalseColorDebugSettings
    {
        public bool falseColor = false;

        public float colorThreshold0 = 0.0f;
        public float colorThreshold1 = 2.0f;
        public float colorThreshold2 = 10.0f;
        public float colorThreshold3 = 20.0f;

        public void OnValidate()
        {
        }
    }
}
