using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    public float walkSpeed = 5000f;
    public float sprintSpeed = 75f;
    public float groundDrag = 5f;
    public float dashSpeed = 100f;

    [Header("Jump")]
    public float jumpMagnitude = 8f;//default 8 jump force
    public float jumpCooldown = 0.75f;
    public float gravityScale = 0.5f;
    bool onJump;

    [Header("Crouch")]
    public float crouchSpeed = 25f;
    public float crouchHeight = 0.5f;
    private float initialHeight;

    [Header("Ground Check")]
    public float playerHeight = 2f; //default 2
    public LayerMask groundLayer;
    bool onGround;
    [Header("Slope Check")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    [Header("Sound Effect")]
    public AudioClip walkingSound;
    public AudioClip sprintingSound;
    public AudioClip dashingSound;
    public AudioClip jumpingSound;
    public AudioClip groundingSound;
    [Header("Animator")]
    public Animator animator;

    public enum MovementState
    { 
        crouching,
        walking,
        sprinting,
        dashing,
        idle,
        inAir
    }

    public bool dashing;

    [Header("Others")]
    public Transform orientation;
    public Vector3 moveDirection;
    public Transform playerObject;
    Rigidbody rb;
    Camera cam;

    private Vector3 initialCamPos;
    public MovementState currentState;

    private float horizontalInput;
    private float verticalInput;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private MovementState lastState;

    private bool soundLimiter = true;
    //private bool keepMomentum;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();

        rb.freezeRotation = true;   //If freezeRotation is enabled, the rotation is not modified by the physics simulation. This is useful for creating first person shooters, because the player needs full control of the rotation using the mouse
        initialCamPos = cam.transform.localPosition;
        initialHeight = playerObject.transform.localScale.y;  
    }
    void FixedUpdate() //for physics calculations , use fixed update for smoother movement
    {
        playerMovement();
        applyGravity();
    }
    // Update is called once per frame
    void Update()
    {
        //player on ground check
        onGround = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);
        if (onGround)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        getInput();
        speedLimiting();
        speedController();
        debug();
        animationHandler();


        
    }

    private void getInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        jumpInput();
        crouchInput();
          
    }

    private void playerMovement()
    {
        //calculation of player movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //seperate speed when on slope
        if(onSlope() && !exitingSlope)
        {
            rb.AddForce(getSlopeMoveDirection() * moveSpeed * 1.5f, ForceMode.Force);

            if(rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        if (currentState != MovementState.idle)
            movementSound();
        rb.useGravity = !onSlope(); //turn off gravity when player climbing slope, prevent player slide down when climbing
        //seperate speed on ground and not on ground
        if(onGround)
            rb.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);
    }
    private void movementSound()
    {
        float soundInterval;
        AudioClip currentClip;
        if (currentState == MovementState.sprinting)
        {
            currentClip = sprintingSound;
            soundInterval = 0.4f;
        }
        else
        {
            currentClip = walkingSound;
            soundInterval = 0.8f;
        }
        if (!SoundManager.Instance.IsSoundPlaying(currentClip) && soundLimiter)
        {
            SoundManager.Instance.PlaySound(currentClip);
            soundLimiter = false;
            Invoke("resetSoundLimiter", soundInterval);
        }
    }
    private void resetSoundLimiter()
    {
        soundLimiter = true;
    }
    private void speedLimiting()
    {
        //limiting speed on slope
        if(onSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            //limit velocity 
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
        
    }
    private void jumpInput()
    {
        if (Input.GetButton("Jump") && !onJump && onGround)
        {
            onJump = true;
            exitingSlope = true;
            rb.velocity = new Vector3(rb.velocity.x * 0.5f, 0f, rb.velocity.z * 0.5f); //reset player jump height
            SoundManager.Instance.PlaySound(jumpingSound);
            rb.AddForce(transform.up * jumpMagnitude, ForceMode.Impulse);
            Invoke(nameof(resetJump), jumpCooldown); //invoke the method reset jump after cooldown, allow jumping continuously
            StartCoroutine(checkingGrounding());
        }
    }
    private IEnumerator checkingGrounding()
    {
        // Wait for a short time to ensure the player has left the ground
        yield return new WaitForSeconds(0.1f);

        // Check if the player is still grounded
        while (!onGround)
        {
            yield return null;
        }

        // Player has landed, play landing sound here
        SoundManager.Instance.PlaySound(groundingSound);
    }
    private void applyGravity()
    {
        // Apply custom gravity to the Rigidbody
        Vector3 gravity = gravityScale * Physics.gravity * 2f;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }
    private void resetJump()
    {
        onJump = false;
        exitingSlope = false;
    }
    private void crouchInput()
    {
        if (Input.GetButtonDown("Crouch")) //let the force only apply once
        {
            //transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z); //bug , will transform all child objects
            cam.transform.localPosition = new Vector3(initialCamPos.x, initialCamPos.y * 0.5f, initialCamPos.z);
            playerObject.transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);

            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            Debug.Log("Crouch Button Pressed");
            Debug.Log("Initial Cam Pos: " + initialCamPos);
        }
        if(Input.GetButtonUp("Crouch")) //when unpress , return to normal
        {
            //transform.localScale = new Vector3(transform.localScale.x, initialHeight, transform.localScale.z);
            cam.transform.localPosition = new Vector3(initialCamPos.x, initialCamPos.y, initialCamPos.z);
            playerObject.transform.localScale = new Vector3(transform.localScale.x, initialHeight, transform.localScale.z);
        }
    }
       
    private void speedController()
    {
        if (onGround && Input.GetButton("Crouch"))
        {
            currentState = MovementState.crouching;
            moveSpeed = crouchSpeed;

        }

        else if (onGround && Input.GetButton("Sprint") && Input.GetAxisRaw("Vertical")>0)
        {
            currentState = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        else if(onGround && moveDirection.magnitude>0)
        {
            currentState = MovementState.walking;
            moveSpeed = walkSpeed;
   
        }
        else if (onGround)
        {
            currentState = MovementState.idle;
            moveSpeed = walkSpeed;
        }
        else
        {
            currentState = MovementState.inAir;
            if (desiredMoveSpeed < sprintSpeed)
                desiredMoveSpeed = walkSpeed;
            else
                desiredMoveSpeed = sprintSpeed;
        }
        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        //if (lastState == MovementState.dashing) keepMomentum = true;


        lastDesiredMoveSpeed = desiredMoveSpeed;
        lastState = currentState;
    }

    private bool onSlope()
    {
        if(Physics.Raycast(transform.position,Vector3.down,out slopeHit,playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }
    private Vector3 getSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
    private void dashInput()
    {
        if (dashing)
        {
            currentState = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
        }
    }
    private void animationHandler()
    {
        ResetParametersToDefault();
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        //animator.SetFloat("speed", Mathf.Abs(x) + Mathf.Abs(z));

        animator.SetFloat("Vertical", z);
        animator.SetFloat("Horizontal", x);
        switch (currentState)
        {
            case MovementState.sprinting:
                animator.SetBool("Sprint",true);
                break;
            case MovementState.walking:
                animator.SetBool("Walk",true);
                break;
            case MovementState.crouching:
                animator.SetBool("Crouch",true);
                break;
            case MovementState.idle:
                animator.SetTrigger("Idle");
                break;
        }



    }
    public void ResetParametersToDefault()
    {
        if (animator != null)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;

            foreach (AnimatorControllerParameter param in parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(param.name, false);
                }
                else if (param.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(param.name, param.defaultFloat);
                }
                else if (param.type == AnimatorControllerParameterType.Int)
                {
                    animator.SetInteger(param.name, param.defaultInt);
                }
            }
        }
    }

    private void debug()
    {
       
    }
}