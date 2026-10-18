using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float jumpSpeed = 8f;
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] Vector2 deathKick = new Vector2 (5f, 5f);
    [SerializeField] GameObject bullet;
    [SerializeField] Transform gun;
    
    Vector2 moveInput;
    Rigidbody2D myRigidbody;
    Animator myAnimator;
    CapsuleCollider2D myBodyCollider;
    BoxCollider2D myFeetCollider;
    float gravityScaleAtStart;

    bool isAlive = true;
    bool hasRunningParam;
    bool hasClimbingParam;
    bool hasDyingParam;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myBodyCollider = GetComponent<CapsuleCollider2D>();
        myFeetCollider = GetComponent<BoxCollider2D>();
        if (myRigidbody != null)
        {
            gravityScaleAtStart = myRigidbody.gravityScale;
        }

        // Cache animator parameters safely
        if (myAnimator != null)
        {
            foreach (var p in myAnimator.parameters)
            {
                if (p.name == "isRunning") hasRunningParam = true;
                if (p.name == "isClimbing") hasClimbingParam = true;
                if (p.name == "Dying") hasDyingParam = true;
            }
        }

        // Auto-bind Cinemachine cameras to follow Player
        var vcams = Object.FindObjectsByType<CinemachineVirtualCameraBase>();
        foreach (var vcam in vcams)
        {
            if (vcam.Follow == null) vcam.Follow = transform;
        }
        var sdc = Object.FindAnyObjectByType<CinemachineStateDrivenCamera>();
        if (sdc != null)
        {
            if (sdc.Follow == null) sdc.Follow = transform;
            if (sdc.m_AnimatedTarget == null) sdc.m_AnimatedTarget = myAnimator;
        }
    }

    void Update()
    {
        if (!isAlive) { return; }

        float x = 0f;
        float y = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
        }
        else
        {
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
        }

        moveInput.x = x;
        moveInput.y = y;

        Run();
        FlipSprite();
        ClimbLadder();
        Jump();
        Die();
    }

    void Jump()
    {
        bool jumpPressed = false;
        if (Keyboard.current != null)
        {
            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame
                         || Keyboard.current.wKey.wasPressedThisFrame
                         || Keyboard.current.upArrowKey.wasPressedThisFrame;
        }
        else
        {
            jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        if (jumpPressed)
        {
            int groundMask = LayerMask.GetMask("Ground", "Bouncing", "Platforms");
            if (groundMask == 0) groundMask = (1 << 6) | (1 << 10); // Layer 6 Ground fallback
            if (myFeetCollider != null && myFeetCollider.IsTouchingLayers(groundMask))
            {
                myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpSpeed);
            }
        }
    }

    void Run()
    {
        Vector2 playerVelocity = new Vector2(moveInput.x * runSpeed, myRigidbody.linearVelocity.y);
        myRigidbody.linearVelocity = playerVelocity;

        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon;
        if (myAnimator != null && hasRunningParam)
        {
            myAnimator.SetBool("isRunning", playerHasHorizontalSpeed);
        }
    }

    void FlipSprite()
    {
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon;

        if (playerHasHorizontalSpeed)
        {
            transform.localScale = new Vector2(Mathf.Sign(myRigidbody.linearVelocity.x), 1f);
        }
    }

    void ClimbLadder()
    {
        if (myFeetCollider == null || !myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Climbing"))) 
        { 
            if (myRigidbody != null) myRigidbody.gravityScale = gravityScaleAtStart;
            if (myAnimator != null && hasClimbingParam) myAnimator.SetBool("isClimbing", false);
            return;
        }
        
        Vector2 climbVelocity = new Vector2(myRigidbody.linearVelocity.x, moveInput.y * climbSpeed);
        myRigidbody.linearVelocity = climbVelocity;
        myRigidbody.gravityScale = 0f;

        bool playerHasVerticalSpeed = Mathf.Abs(myRigidbody.linearVelocity.y) > Mathf.Epsilon;
        if (myAnimator != null && hasClimbingParam)
        {
            myAnimator.SetBool("isClimbing", playerHasVerticalSpeed);
        }
    }

    void Die()
    {
        if (myBodyCollider != null && myBodyCollider.IsTouchingLayers(LayerMask.GetMask("Enemies", "Hazards")))
        {
            isAlive = false;
            if (myAnimator != null && hasDyingParam) myAnimator.SetTrigger("Dying");
            if (myRigidbody != null) myRigidbody.linearVelocity = deathKick;
            // FindObjectOfType<GameSession>().ProcessPlayerDeath();
        }
    }
}