using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{

    public float jumpHeight = 4;
    public float timeToJumpApex = .4f;
    public float accelerationTimeAirborne = .1f;
    public float accelerationTimeGrounded = .1f;
    public float aerialMoveSpeed = 12;
    public float groundMoveSpeed = 16;

    float gravity;
    float jumpVelocity;
    Vector3 velocity;
    float velocityXSmoothing = 0.1f;
    float crouchJumpMultiplierX = 0.9f;
    float crouchJumpMultiplierY = 0.8f;
    float crouchFrictionMultiplier = 0.35f;
    float diveMultiplier = 1.3f;

    bool diving = true;
    bool airborne = false;
    bool crouching = false;
    bool lunging = false;
    bool shooting = false;
    bool invulnerable = false;

    Controller2D controller;
    Animator animator;
    SpriteRenderer spriteRenderer;

    AnimatorClipInfo[] currentClipInfo;
    string clipName;
    float currentClipLength;
    
    void Start()
    {
        controller = GetComponent<Controller2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        print("Gravity: " + gravity + "  Jump Velocity: " + jumpVelocity);
    }

    void Update()
    {
        //Fetch the current Animation clip information for the base layer
        currentClipInfo = this.animator.GetCurrentAnimatorClipInfo(0);
        //Access the Animation clip name
        clipName = currentClipInfo[0].clip.name;
        //Access the current length of the clip
        currentClipLength = currentClipInfo[0].clip.length;
        //Debug.Log(clipName);

        //Additional variable to smooth changes in velocity
        float targetVelocityX;
        
        //Touching a wall or ceiling stops your vertical momentum (before gravity)
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }

        if (controller.collisions.below)
        {
            airborne = true;
        }
        else
        {
            airborne = false;
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
        if (Input.GetAxisRaw("Vertical") < 0f)
        {
            //If on ground, slow horizontal momentum harshly
            if (controller.collisions.below)
            {
                //Debug.Log("inputX: " + input.x.ToString());
                //Debug.Log("velocityX: " + velocity.x.ToString());
                if (spriteRenderer.flipX)
                {
                    input.x = Mathf.Lerp((velocity.x/groundMoveSpeed), 0f, crouchFrictionMultiplier);
                }
                else
                {
                    input.x = Mathf.Lerp((velocity.x/groundMoveSpeed), 0f, crouchFrictionMultiplier);
                }
                //Debug.Log("crouchedX: " + input.x.ToString());
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
            diving = false;
        }

        if (controller.collisions.below)
        {
            airborne = false;
        }
        else
        {
            airborne = true;
        }

        //Jumping, after some Movement is handled
        if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below)
        {
            if (!crouching)
            {
                velocity.y = jumpVelocity;
            }
            else
            {
                velocity.y = jumpVelocity*crouchJumpMultiplierY;
                if (spriteRenderer.flipX)
                {
                    if(velocity.x < -jumpVelocity* crouchJumpMultiplierX)
                    {
                        velocity.x -= jumpVelocity* crouchJumpMultiplierX;
                    }
                    else
                    {
                        velocity.x = -jumpVelocity* crouchJumpMultiplierX;
                    }
                }
                else
                {
                    if (velocity.x > jumpVelocity* crouchJumpMultiplierX)
                    {
                        velocity.x += jumpVelocity* crouchJumpMultiplierX;
                    }
                    else
                    {
                        velocity.x = jumpVelocity*crouchJumpMultiplierX;
                    }
                }
            }
        }

        //Attacking goes here
        //handleAttacks()
        //Attack Execution priority - dependent on current status/animation?
        
        
        // Figure out where to put code for hitstun, too. Need to make sprites for jumping/falling + hitstun

        if (controller.collisions.below || controller.collisions.above)
        {
            targetVelocityX = input.x * groundMoveSpeed;
        }
        else
        {
            if (input.x != 0f || Mathf.Abs(velocity.x) < 3f)
            {
                targetVelocityX = input.x * aerialMoveSpeed;
            }
            else
            {
                targetVelocityX = velocity.x;
            }
        }

        //Changes made to actual velocity
        if (airborne)
        {
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (!airborne) ? accelerationTimeGrounded : accelerationTimeAirborne);
        }
        else
        {
            velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (!airborne) ? accelerationTimeGrounded : accelerationTimeAirborne);
        }
        if (Mathf.Abs(velocity.x) < 0.05f)
        {
            velocity.x = 0f;
        }

        if (diving)
        {
            velocity.y += gravity * Time.deltaTime * diveMultiplier;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
        controller.Move(velocity * Time.deltaTime);

        updateAnimations();
    }

    void updateAnimations()
    {
        //Crouching (if in-Animator parameters are met - from idle/run states only)
        if(Input.GetAxisRaw("Vertical") < 0f)
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