using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    string tagMask;
    Combat combat;

    public void Init(string newTagMask, Combat newCombat) {
        tagMask = newTagMask;
        combat = newCombat;
    }

    public void TakeDamage(AttackData attackData) {
        if (tagMask == attackData.tagMask || attackData.tagMask == "Neutral") {
            combat.TakeDamage(attackData);
        }
    }
}
