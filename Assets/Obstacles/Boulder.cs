using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boulder : MonoBehaviour
{
    [SerializeField] float speed = 40f;
    [SerializeField] Rigidbody2D rb;

    void Start() {
        gameObject.GetComponent<Attack>().Init(new AttackData("Neutral", 1));
    }

    // Update is called once per frame
    void Update()
    {
        if (IsGrounded()) {
            RollLeft();
        }

        // Destroy when at the left edge of the screen
        if (transform.position.x < -100) {
            Destroy(gameObject);
        }
    }

    bool IsGrounded() {
        return transform.position.y <= 0;
    }

    void RollLeft() {
        transform.position += new Vector3(-speed, 0, 0) * Time.deltaTime;
    }
}
