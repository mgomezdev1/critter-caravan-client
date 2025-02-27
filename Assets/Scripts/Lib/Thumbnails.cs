using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System.IO;

public static class Thumbnails
{
    public static Texture2D GetCameraRender(Camera camera)
    {
        // Create a new RenderTexture with the same dimensions as the camera
        RenderTexture renderTexture = new(camera.pixelWidth, camera.pixelHeight, 24);
        camera.targetTexture = renderTexture;
        camera.Render();

        // Create a new Texture2D with the same dimensions as the RenderTexture
        Texture2D texture = new(camera.pixelWidth, camera.pixelHeight, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, camera.pixelWidth, camera.pixelHeight), 0, 0);
        texture.Apply();

        // Clean up
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.Destroy(renderTexture);

        return texture;
    }

    public static string ConvertToBase64(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToPNG();
        string base64String = System.Convert.ToBase64String(imageBytes);
        return $"data:image/png;base64,{base64String}";
    }

    public static string FromCamera(Camera camera)
    {
        return ConvertToBase64(GetCameraRender(camera));
    }

    public static Texture2D ParseBase64(string uri)
    {
        string base64 = uri;

        // Remove the prefix if it exists
        if (IsBase64Encoded(uri, out string rawBase64))
        {
            base64 = rawBase64;
        }

        byte[] imageBytes = GetBase64TextureBytes(base64);
        Texture2D texture = new(2, 2);
        texture.LoadImage(imageBytes);
        return texture;
    }

    public static bool IsBase64Encoded(string uri, out string base64)
    {
        // Remove the prefix if it exists
        if (uri.StartsWith("data:image/png;base64,"))
        {
            base64 = uri["data:image/png;base64,".Length..];
            return true;
        }
        base64 = string.Empty;
        return false;
    }

    public static byte[] GetBase64TextureBytes(string base64)
    {
        return System.Convert.FromBase64String(base64);
    }
}