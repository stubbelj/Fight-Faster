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

        StartCoroutine(DelayedForce());
    }

    IEnumerator DelayedForce() {
        yield return new WaitForSeconds(5f);
        print("bam");
        rb.velocity += new Vector2(50, 0);
    }

    Dictionary<string, bool> currInputs = new Dictionary<string, bool> {
        {"left", false},
        {"right", false},
        {"up", false},
        {"down", false},
        {"jump", false},
    };
    
    void Update() {
        if (Input.GetKeyDown("a")) {
            rb.velocity += -Vector2.right * moveSpeed * Time.deltaTime;
        }
        else if (Input.GetKeyDown("d")) {
            rb.velocity += Vector2.right * moveSpeed * Time.deltaTime;
        }

        if (Input.GetKeyDown("space")) {
            Jump();
        }

        SetGrounded();
        ClampMovement();
        rb.velocity += 9.8f * -Vector2.up * Time.deltaTime;
    }

    //called every frame from PlayerInputManager.Update()
    public void HandleInputs(Dictionary<string, bool> newInputs) {
        currInputs = newInputs;

        if (newInputs["left"]) {
            rb.velocity += -Vector2.right * moveSpeed * Time.deltaTime;
        }
        else if (newInputs["right"]) {
            rb.velocity += Vector2.right * moveSpeed * Time.deltaTime;
        }

        if (newInputs["jump"]) {
            Jump();
        }
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
        //clamp velocity
        rb.velocity = new Vector3(Mathf.Clamp(rb.velocity.x, -maxVel.x, maxVel.x), Mathf.Clamp(rb.velocity.y, -maxVel.y, maxVel.y));
    }
}
