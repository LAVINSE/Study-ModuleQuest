using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** �۾��� ���� ������ */
public enum TaskState
{
    Inactive,
    Running,
    Complete
}

[CreateAssetMenu(menuName = "Quest/Task/Task", fileName = "Task_")]
public class Task : ScriptableObject
{
    #region Events
    public delegate void StateChangedHandler(Task task, TaskState currentState, TaskState prevState);
    public delegate void SuccessChangedHandler(Task task, int currentSuccess, int prevSuccess);
    #endregion

    [SerializeField]
    private Category category; // �з�

    [Header("Text")]
    [SerializeField]
    private string codeName; // ���������� ����� �̸�
    [SerializeField]
    private string description; // ����

    [Header("Action")]
    [SerializeField]
    private TaskAction action;

    [Header("Target")]
    [SerializeField]
    private TaskTarget[] targets; // ��� Ÿ������ �ϴ��� Ȯ��

    [Header("Setting")]
    [SerializeField]
    private InitialSuccessValue initialSuccessValue; // �ʱ� ���� ���� ���ϴ°�
    [SerializeField]
    private int needSuccessToComplete; // �ʿ��� ���� Ƚ��
    [SerializeField]
    private bool canReceiveReportsDuringCompletion; // �۾��� �Ϸ��߾ ��� ���� Ƚ���� �������� Ȯ���ϴ� ����

    private TaskState state;
    private int currentSuccess;

    public event StateChangedHandler onStateChanged; // ���°� ����Ǿ����� ������ Event
    public event SuccessChangedHandler onSuccessChanged; // ���� ����Ƚ���� ����Ǿ����� ������ Event

    public int CurrentSuccess // ���� ���� Ƚ��
    {
        get => currentSuccess;
        set
        {
            int prevSuccess = currentSuccess;
            currentSuccess = Mathf.Clamp(value, 0, needSuccessToComplete);
            if (currentSuccess != prevSuccess)
            {
                State = currentSuccess == needSuccessToComplete ? TaskState.Complete : TaskState.Running;
                onSuccessChanged?.Invoke(this, currentSuccess, prevSuccess);
            }
        }
    }
    public Category Category => category;
    public string CodeName => codeName;
    public string Description => description;
    public int NeedSuccessToComplete => needSuccessToComplete;
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

    /** �۾��� ���� Quest�� ������ Ȯ���Ѵ� */
    public void Setup(Quest owner)
    {
        Owner = owner;
    }

    /** �۾��� ���������� �����Ѵ� */
    public void Start()
    {
        // ���� ����
        State = TaskState.Running;

        // �ʱ� �������� ���� ���
        if (initialSuccessValue)
            CurrentSuccess = initialSuccessValue.GetValue(this);
    }

    /** �۾��� ������ �������� �����Ѵ� */
    public void End()
    {
        // �̺�Ʈ null
        onStateChanged = null;
        onSuccessChanged = null;
    }

    /** ���� �޴´� */
    public void ReceiveReport(int successCount)
    {
        // ���� ���� ��� �������� ���� this�� �� ������ ���� �����ߴ��� �˱� ����
        CurrentSuccess = action.Run(this, CurrentSuccess, successCount);
    }

    /** �۾��� �Ϸ��Ѵ� */
    public void Complete()
    {
        // ���� ����Ƚ���� �ʿ��� ����Ƚ���� �ٲٱ�
        CurrentSuccess = needSuccessToComplete;
    }

    public bool IsTarget(string category, object target)
        => Category == category && // ī�װ��� ���� ��� true
        targets.Any(x => x.IsEqual(target)) && // Setting �س��� Target�� �߿� �ش��ϴ� Target�� ���� ��� true
        (!IsComplete || (IsComplete && canReceiveReportsDuringCompletion)); // �Ϸᰡ �ƴ� ��� or �Ϸ�� ���¿��� ��� ���������� true
}