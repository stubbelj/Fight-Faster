using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public Hitbox hitbox;

    public void Init(AttackData newAttackData) {
        hitbox = transform.Find("Hitbox").GetComponent<Hitbox>();
        hitbox.Init(newAttackData);
    }
}