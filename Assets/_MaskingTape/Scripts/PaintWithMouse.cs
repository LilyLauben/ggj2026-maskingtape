using UnityEngine;
using UnityEngine.InputSystem;

public class PaintWithMouse : MonoBehaviour
{
    [SerializeField]
    private Camera mainCam;
    [SerializeField]
    private Camera tapeCam;
    [SerializeField]
    private Shader paintShader;

    [SerializeField, Range(1, 500)]
    private float brushSizeX = 1;
    [SerializeField, Range(1, 500)]
    private float brushSizeY = 1;
    [SerializeField, Range(0, 1)]
    private float strength = 1;

    [SerializeField]
    private CalculateScore calc;

    private RenderTexture paintMask;
    private RenderTexture tapeMask;
    private Material wallMaterial, paintMaterial;

    private bool canPaint;

    void Start()
    {
        paintMaterial = new Material(paintShader);
        paintMaterial.SetVector("_Color", Color.red);
        paintMaterial.SetFloat("_Strength", strength);

        wallMaterial = GetComponent<MeshRenderer>().material;

        paintMask = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
        wallMaterial.SetTexture("_PaintMask", paintMask);

        tapeMask = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
        tapeMask.depth = 16;
        tapeCam.targetTexture = tapeMask;
        wallMaterial.SetTexture("_TapeMask", tapeMask);

        GameManager.instance.OnGameStateChanged += CheckPaint;
    }

    void Update()
    {
        if (canPaint && Mouse.current.leftButton.isPressed)
        {
            Vector3 position = Mouse.current.position.ReadValue();
            Ray ray = mainCam.ScreenPointToRay(position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Wall")))
            {
                //Debug.DrawRay(ray.origin, hit.point - ray.origin, Color.red);
                Debug.Log(hit.point);

                paintMaterial.SetVector("_Coordinates", new Vector4(hit.textureCoord.x, hit.textureCoord.y, 0, 0));
                paintMaterial.SetVector("_Size", new Vector4(brushSizeX, brushSizeY, 0, 0));

                RenderTexture temp = RenderTexture.GetTemporary(paintMask.width, paintMask.height, 0, RenderTextureFormat.ARGBFloat);
                Graphics.Blit(paintMask, temp);
                Graphics.Blit(temp, paintMask, paintMaterial);
                RenderTexture.ReleaseTemporary(temp);
            }
        }

    }

    private void CheckPaint(GameState _state)
    {
        canPaint = _state == GameState.PAINTING;
        if (_state == GameState.RESULTS) { wallMaterial.SetInt("_RemoveTape", 1); }
    }

    [ContextMenu("RemoveTape")]
    public void RemoveTape()
    {
        wallMaterial.SetInt("_RemoveTape", 1);
        calc.CalculateScoreFromTextures(tapeMask, paintMask, wallMaterial.GetTexture("_GoalMask"));
    }
}
