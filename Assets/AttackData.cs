using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackData
{
    static int attackIDStream = 0;

    public string tagMask;

    public float damage;

    public int attackID;
        
    public AttackData (string newTagMask, int newDamage) {
        tagMask = newTagMask;
        damage = newDamage;

        attackID = attackIDStream++;
    }
}
