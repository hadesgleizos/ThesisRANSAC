using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEssentials.CameraPerLayerFarDist
{
    /// <summary>
    ///
    /// </summary>
    [ExecuteInEditMode]
    public class PerLayerFarDistance : MonoBehaviour
    {
        [SerializeField] private Camera _camera; public Camera Camera { get { return (_camera); } }
        [SerializeField] private float[] _distances = new float[32];
        [SerializeField] private bool _useRoundFar = false;

        private void OnEnable()
        {
            SetupCullings();
        }

        private void SetupCullings()
        {
            if (_camera == null)
            {
                return;
            }
            _camera.layerCullDistances = _distances;
            _camera.layerCullSpherical = _useRoundFar;
        }
       
        private void OnValidate()
        {
            SetupCullings();
        }
    }
}