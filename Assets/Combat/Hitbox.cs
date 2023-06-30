using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    AttackData attackData;
    
    public void Init(AttackData newAttackData) {
        attackData = newAttackData;
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.tag == "Hurtbox") {
            col.GetComponent<Hurtbox>().TakeDamage(attackData);
        }
    }
}
