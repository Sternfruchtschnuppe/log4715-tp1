using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets._2D
{
    public class PlatformerCharacter2D : MonoBehaviour
    {
        [SerializeField] private int m_BonusJumps = 2;
        private int currentJumps = 0;
        private bool m_IsOnWall = false;
        [SerializeField] private Transform m_WallCheck;  
        [SerializeField] private float m_WallCheckRadius = 0.2f;
        [SerializeField] private float m_MaxJumpForce = 800f;
        private float m_CurrentCharge = 0f;
        private float m_ChargeSpeed = 1f;
        
        [SerializeField] private float m_MaxSpeed = 10f;                    // The fastest the player can travel in the x axis.
        [SerializeField] private float m_JumpForce = 400f;                  // Amount of force added when the player jumps.
        [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;  // Amount of maxSpeed applied to crouching movement. 1 = 100%
        [SerializeField] private bool m_AirControl = false;                 // Whether or not a player can steer while jumping;
        [SerializeField] private LayerMask m_WhatIsGround;                  // A mask determining what is ground to the character

        private Transform m_GroundCheck;    // A position marking where to check if the player is grounded.
        const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
        public bool m_Grounded { get; private set; }

        private Transform m_CeilingCheck;   // A position marking where to check for ceilings
        const float k_CeilingRadius = .01f; // Radius of the overlap circle to determine if the player can stand up
        private Animator m_Anim;            // Reference to the player's animator component.
        private Rigidbody2D m_Rigidbody2D;
        private bool m_FacingRight = true;  // For determining which way the player is currently facing.

        private void Awake()
        {
            // Setting up references.
            m_GroundCheck = transform.Find("GroundCheck");
            m_CeilingCheck = transform.Find("CeilingCheck");
            m_Anim = GetComponent<Animator>();
            m_Rigidbody2D = GetComponent<Rigidbody2D>();
        }


        private void FixedUpdate()
        {
            m_Grounded = false;

            // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
            // This can be done using layers instead but Sample Assets will not overwrite your project settings.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject && !m_Grounded) 
                {
                    m_Grounded = true;
                    currentJumps = 0;
                }
            }
            
            m_IsOnWall = false;
            colliders = Physics2D.OverlapCircleAll(m_WallCheck.position, m_WallCheckRadius, m_WhatIsGround);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != gameObject) 
                {
                    m_IsOnWall = true;
                }
            }
            
            m_Anim.SetBool("Ground", m_Grounded);

            // Set the vertical animation
            m_Anim.SetFloat("vSpeed", m_Rigidbody2D.linearVelocity.y);
        }


        public void Move(float move, bool crouch, bool jump)
        {
            // If crouching, check to see if the character can stand up
            if (!crouch && m_Anim.GetBool("Crouch"))
            {
                // If the character has a ceiling preventing them from standing up, keep them crouching
                if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
                {
                    crouch = true;
                }
            }

            // Set whether or not the character is crouching in the animator
            m_Anim.SetBool("Crouch", crouch);

            //only control the player if grounded or airControl is turned on
            if (m_Grounded || m_AirControl)
            {
                // Reduce the speed if crouching by the crouchSpeed multiplier
                move = (crouch ? move*m_CrouchSpeed : move);

                // The Speed animator parameter is set to the absolute value of the horizontal input.
                m_Anim.SetFloat("Speed", Mathf.Abs(move));
                
                m_Rigidbody2D.linearVelocity = new Vector2(move*m_MaxSpeed, m_Rigidbody2D.linearVelocity.y);
                
                // Move the character

                // If the input is moving the player right and the player is facing left...
                if (move > 0 && !m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
                    // Otherwise if the input is moving the player left and the player is facing right...
                else if (move < 0 && m_FacingRight)
                {
                    // ... flip the player.
                    Flip();
                }
            }
            
            if (jump && m_IsOnWall && !m_Grounded)
            {
                m_Rigidbody2D.linearVelocity = new Vector2(0, m_Rigidbody2D.linearVelocity.y);
                m_Rigidbody2D.AddForce(new Vector2(0.5f * (m_FacingRight ? -m_JumpForce : m_JumpForce), m_JumpForce));
                Flip();
                StartCoroutine(DisableBool());
            }
            else if (jump && !m_Grounded && currentJumps < m_BonusJumps && !m_IsOnWall)
            {
                m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0);
                m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
                ++currentJumps;
            }
            
            
            // If the player should jump...
            if ((jump && m_Grounded && m_Anim.GetBool("Ground")) || 
                (m_Grounded && !crouch && m_CurrentCharge > 0f))
            {
                // Add a vertical force to the player.
                m_Grounded = false;
                m_Anim.SetBool("Ground", false);
                m_Rigidbody2D.AddForce(new Vector2(0f, Mathf.Lerp(m_JumpForce, m_MaxJumpForce, m_CurrentCharge)));
                m_CurrentCharge = 0;
            }

            if (crouch)
            {
                m_CurrentCharge = Mathf.Min(m_CurrentCharge + Time.fixedDeltaTime * m_ChargeSpeed, 1.0f);
            }
        }
        
        IEnumerator DisableBool() {
            m_AirControl = false;
            yield return new WaitForSeconds(0.4f);
            m_AirControl = true;
        }

        private void Flip()
        {
            // Switch the way the player is labelled as facing.
            m_FacingRight = !m_FacingRight;

            // Multiply the player's x local scale by -1.
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
        }
    }
}
