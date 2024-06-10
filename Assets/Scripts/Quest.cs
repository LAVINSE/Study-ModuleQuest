using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum QuestState
{ 
    Inactive,
    Running,
    Complete,
    Cancel,
    WaitingForCompletion
}


[CreateAssetMenu(menuName = "Quest/Quest", fileName = "Quest")]
public class Quest : ScriptableObject
{
    [SerializeField] private Category category;
    [SerializeField] private Sprite icon;

    [Header("Text")]
    [SerializeField] private string codeName;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;

    [Header("Option")]
    [SerializeField] private bool useAutoComplete;

    public Category Category => category;
    public Sprite Icon => icon;
    public string CodeName => codeName;
    public string DisplayName => displayName;
    public string Description => description;
    public QuestState State { get; private set; }
}
