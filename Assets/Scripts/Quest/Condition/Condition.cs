using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Condition : ScriptableObject
{
    [SerializeField]
    private string description; // 해당 조건에 대한 설명

    /** 조건을 통과했는지 확인한다 */
    public abstract bool IsPass(Quest quest);
}
