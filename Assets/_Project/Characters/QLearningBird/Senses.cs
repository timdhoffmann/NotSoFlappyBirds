using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Senses : MonoBehaviour
{
    #region Properties
    public double DistanceToTop { get; private set; } = 0d;
    public double DistanceToBottom { get; private set; } = 0d;
    #endregion

    #region Fields
    [SerializeField] private GameObject _eyes;
    [SerializeField] private float _raycastMaxDistance = 100f;
    [SerializeField] private float _debugRaycastLifetime = 0f;
    #endregion

    private void Start ()
    {
        Debug.Assert(_eyes, "Eyes not found.", this);
    }

    /// <summary>
    /// Checks for obstacle on the top or bottom.
    /// </summary>
    public void CheckForObstacle ()
    {
        DistanceToBottom = 0d;
        DistanceToTop = 0d;

        Vector2 origin = _eyes.transform.position;
        Vector2 up = _eyes.transform.up;
        
        // Debug raycasts.
        Debug.DrawRay(origin, up * _raycastMaxDistance, Color.red, _debugRaycastLifetime);
        Debug.DrawRay(origin, -up * _raycastMaxDistance, Color.red, _debugRaycastLifetime);

        // Raycast up.
        RaycastHit2D hit = Physics2D.Raycast(origin, up, _raycastMaxDistance);
        if (hit.collider)
        {
            if (hit.collider.gameObject.tag == "top")
            {
                DistanceToTop = hit.distance;
            }
        }

        // Raycast down.
        hit = Physics2D.Raycast(origin, -up, _raycastMaxDistance);
        if (hit.collider)
        {
            if (hit.collider.gameObject.tag == "bottom")
            {
                DistanceToBottom = hit.distance;
            }
        }
    }
}
