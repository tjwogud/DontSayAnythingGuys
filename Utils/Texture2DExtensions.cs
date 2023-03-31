using UnityEngine;

namespace DontSayAnythingGuys.Utils
{
    public static class Texture2DExtensions
    {
        public static Texture2D Copy(this Texture2D texture)
        {
            if (texture == null)
                return null;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, renderTexture);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D readableTexture = new Texture2D(texture.width, texture.height);
            readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            readableTexture.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readableTexture;
        }

        public static Texture2D Readable(this Texture2D texture)
        {
            return texture.isReadable ? texture : texture.Copy();
        }

        public static Texture2D FixedResize(this Texture2D texture, int width, int height)
        {
            Texture2D result = new Texture2D(width, height, texture.format, true);
            Color[] pixels = result.GetPixels(0);
            float xIncrease = 1f / width;
            float yIncrease = 1f / height;
            for (int pixel = 0; pixel < pixels.Length; pixel++)
            {
                int x = pixel % width;
                int y = (int)Mathf.Floor(pixel / width);
                pixels[pixel] = texture.GetPixelBilinear(xIncrease * x, yIncrease * y);
            }
            result.SetPixels(pixels, 0);
            result.Apply();
            return result;
        }
    }
}
