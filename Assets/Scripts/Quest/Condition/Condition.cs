using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Condition : ScriptableObject
{
    [SerializeField]
    private string description; // �ش� ���ǿ� ���� ����

    /** ������ ����ߴ��� Ȯ���Ѵ� */
    public abstract bool IsPass(Quest quest);
}
