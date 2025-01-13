using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials.CameraPerLayerFarDist
{
    /// <summary>
    /// Customize camera layer culling distances, using a default distance for most layers,
    /// but allowing per-layer overrides for certain layers you need visible beyond the default.
    /// </summary>
    [ExecuteInEditMode]
    public class PerLayerFarDistance : MonoBehaviour
    {
        [System.Serializable]
        public class LayerOverride
        {
            [Tooltip("Name of the layer exactly as it appears in the Unity Editor.")]
            public string layerName;
            
            [Tooltip("Custom culling distance for this layer.")]
            public float distance;
        }

        [Header("Camera Reference")]
        [SerializeField] private Camera _camera;
        public Camera Camera => _camera;

        [Header("Default Settings")]
        [Tooltip("This distance will be applied to all layers except those that have a specific override.")]
        [SerializeField] private float _defaultCullDistance = 100f;
        
        [Tooltip("If true, uses a spherical culling mode (distance from camera center) instead of a plane-based culling.")]
        [SerializeField] private bool _useRoundFar = false;

        [Header("Per-Layer Overrides")]
        [Tooltip("Add entries for layers that need custom distances (e.g. Clouds, Mountains).")]
        [SerializeField] private List<LayerOverride> _layerOverrides = new List<LayerOverride>();

        private void OnEnable()
        {
            SetupCullings();
        }

        private void OnValidate()
        {
            SetupCullings();
        }

        private void SetupCullings()
        {
            if (_camera == null)
                return;

            // Prepare an array of culling distances for every layer (0 - 31)
            float[] distances = new float[32];

            // Set the default culling distance for all layers first
            for (int i = 0; i < distances.Length; i++)
            {
                distances[i] = _defaultCullDistance;
            }

            // Apply any overrides we might have for specific layers
            foreach (LayerOverride layerOverride in _layerOverrides)
            {
                int layerIndex = LayerMask.NameToLayer(layerOverride.layerName);
                if (layerIndex >= 0 && layerIndex < 32)
                {
                    distances[layerIndex] = layerOverride.distance;
                }
                else
                {
                    Debug.LogWarning($"Layer \"{layerOverride.layerName}\" not found or out of range (0-31).");
                }
            }

            _camera.layerCullDistances = distances;
            _camera.layerCullSpherical = _useRoundFar;
        }
    }
}
