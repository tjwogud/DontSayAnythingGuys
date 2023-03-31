using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace DontSayAnythingGuys.Utils
{
    public class WebTexture
    {
        public string Url { get; private set; }
        public Texture2D Texture { get; private set; }
        public bool Loaded { get; private set; } = false;
        public bool Failed { get; private set; } = false;
        
        public WebTexture(string url, Func<Texture2D, Texture2D> onLoad = null)
        {
            Url = url;
            if (url == null)
            {
                Failed = true;
                return;
            }
            StaticCoroutine.Run(LoadTextureCo(onLoad));
        }

        private IEnumerator LoadTextureCo(Func<Texture2D, Texture2D> onLoad)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(Url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Failed = true;
                    yield break;
                }

                Texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Loaded = true;

                if (onLoad != null)
                    Texture = onLoad.Invoke(Texture);
            }
        }
    }
}
