using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    private bool isFacingRight = true;
    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;
    // Start is called before the first frame update

    [Header("Jumping")]
    public float jumpPower=10f;
    public int maxJumps = 3;
    int jumpRemaining;

    [Header("GroundCheck")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize=new Vector2(0.5f, 0.5f);
    public LayerMask groundLayer;
    bool isGrounded;

    [Header("WallCheck")]
    public Transform wallCheckPos;
    public Vector2 wallCheckSize = new Vector2(0.5f, 0.5f);
    public LayerMask wallLayer;

    [Header("WallMovement")]
    public float wallSlideSpeed =2;
    bool isWallSliding;
    bool isWallJumping;
    float wallJumpDirection;
    public float wallJumpTime=0.5f;
    float wallJumpTimer;
    public Vector2 wallJumpPower = new Vector2(5f, 10f);

    [Header("Grabity")]
    public float baseGravity =2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMultiplier = 2f;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        GroundCheck();
        ProcessGravity();
        ProcessWallSlide();
        ProcessWallJump();
        
        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontalMovement * moveSpeed, rb.velocity.y);
            Flip();
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalMovement = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (jumpRemaining>0) 
        {
            if (context.performed)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpPower);
                jumpRemaining--;
            }
            else if (context.canceled)
            {
                //light tap of the jumping buttons
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
                jumpRemaining--;

            }
        }

        if (context.performed && wallJumpTimer > 0f)
        {
            isWallJumping = true;
            rb.velocity=new Vector2(wallJumpDirection* wallJumpPower.x,wallJumpPower.y); //Jump away from wall
            wallJumpTimer = 0f;
            if (transform.localScale.x != wallJumpDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 ls = transform.localScale;
                ls.x *= -1;
                transform.localScale = ls;
            }
            Invoke(nameof(CancelWallJump), wallJumpTime + 0.1f);
        }
        
        
    }

    private void GroundCheck()
    {
        if(Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer))
        {
            jumpRemaining = maxJumps;
            isGrounded = true;
        }
        else{
            isGrounded= false;
        }

    }

    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0, wallLayer);

    }

    private void CancelWallJump()
    {
        isWallJumping= false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);
    }

    private void ProcessGravity()
    {
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = baseGravity * fallSpeedMultiplier;//make falling faster
            rb.velocity = new Vector2(rb.velocity.x , Mathf.Max(rb.velocity.y,-maxFallSpeed));
        }
        else
        {
            rb.gravityScale = baseGravity;
        }
    }

    private void ProcessWallSlide()
    {
        if (!isGrounded&&WallCheck()&&horizontalMovement!=0)
        {
            isWallSliding=true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void ProcessWallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpDirection = -transform.localScale.x;
            wallJumpTimer = wallJumpTime;
            CancelInvoke(nameof(CancelWallJump));
        }
        else if(wallJumpTimer>0f){
            wallJumpTimer -= Time.deltaTime;
        }
    }

    private void Flip()
    {
        if(!isFacingRight&&horizontalMovement>0 || isFacingRight && horizontalMovement < 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls=transform.localScale;
            ls.x *= -1;
            transform.localScale = ls;
        }

    }
}
