using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{

    //public float jumpHeight = 4;
    //public float timeToJumpApex = .4f;
    public float aerialMoveSpeed;
    public float groundMoveSpeed;

    //float gravity;
    public float jumpForce;
    Vector3 velocity;
    float crouchJumpMultiplierX = 0.9f;
    float crouchJumpMultiplierY = 0.8f;
    float frictionAerial = 0f;
    float frictionDefault = 1.2f;
    float frictionCrouchMultiplier = 2.0f;
    float dragValueDefault = 2.0f;
    float dragValueCrouch = 3.0f;
    float gravityScaleDefault = 3.0f;
    float diveMultiplier = 4.0f;

    bool diving = true;
    bool airborne = false;
    bool crouching = false;
    bool lunging = false;
    bool shooting = false;
    bool invulnerable = false;

    Collider2D collider;
    Rigidbody2D rigidbody;
    Controller2D controller;
    PhysicsMaterial2D material;
    Animator animator;
    SpriteRenderer spriteRenderer;

    public LayerMask layerMask = -1; //make sure we aren't in this layer 
    public float skinWidth = 0.1f; //probably doesn't need to be changed 

    private float minimumExtent;
    private float partialExtent;
    private float sqrMinimumExtent;
    private Vector2 previousPosition;

    // Input variables
    public float thumbstickDeadZone;

    AnimatorClipInfo[] currentClipInfo;
    string clipName;
    float currentClipLength;

    void Start()
    {
        collider = GetComponent<Collider2D>();
        rigidbody = GetComponent<Rigidbody2D>();
        controller = GetComponent<Controller2D>();
        material = collider.sharedMaterial;
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        //gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        //jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        //print("Gravity: " + gravity + "  Jump Velocity: " + jumpVelocity);

        /*previousPosition = rigidbody.position;
        minimumExtent = Mathf.Min(Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.y), collider.bounds.extents.z);
        partialExtent = minimumExtent * (1.0f - skinWidth);
        sqrMinimumExtent = minimumExtent * minimumExtent;
        */
    }

    /*void FixedUpdate()
    {
        //have we moved more than our minimum extent? 
        Vector3 movementThisStep = rigidbody.position - previousPosition;
        float movementSqrMagnitude = movementThisStep.sqrMagnitude;

        if (movementSqrMagnitude > sqrMinimumExtent)
        {
            float movementMagnitude = Mathf.Sqrt(movementSqrMagnitude);
            RaycastHit hitInfo;

            //check for obstructions we might have missed 
            if (Physics.Raycast(previousPosition, movementThisStep, out hitInfo, movementMagnitude, layerMask.value))
            {
                if (!hitInfo.collider)
                    return;

                if (hitInfo.collider.isTrigger)
                    hitInfo.collider.SendMessage("OnTriggerEnter", collider);

                if (!hitInfo.collider.isTrigger)
                    rigidbody.position = hitInfo.point - (movementThisStep / movementMagnitude) * partialExtent;

            }
        }

        previousPosition = rigidbody.position;
    }*/

    void FixedUpdate()
    {
        //Fetch the current Animation clip information for the base layer
        currentClipInfo = this.animator.GetCurrentAnimatorClipInfo(0);
        //Access the Animation clip name
        clipName = currentClipInfo[0].clip.name;
        //Access the current length of the clip
        currentClipLength = currentClipInfo[0].clip.length;
        //Debug.Log(clipName);

        controller.UpdateCollisions(rigidbody.velocity);
        Debug.Log(rigidbody.velocity);

        //Additional variable to smooth changes in velocity
        float targetVelocityX;

        //Touching a wall or ceiling stops your vertical momentum (before gravity)
        if (collider.IsTouchingLayers(LayerMask.GetMask("Solid")) && controller.collisions.below)
        {
            //velocity.y = 0;
        }

        if (collider.IsTouchingLayers(LayerMask.GetMask("Solid")) && controller.collisions.below)
        {
            airborne = false;
        }
        else
        {
            airborne = true;
        }

        if(clipName == "anim_crouch" || clipName == "anim_uncrouch")
        {
            crouching = true;
        }
        else
        {
            crouching = false;
        }

        //Horizontal movement
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(inputX, inputY);

        //Movement specifics handled first:
        //-Flipping the sprite
        //-Diving
        if (Input.GetAxisRaw("Vertical") < -thumbstickDeadZone)
        {
            //If on ground, slow horizontal momentum harshly
            if (!airborne)
            {
                if (material.friction != frictionDefault * frictionCrouchMultiplier)
                {
                    material.friction = frictionDefault * frictionCrouchMultiplier;
                    // Disable then enable the collider, otherwise changes to the physics material won't be applied
                    collider.enabled = false;
                    collider.enabled = true;
                }
                rigidbody.drag = dragValueCrouch;
                diving = false;
            }
            else
            {
                //If in air, increase effect of gravity
                diving = true;
            }
        }
        else
        {
            if (material.friction != frictionDefault)
            {
                material.friction = frictionDefault;
                // Disable then enable the collider, otherwise changes to the physics material won't be applied
                collider.enabled = false;
                collider.enabled = true;
            }
            rigidbody.drag = 0f;
            diving = false;
        }

        //Jumping, after some Movement is handled
        if (Input.GetButtonDown("Jump") && !airborne)
        {
            if (!crouching)
            {
                rigidbody.AddForce(new Vector2(0, jumpForce));
            }
            else
            {
                if (spriteRenderer.flipX)
                {
                    rigidbody.AddForce(new Vector2(-jumpForce * crouchJumpMultiplierX, jumpForce * crouchJumpMultiplierY));
                }
                else
                {
                    rigidbody.AddForce(new Vector2(jumpForce * crouchJumpMultiplierX, jumpForce * crouchJumpMultiplierY));
                }
            }
        }

        //Attacking goes here
        //handleAttacks()
        //Attack Execution priority - dependent on current status/animation?
        
        
        // Figure out where to put code for hitstun, too. Need to make sprites for jumping/falling + hitstun

        if (!airborne)
        {
            targetVelocityX = input.x * groundMoveSpeed;
            if (input.x == 0f)
            {
                rigidbody.drag = dragValueDefault;
            }
        }
        else
        {
            targetVelocityX = input.x * aerialMoveSpeed;
            /*if (input.x != 0f)
            {
                targetVelocityX = input.x * aerialMoveSpeed;
            }
            else
            {
                targetVelocityX = velocity.x;
            }*/
        }

        //Changes made to actual velocity
        if (airborne)
        {
            velocity.x = targetVelocityX;
            material.friction = frictionAerial;
            Debug.Log(material.friction);
        }
        else
        {
            velocity.x = targetVelocityX;
        }
        if (Mathf.Abs(velocity.x) < 0.05f)
        {
            velocity.x = 0f;
        }

        if (diving)
        {
            rigidbody.gravityScale = gravityScaleDefault*diveMultiplier;
        }
        else
        {
            rigidbody.gravityScale = gravityScaleDefault;
        }
        //rigidbody.velocity = velocity * Time.deltaTime;
        rigidbody.AddForce(velocity);
        print(airborne);

        controller.UpdateCollisions(rigidbody.velocity);
        updateAnimations();
    }

    void updateAnimations()
    {
        //Crouching (if in-Animator parameters are met - from idle/run states only)
        if(Input.GetAxisRaw("Vertical") < -thumbstickDeadZone)
        {
            animator.SetBool("crouching", true);
        }
        else
        {
            animator.SetBool("crouching", false);
        }

        //Flipping sprite for movement
        if (Input.GetAxisRaw("Horizontal") != 0f)
        {
            animator.SetBool("movebuttonheld", true);
            if (!animator.GetBool("crouching")/* and an attack or something isn't being performed i.e. jump animation active */)
            {
                if (Input.GetAxisRaw("Horizontal") > 0)
                {
                    spriteRenderer.flipX = false;
                }
                else
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
        else
        {
            animator.SetBool("movebuttonheld", false);
        }

        //Airborne status (for animations)
        if (airborne)
        {
            animator.SetBool("airborne", true);
        }
        else
        {
            animator.SetBool("airborne", false);
        }
    }
}