using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum TaskState
{
    Inactive,
    Running,
    Complete
}

[CreateAssetMenu(menuName = "Quest/Task/Task", fileName = "Task")]
public class Task : ScriptableObject
{
    #region Events
    public delegate void StateChangedHandler(Task task, TaskState currentState, TaskState prevState);
    public delegate void SuccessChangedHandler(Task task, int currentSuccess, int prevSuccess);
    // public System.Action<Task, TaskState, TaskState> onStateChangeHandler; >> 이런식으로 사용 가능
    #endregion // Events

    [SerializeField] private Category category;

    [Header("Text")]
    [SerializeField] private string codeName;
    [SerializeField] private string description;

    [Header("Action")]
    [SerializeField] private TaskAction action;

    [Header("Target")]
    [SerializeField] private TaskTarget[] targets;

    [Header("Setting")]
    [SerializeField] private InitalSuccessValue initalSuccessValue;
    [SerializeField] private int needSuccessToComplete;
    [SerializeField] private bool canReceiveReportsDuringCompletion; // 작업이 완료되었어도 계속 성공횟수를 보고 받을 것인지 확인

    private TaskState state;
    private int currentSuccess;

    public event StateChangedHandler onStateChanged;
    public event SuccessChangedHandler onSuccessChanged;

    public int CurrentSuccess
    {
        get => CurrentSuccess;
        set
        {
            int prevSuccess = currentSuccess;
            currentSuccess = Mathf.Clamp(value, 0, needSuccessToComplete);
            if(currentSuccess != prevSuccess)
            {
                State = currentSuccess == needSuccessToComplete ? TaskState.Complete : TaskState.Running;
                onSuccessChanged?.Invoke(this, CurrentSuccess, prevSuccess);
            }
        }
    }

    public Category Category => category;
    public int NeedSuccessToComplete => needSuccessToComplete;

    public string CodeName => codeName;
    public string Description => description;

    public TaskState State
    {
        get => state;
        set
        {
            var prevState = state;
            state = value;
            onStateChanged?.Invoke(this, state, prevState);
        }
    }

    public bool IsComplete => State == TaskState.Complete;
    public Quest Owner { get; private set; }

    public void Setup(Quest owner)
    {
        Owner = owner;
    }
    
    public void Start()
    {
        State = TaskState.Running;
        if (initalSuccessValue)
            CurrentSuccess = initalSuccessValue.GetValue(this);
    }

    public void End()
    {
        onStateChanged = null;
        onSuccessChanged = null;
    }

    public void Complete()
    {
        CurrentSuccess = needSuccessToComplete;
    }

    public void ReceiveReport(int successCount)
    {
        CurrentSuccess = action.Run(this, CurrentSuccess, successCount);
    }

    public bool IsTarget(string category, object target)
        => Category == category && targets.Any(x => x.IsEqual(target))
        && (!IsComplete || (IsComplete && canReceiveReportsDuringCompletion));
}

