using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombat : Combat
{
    public GameObject slashPrefab;

    // Slash Timer
    private float slashTimer = 0f;
    private float slashCooldown = 3f;

    void Awake() {
        transform.Find("Hurtbox").GetComponent<Hurtbox>().Init("Enemy", this);
    }

    public override void TakeDamage(AttackData attackData) {
        health -= attackData.damage;
        print("enemy took damage!");
    }

    // Update is called once per frame
    void Update()
    {
        // Slash ONCE every 3 seconds
        if (slashTimer > 0) {
            slashTimer -= Time.deltaTime;
        } else {
            Slash();
            slashTimer = slashCooldown;
        }

        // if health is 0, die
        if (health <= 0) Die();
    }

    void Slash() {
        GameObject newSlashPrefab = GameObject.Instantiate(slashPrefab, transform.position + new Vector3(9, 0, 0), Quaternion.identity);
        newSlashPrefab.GetComponent<Attack>().Init(new AttackData("Player", 1));
    }

    void Die() {
        print("enemy died!");
        // Destroy Enemy
        Destroy(gameObject);
    }
}
