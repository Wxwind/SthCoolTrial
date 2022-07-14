using UnityEngine;

public class PlayerTrace : MonoBehaviour
{
    private RenderTexture _splatmap;
    public Shader _drawShader;
    private Material _snowMaterial, _drawMaterial;
    public GameObject _terrain;
    public Transform[] _foot;
    public float snowRayDistance;
    private int _terrainMask;
    private RaycastHit _hit;
    [Range(1, 500)] public float _brushSize;
    [Range(0, 1)] public float _brushStrength;

    private Animator _animator;

    private void Start()
    {
        _drawMaterial = new Material(_drawShader);
        _drawMaterial.SetVector("_Color", Color.red);
        _terrainMask = LayerMask.GetMask("Terrain");
        _snowMaterial = _terrain.GetComponent<MeshRenderer>().material;
        _splatmap = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBFloat);
        _snowMaterial.SetTexture("_MaskTex", _splatmap);
        _animator = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        foreach (var foot in _foot)
        {
            if (_animator.GetBool("IsRunning")&&Physics.Raycast(foot.position, Vector3.down, out _hit,snowRayDistance,_terrainMask))
            {
                _drawMaterial.SetVector("_Coordinate", new Vector4(_hit.textureCoord.x, _hit.textureCoord.y, 0, 0));
                _drawMaterial.SetFloat("_Size", _brushSize);
                _drawMaterial.SetFloat("_Strenth", _brushStrength);
                RenderTexture temp = RenderTexture.GetTemporary(_splatmap.width, _splatmap.height, 0,
                    RenderTextureFormat.ARGBFloat);
                Graphics.Blit(_splatmap, temp);
                Graphics.Blit(temp, _splatmap, _drawMaterial);
                RenderTexture.ReleaseTemporary(temp);
            }
        }
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, 256, 256), _splatmap, ScaleMode.ScaleToFit, false, 1);
    }
}