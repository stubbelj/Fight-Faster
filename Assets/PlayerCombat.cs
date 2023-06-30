using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : Combat
{
    public GameObject slashPrefab;

    void Awake() {
        transform.Find("Hurtbox").GetComponent<Hurtbox>().Init("Player", this);
    }

    public override void TakeDamage(AttackData attackData) {
        health -= attackData.damage;
        print("player took damage!");
    }

    void Update() {
        if (Input.GetKeyDown("f")) {
            Slash();
        }
    }

    void Slash() {
        GameObject newSlashPrefab = GameObject.Instantiate(slashPrefab, transform.position + new Vector3(9, 0, 0), Quaternion.identity);
        newSlashPrefab.GetComponent<Attack>().Init(new AttackData("Enemy", 1));
    }
}
