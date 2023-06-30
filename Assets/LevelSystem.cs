using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSystem : MonoBehaviour
{
    public int currLevel;
    public int skillPoints;
    public float exp;
    float[] expThresholds = new float[]{100, 150, 225, 337.5f, 506.25f};

    void Awake() {
        currLevel = 0;
        exp = 0;
    }

    void AddExp(float addedExp) {
        exp += addedExp;
        if (exp > expThresholds[currLevel]) {
            LevelUp();
        }
    }

    void LevelUp() {
        exp -= expThresholds[currLevel];
        currLevel++;
        skillPoints++;
    }
}
