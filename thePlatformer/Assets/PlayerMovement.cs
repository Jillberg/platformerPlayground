using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    private bool isFacingRight = true;
    public Animator animator;
    BoxCollider2D playerCollider;
    public ParticleSystem smokeFX;

    [Header("Movement")]
    public float moveSpeed = 5f;
    float horizontalMovement;

    [Header("Dashing")]
    public float dashSpeed=20f;
    public float dashDuration=0.1f;
    public float dashCooldown = 0.1f;
    bool isDashing;
    bool canDash = true;
    TrailRenderer trailRenderer;

    [Header("Jumping")]
    public float jumpPower=10f;
    public int maxJumps = 2;
    int jumpRemaining;

    [Header("GroundCheck")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize=new Vector2(0.5f, 0.5f);
    public LayerMask groundLayer;
    bool isGrounded;
    bool isOnPlatForm;
    public float droppingTime = 0.25f;

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

    [Header("Gravity")]
    public float baseGravity =2f;
    public float maxFallSpeed = 18f;
    public float fallSpeedMultiplier = 2f;

    void Start()
    {
        playerCollider = GetComponent<BoxCollider2D>();
        trailRenderer = GetComponent<TrailRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("yVelocity", rb.velocity.y);
        animator.SetFloat("magnitude", rb.velocity.magnitude);
        animator.SetBool("isWallSliding", isWallSliding);
        if(isDashing)
        {
            return;
        }
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

    public void Dash(InputAction.CallbackContext context)
    {
        if(context.performed && canDash)
        {
            StartCoroutine(DashCoroutine());
        }
    }

    private IEnumerator DashCoroutine()
    {
        canDash = false;
        isDashing = true;
        trailRenderer.emitting = true;
        float dashDirection=isFacingRight?1f:-1f;
        rb.velocity=new Vector2(dashDirection*dashSpeed,rb.velocity.y);

        yield return new WaitForSeconds(dashDuration);

        rb.velocity=new Vector2(0f,rb.velocity.y);
        isDashing = false;
        trailRenderer.emitting = false;

        yield return new WaitForSeconds(dashCooldown);

        yield return new WaitUntil(() => isGrounded);

        canDash=true;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (jumpRemaining>0) 
        {
            if (context.performed)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpPower);
                jumpRemaining--;
                Debug.Log("high"+jumpRemaining);
                JumpFX();
            }
            else if (context.canceled)
            {
                //light tap of the jumping buttons
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
                jumpRemaining--;
                Debug.Log("low"+jumpRemaining);
                JumpFX();

            }
        }

        if (context.performed && wallJumpTimer > 0f)
        {
            isWallJumping = true;
            rb.velocity=new Vector2(wallJumpDirection* wallJumpPower.x,wallJumpPower.y); //Jump away from wall
            wallJumpTimer = 0;
            JumpFX();
            if (transform.localScale.x != wallJumpDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 ls = transform.localScale;
                ls.x *= -1;
                transform.localScale = ls;
            }
            Invoke(nameof(CancelWallJump), wallJumpTime + 0.1f);
            jumpRemaining = maxJumps;
        }
        
        
    }

    private void JumpFX()
    {
        animator.SetTrigger("jump");
        smokeFX.Play();
    }

    public void Drop(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && isOnPlatForm&&playerCollider.enabled)
        {
            StartCoroutine(DisablePlayerCollider(droppingTime));
        }

    }

    private IEnumerator DisablePlayerCollider(float disableTime)
    {
        playerCollider.enabled = false;
        yield return new WaitForSeconds(disableTime);
        playerCollider.enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            isOnPlatForm = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            isOnPlatForm = false;
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

        if(rb.velocity.y==0)
        {
            smokeFX.Play();
        }

    }
}
