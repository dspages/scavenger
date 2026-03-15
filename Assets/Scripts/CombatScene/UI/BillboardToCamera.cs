using UnityEngine;

/// <summary>
/// Keeps the attached transform always facing the camera (billboard).
/// Used for the health bar Canvas so it stays screen-centric regardless of unit facing.
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        transform.rotation = _camera.transform.rotation;
    }
}
