using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Senses : MonoBehaviour
{
    #region Properties
    public float DistanceTravelled { get; private set; } = 0f;

    public float DistanceToTop { get; private set; } = 0f;
    public float DistanceToBottom { get; private set; } = 0f;
    #endregion

    #region Fields
    [SerializeField] private GameObject _eyes;
    [SerializeField] private float _raycastMaxDistance = 100f;
    [SerializeField] private float _debugRaycastLifetime = 1f;
    #endregion

    private void Start ()
    {
        Debug.Assert(_eyes, "Eyes not found.", this);
    }

    // Update is called once per frame
    void Update ()
    {
        //if(!isAlive)
        //{
        //    return;
        //}

        CheckForObstacle();
    }

    /// <summary>
    /// Checks for obstacle on the top or bottom.
    /// </summary>
    private void CheckForObstacle ()
    {
        DistanceToBottom = 0f;
        DistanceToTop = 0f;

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

    //private void FixedUpdate ()
    //{
    //    if (!isAlive)
    //    {
    //        return;
    //    }

    //     MoveBasedOnDna();
    //}
}
