using System.Collections;
using UnityEngine;

namespace DontSayAnythingGuys.Utils
{
    public static class StaticCoroutine
    {
        private static StaticCoroutine_ instance;

        public static void Run(IEnumerator routine)
        {
            if (!instance)
                new GameObject("StaticCoroutine").AddComponent<StaticCoroutine_>();
            instance.StartCoroutine(routine);
        }

        private class StaticCoroutine_ : MonoBehaviour
        {
            private void Awake()
            {
                if (instance)
                {
                    DestroyImmediate(this);
                    return;
                }
                instance = this;
                DontDestroyOnLoad(this);
            }
        }
    }
}
