using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** 작업의 상태 열거형 */
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
    private Category category; // 분류

    [Header("Text")]
    [SerializeField]
    private string codeName; // 내부적으로 사용할 이름
    [SerializeField]
    private string description; // 설명

    [Header("Action")]
    [SerializeField]
    private TaskAction action;

    [Header("Target")]
    [SerializeField]
    private TaskTarget[] targets; // 어떤걸 타겟으로 하는지 확인

    [Header("Setting")]
    [SerializeField]
    private InitialSuccessValue initialSuccessValue; // 초기 성공 값을 정하는거
    [SerializeField]
    private int needSuccessToComplete; // 필요한 성공 횟수
    [SerializeField]
    private bool canReceiveReportsDuringCompletion; // 작업을 완료했어도 계속 성공 횟수를 받을건지 확인하는 변수

    private TaskState state;
    private int currentSuccess;

    public event StateChangedHandler onStateChanged; // 상태가 변경되었을때 실행할 Event
    public event SuccessChangedHandler onSuccessChanged; // 현재 성공횟수가 변경되었을때 실행할 Event

    public int CurrentSuccess // 현재 성공 횟수
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

    /** 작업을 가진 Quest가 누군지 확인한다 */
    public void Setup(Quest owner)
    {
        Owner = owner;
    }

    /** 작업이 시작했을때 실행한다 */
    public void Start()
    {
        // 상태 변경
        State = TaskState.Running;

        // 초기 성공값이 있을 경우
        if (initialSuccessValue)
            CurrentSuccess = initialSuccessValue.GetValue(this);
    }

    /** 작업이 완전히 끝났을때 실행한다 */
    public void End()
    {
        // 이벤트 null
        onStateChanged = null;
        onSuccessChanged = null;
    }

    /** 보고를 받는다 */
    public void ReceiveReport(int successCount)
    {
        // 들어온 값이 계속 더해지는 로직 this를 한 이유는 누가 실행했는지 알기 위해
        CurrentSuccess = action.Run(this, CurrentSuccess, successCount);
    }

    /** 작업을 완료한다 */
    public void Complete()
    {
        // 현재 성공횟수를 필요한 성공횟수로 바꾸기
        CurrentSuccess = needSuccessToComplete;
    }

    public bool IsTarget(string category, object target)
        => Category == category && // 카테고리가 같을 경우 true
        targets.Any(x => x.IsEqual(target)) && // Setting 해놓은 Target들 중에 해당하는 Target이 있을 경우 true
        (!IsComplete || (IsComplete && canReceiveReportsDuringCompletion)); // 완료가 아닐 경우 or 완료된 상태에서 계속 보고받을경우 true
}