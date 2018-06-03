using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brain : MonoBehaviour
{

    #region Properties

    public Dna Dna { get; private set; }
    public int Crashes { get; private set; } = 0;
    public float DistanceTravelled { get; private set; } = 0;
    #endregion

    #region Fields
    [SerializeField] private GameObject eyes;
    [SerializeField] private float raycastMaxDistance = 1f;
    [SerializeField] private float debugRaycastLifetime = 1f;
    [SerializeField] private float moveSpeed = 0.1f;
    [SerializeField] private bool canSeeTop = false;
    [SerializeField] private bool canSeeBottom = false;
    [SerializeField] private bool canSeeUpWall = false;
    [SerializeField] private bool canSeeDownWall = false;

    private int DnaLength = 5;
    private Vector2 startPosition;
    private Rigidbody2D rb;
    private bool isAlive = true;
    private float timeAlive = 0f;
    #endregion

    #region Public Methods

    public void Init ()
    {
        // Initialize Dna.
        // Gene 0 = forward.
        // Gene 1 = UpWall.
        // Gene 2 = DownWall.
        // Gene 3 = normal upward.
        Dna = new Dna(DnaLength, 200);

        // Randomize initial position.
        this.transform.Translate(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
        startPosition = this.transform.position;
    }
    #endregion

    private void Start ()
    {
        startPosition = this.transform.position;
        rb = GetComponent<Rigidbody2D>();
        Debug.Assert(rb, "RigidBody2D not found.", this);
        Debug.Assert(eyes, "Eyes not found.", this);
    }

    // Update is called once per frame
    void Update ()
    {
        if(!isAlive)
        {
            return;
        }

        CheckForObstacle();
    }

    private void CheckForObstacle ()
    {
        canSeeBottom = false;
        canSeeDownWall = false;
        canSeeTop = false;
        canSeeUpWall = false;

        Vector2 origin = eyes.transform.position;
        Vector2 forward = eyes.transform.right;
        Vector2 up = eyes.transform.up;
        
        // Debug raycasts.
        Debug.DrawRay(origin, forward * raycastMaxDistance, Color.red, debugRaycastLifetime);
        Debug.DrawRay(origin, up * raycastMaxDistance, Color.red, debugRaycastLifetime);
        Debug.DrawRay(origin, -up * raycastMaxDistance, Color.red, debugRaycastLifetime);
        
        // Raycast forward.
        RaycastHit2D hit = Physics2D.Raycast(origin, forward, raycastMaxDistance);
        if (hit.collider)
        {
            if(hit.collider.gameObject.tag == "upwall")
            {
                canSeeUpWall = true;
            }
            else if(hit.collider.gameObject.tag == "downwall")
            {
                canSeeDownWall = true;
            }
        }

        // Raycast up.
        hit = Physics2D.Raycast(origin, up, raycastMaxDistance);
        if (hit.collider)
        {
            if (hit.collider.gameObject.tag == "top")
            {
                canSeeTop = true;
            }
        }

        // Raycast down.
        hit = Physics2D.Raycast(origin, -up, raycastMaxDistance);
        if (hit.collider)
        {
            if (hit.collider.gameObject.tag == "bottom")
            {
                canSeeBottom = true;
            }
        }
        timeAlive = PopulationManager.TimeElapsed;
    }

    private void FixedUpdate ()
    {
        if (!isAlive)
        {
            return;
        }

        MoveBasedOnDna();
    }

    private void MoveBasedOnDna ()
    {
        float upForce = 0f;
        float forwardForce = 1f;

        if (canSeeUpWall)
        {
            upForce = Dna.Genes[0];
        }
        else if(canSeeDownWall)
        {
            upForce = Dna.Genes[1];
        }
        else if (canSeeTop)
        {
            upForce = Dna.Genes[2];
        }
        else if (canSeeBottom)
        {
            upForce = Dna.Genes[3];
        }
        else
        {
            upForce = Dna.Genes[4];
        }

        rb.AddForce(this.transform.right * forwardForce);
        rb.AddForce(this.transform.up * upForce * moveSpeed);

        DistanceTravelled = Vector2.Distance(startPosition, this.transform.position);
    }

    private void OnCollisionEnter2D (Collision2D collision)
    {
        if (collision.gameObject.tag == "dead")
        {
            isAlive = false;
        }
        else
        {
            Crashes++;
        }
    }
}
