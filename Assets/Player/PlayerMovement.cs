using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed;
    [SerializeField] bool isGrounded;
    [SerializeField] LayerMask groundMask;
    [SerializeField] int jumps;
    [SerializeField] int maxJumps;
    [SerializeField] float jumpForce;

    Vector2 maxVel = new Vector2(50f, 150f);

    Rigidbody2D rb;
    float lastGroundChecked;
    float lastJumped;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        lastGroundChecked = Time.time;
        lastJumped = Time.time;
    }

    Dictionary<string, bool> currInputs = new Dictionary<string, bool> {
        {"left", false},
        {"right", false},
        {"up", false},
        {"down", false},
        {"jump", false},
    };
    
    void Update() {
        if (Input.GetKey("a")) {
            rb.velocity += -Vector2.right * moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKey("d")) {
            rb.velocity += Vector2.right * moveSpeed * Time.deltaTime;
        }

        if (Input.GetKeyDown("space")) {
            Jump();
        }

        SetGrounded();
        ClampMovement();
        rb.velocity += 9.8f * -Vector2.up * Time.deltaTime;
    }

    void SetGrounded() {
        if (Time.time > lastGroundChecked + 0.1f) {
            if (Physics2D.OverlapCircle(new Vector3(transform.position.x, transform.position.y - GetComponent<SpriteRenderer>().bounds.extents.y - 0.01f, 0), 0.005f, groundMask)) {
                isGrounded = true;
                jumps = maxJumps;
                lastGroundChecked = Time.time;
            } else {
                isGrounded = false;
                lastGroundChecked = Time.time;
            }
        }
    }

    void Jump() {
        if (jumps > 0 && Time.time > lastJumped + 0.1f) {
            jumps--;
            rb.velocity += new Vector2(0, jumpForce);
            lastJumped = Time.time;
        }
    }

    void ClampMovement() {
        rb.velocity = new Vector3(Mathf.Clamp(rb.velocity.x, -maxVel.x, maxVel.x), Mathf.Clamp(rb.velocity.y, -maxVel.y, maxVel.y));
    }
}
