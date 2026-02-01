using UnityEngine;

public class CalculateScore : MonoBehaviour
{
    [SerializeField]
    private Material calcMaterial;

    private CustomRenderTexture calcTexture;

    public void CalculateScoreFromTextures(RenderTexture _tapeMask, RenderTexture _paintMask, Texture _goalMask)
    {
        // Commented out bc not working yet
        // calcMaterial.SetTexture("_TapeMask", _tapeMask);
        // calcMaterial.SetTexture("_PaintMask", _paintMask);
        // calcMaterial.SetTexture("_GoalMask", _goalMask);
        // calcTexture = new CustomRenderTexture(1024, 1024)
        // {
        //     enableRandomWrite = true,
        //     material = calcMaterial,
        //     updateMode = CustomRenderTextureUpdateMode.OnLoad
        // };
        // calcTexture.Create();

        //float percent = CalculateWhitePixelPercentage(calcTexture);
    }


    // private float CalculateWhitePixelPercentage(RenderTexture _rt)
    // {
    //     Texture2D texture = new Texture2D(_rt.width, _rt.height);
    //     texture.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0);
    //     texture.Apply();
    //     // Get all pixels from the texture
    //     Color32[] pixels = texture.GetPixels32();
    //     int totalPixels = pixels.Length;
    //     int whitePixelCount = 0;

    //     // Define a threshold for white (pure white is 255)
    //     byte threshold = 250;

    //     foreach (Color32 color in pixels)
    //     {
    //         // Check any channel since we know texture is b&w
    //         if (color.r >= threshold)
    //         {
    //             whitePixelCount++;
    //         }
    //     }

    //     // Calculate the percentage
    //     if (totalPixels > 0)
    //     {
    //         return (float)whitePixelCount / totalPixels * 100f;
    //     }
    //     else
    //     {
    //         return 0f;
    //     }
    // }
}
