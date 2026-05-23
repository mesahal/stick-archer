using UnityEngine;

/// <summary>
/// Lightweight parallax scrolling for background layers.
/// Mobile-optimized: moves based on camera position, no per-frame texture scrolling.
/// </summary>
public class SimpleParallax : MonoBehaviour
{
    [Header("Parallax Layers")]
    public ParallaxLayer[] layers;
    
    [Header("Camera Reference")]
    public Transform cameraTransform;
    
    [Header("Settings")]
    public bool lockY = true;  // 2D games often only scroll horizontally
    
    private Vector3 lastCamPos;
    
    [System.Serializable]
    public struct ParallaxLayer
    {
        public Transform layerTransform;
        [Tooltip("0 = moves with camera, 1 = stationary (distant)")] 
        [Range(0f, 1f)] public float parallaxFactor;
        public bool tileHorizontal;
        public float spriteWidth;
    }
    
    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
        
        if (cameraTransform != null)
            lastCamPos = cameraTransform.position;
    }
    
    void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        Vector3 camDelta = cameraTransform.position - lastCamPos;
        
        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;
            
            // Parallax movement: layers move opposite to camera
            Vector3 parallax = camDelta * (1f - layer.parallaxFactor);
            
            if (lockY)
                parallax.y = 0;
            
            layer.layerTransform.position += parallax;
            
            // Optional: horizontal tiling for infinite scroll
            if (layer.tileHorizontal && layer.spriteWidth > 0)
            {
                float camX = cameraTransform.position.x;
                float layerX = layer.layerTransform.position.x;
                float offset = camX - layerX;
                
                if (Mathf.Abs(offset) > layer.spriteWidth)
                {
                    int direction = offset > 0 ? 1 : -1;
                    layer.layerTransform.position += Vector3.right * layer.spriteWidth * direction;
                }
            }
        }
        
        lastCamPos = cameraTransform.position;
    }
}
