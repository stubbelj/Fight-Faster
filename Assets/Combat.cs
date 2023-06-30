using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : MonoBehaviour
{
    public float health = 10f;

    public virtual void TakeDamage(AttackData attackData) {
        health -= attackData.damage;
        print("took damage!");
    }
}