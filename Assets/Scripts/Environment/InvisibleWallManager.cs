using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class InvisibleWallManager : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private List<GameObject> invisibleWalls = new List<GameObject>();
    
    // Visual enhancement options
    [Header("Visual Effects")]
    [SerializeField] private float maxAlpha = 0.7f;         // Maximum opacity when visible
    [SerializeField] private bool useEmission = true;       // Use emission for more visibility
    [SerializeField] private Color emissionColor = new Color(1f, 0f, 0f, 1f); // Bright red emission
    [SerializeField] private float emissionIntensity = 2f;  // How bright the emission is
    [SerializeField] private bool useOutline = false;       // Optional: Add outline effect
    
    // Wall detection method
    [Header("Wall Detection")]
    [SerializeField] private bool useClosestPointDetection = true; // Use closest point on collider instead of center
    [SerializeField] private bool showDebugVisualization = false; // Display visual debugging
    
    // URP specific settings
    [Header("URP Settings")]
    [SerializeField] private Material urpTransparentMaterial; // Drag your URP transparent material here
    [SerializeField] private string emissionPropertyName = "_EmissionColor"; // Property name for emission in URP
    
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private Dictionary<GameObject, Material> highlightMaterials = new Dictionary<GameObject, Material>();
    private Dictionary<GameObject, float> currentAlphas = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, Collider> wallColliders = new Dictionary<GameObject, Collider>();
    private Transform player;
    
    void Start()
    {
        // Initialize wall materials
        InitializeWalls();
        
        // Register for scene load events to find player when scenes are loaded
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Try to find player in already loaded scenes
        FindPlayer();
    }
    
    void OnDestroy()
    {
        // Unregister event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When a new scene is loaded, try to find the player
        FindPlayer();
    }
    
    private void FindPlayer()
    {
        // Look for player in all loaded scenes
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
            //Debug.Log($"Found player in scene: {playerObject.scene.name}");
        }
        else
        {
            //Debug.LogWarning("Player not found in any loaded scene. Make sure it has the correct tag.");
        }
    }
    
    private void InitializeWalls()
    {
        // Initialize all walls
        foreach (GameObject wall in invisibleWalls)
        {
            if (wall == null) continue;
            
            // Cache the collider for distance calculations
            Collider wallCollider = wall.GetComponent<Collider>();
            if (wallCollider == null)
            {
                //Debug.LogWarning($"Wall {wall.name} has no collider. Adding a box collider.");
                wallCollider = wall.AddComponent<BoxCollider>();
            }
            wallColliders[wall] = wallCollider;
            
            MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                originalMaterials[wall] = renderer.material;
                
                // Create highlight material based on URP
                Material highlightMat;
                
                // Use the provided URP material if available, otherwise create one
                if (urpTransparentMaterial != null)
                {
                    highlightMat = new Material(urpTransparentMaterial);
                }
                else
                {
                    // Fallback to Universal Render Pipeline/Lit shader
                    highlightMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    highlightMat.SetFloat("_Surface", 1); // 0 = opaque, 1 = transparent
                    highlightMat.SetFloat("_Blend", 0); // 0 = alpha, 1 = premultiply, 2 = additive, 3 = multiply
                    highlightMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    highlightMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    highlightMat.SetInt("_ZWrite", 0);
                    highlightMat.renderQueue = 3000;
                }
                
                // Set the base color
                highlightMat.SetColor("_BaseColor", highlightColor);
                
                // Setup emission for better visibility if enabled
                if (useEmission)
                {
                    highlightMat.EnableKeyword("_EMISSION");
                    highlightMat.SetColor(emissionPropertyName, emissionColor * emissionIntensity);
                }
                
                // Make sure surfaces are visible from both sides
                highlightMat.SetInt("_Cull", (int)CullMode.Off); // 0 = off, 1 = front, 2 = back
                
                highlightMaterials[wall] = highlightMat;
                currentAlphas[wall] = 0f;
                
                // Apply initial state
                renderer.material = highlightMat;
                SetAlpha(wall, 0f);
            }
        }
    }
    
    void Update()
    {
        // If player is null, try to find it again
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        foreach (GameObject wall in invisibleWalls)
        {
            if (wall == null) continue;
            
            // Calculate distance to player using closest point on collider
            float distanceToPlayer;
            
            if (useClosestPointDetection && wallColliders.ContainsKey(wall))
            {
                // Get closest point on collider to player
                Vector3 closestPoint = wallColliders[wall].ClosestPoint(player.position);
                distanceToPlayer = Vector3.Distance(closestPoint, player.position);
                
                // Debug visualization
                if (showDebugVisualization)
                {
                    Debug.DrawLine(player.position, closestPoint, Color.yellow);
                }
            }
            else
            {
                // Fallback to center-based check
                distanceToPlayer = Vector3.Distance(wall.transform.position, player.position);
            }
            
            bool isPlayerNear = distanceToPlayer <= detectionRadius;
            
            // Adjust alpha based on player proximity
            float targetAlpha = isPlayerNear ? maxAlpha : 0f;
            currentAlphas[wall] = Mathf.Lerp(currentAlphas[wall], targetAlpha, Time.deltaTime * fadeSpeed);
            
            // Update material
            SetAlpha(wall, currentAlphas[wall]);
        }
    }
    
    private void SetAlpha(GameObject wall, float alpha)
    {
        if (!highlightMaterials.ContainsKey(wall)) return;
        
        Material mat = highlightMaterials[wall];
        
        // Set alpha in base color (URP shader)
        Color baseColor = mat.GetColor("_BaseColor");
        baseColor.a = alpha;
        mat.SetColor("_BaseColor", baseColor);
        
        // If using emission, adjust emission intensity based on alpha
        if (useEmission)
        {
            float emissionStrength = alpha > 0 ? 1 : 0;
            mat.SetColor(emissionPropertyName, emissionColor * emissionIntensity * emissionStrength);
        }
        
        // Only enable renderer when visible
        MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Only show when alpha is significant
            renderer.enabled = alpha > 0.01f;
        }
    }
    
    // Optional: Add a public method to manually set the player reference
    public void SetPlayerReference(Transform playerTransform)
    {
        player = playerTransform;
    }
    
    // For debugging the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        if (!showDebugVisualization) return;
        
        foreach (GameObject wall in invisibleWalls)
        {
            if (wall == null) continue;
            
            Collider collider = wall.GetComponent<Collider>();
            if (collider != null && useClosestPointDetection)
            {
                // Draw bounds of collider
                Bounds bounds = collider.bounds;
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                
                // Draw detection radius around each corner of the bounds
                Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
                DrawGizmosForBoundsCorners(bounds);
            }
            else
            {
                // Draw simple sphere for center-based detection
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireSphere(wall.transform.position, detectionRadius);
            }
        }
    }
    
    private void DrawGizmosForBoundsCorners(Bounds bounds)
    {
        // Draw detection radius visualization at each corner of the bounds
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(min.x, min.y, max.z);
        corners[2] = new Vector3(min.x, max.y, min.z);
        corners[3] = new Vector3(min.x, max.y, max.z);
        corners[4] = new Vector3(max.x, min.y, min.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(max.x, max.y, min.z);
        corners[7] = new Vector3(max.x, max.y, max.z);
        
        // Draw smaller gizmos at each corner
        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawWireSphere(corner, 0.3f);
        }
    }
}