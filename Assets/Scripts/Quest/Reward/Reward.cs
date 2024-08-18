using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Reward : ScriptableObject
{
    [SerializeField]
    private Sprite icon; // 아이콘
    [SerializeField]
    private string description; // 설명
    [SerializeField]
    private int quantity; // 수량

    public Sprite Icon => icon;
    public string Description => description;
    public int Quantity => quantity;

    public abstract void Give(Quest quest);
}
