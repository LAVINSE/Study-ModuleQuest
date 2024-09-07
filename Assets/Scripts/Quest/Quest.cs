using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

using Debug = UnityEngine.Debug;

/** 퀘스트의 상태 열거형*/
public enum QuestState
{
    Inactive,
    Running,
    Complete,
    Cancel,
    WaitingForCompletion // 자동으로 완료되는 퀘스트 or 유저가 퀘스트 완료를 눌러야하는 퀘스트
}

[CreateAssetMenu(menuName = "Quest/Quest", fileName = "Quest_")]
public class Quest : ScriptableObject
{
    #region Events
    public delegate void TaskSuccessChangedHandler(Quest quest, Task task, int currentSuccess, int prevSuccess);
    public delegate void CompletedHandler(Quest quest);
    public delegate void CanceledHandler(Quest quest);
    public delegate void NewTaskGroupHandler(Quest quest, TaskGroup currentTaskGroup, TaskGroup prevTaskGroup);
    #endregion

    [SerializeField]
    private Category category;
    [SerializeField]
    private Sprite icon;

    [Header("Text")]
    [SerializeField]
    private string codeName;
    [SerializeField]
    private string displayName;
    [SerializeField, TextArea]
    private string description;

    [Header("Task")]
    [SerializeField]
    private TaskGroup[] taskGroups; // 하나의 퀘스트가 여러 작업그룹을 가질 수 있다

    [Header("Reward")]
    [SerializeField]
    private Reward[] rewards; // 보상

    [Header("Option")]
    [SerializeField]
    private bool useAutoComplete; // 자동완료되는 퀘스트인지 확인하는 변수
    [SerializeField]
    private bool isCancelable; // 취소 할 수 없는 퀘스트인지 확인하는 변수
    [SerializeField]
    private bool isSavable;

    [Header("Condition")]
    [SerializeField]
    private Condition[] acceptionConditions; // 퀘스트가 시작될 조건
    [SerializeField]
    private Condition[] cancelConditions; // 퀘스트를 취소할 수 있는 조건

    private int currentTaskGroupIndex; // 현재 몇번째 작업그룹인지 판단하는 변수

    public Category Category => category;
    public Sprite Icon => icon;
    public string CodeName => codeName;
    public string DisplayName => displayName;
    public string Description => description;
    public QuestState State { get; private set; }
    public TaskGroup CurrentTaskGroup => taskGroups[currentTaskGroupIndex]; // 현재 작업그룹 변수
    public IReadOnlyList<TaskGroup> TaskGroups => taskGroups;
    public IReadOnlyList<Reward> Rewards => rewards;
    public bool IsRegistered => State != QuestState.Inactive; // 비활성화 상태가 아닐경우 등록
    public bool IsComplatable => State == QuestState.WaitingForCompletion; // 완료 대기 상태일경우 완료가능
    public bool IsComplete => State == QuestState.Complete; // 완료 상태 일경우 완료
    public bool IsCancel => State == QuestState.Cancel; // 취소 상태 일경우 취소 

    // 취소 할 수 있는 퀘스트 && 퀘스트가 취소될 조건이 전부 통과됐을경우 true
    public virtual bool IsCancelable => isCancelable && cancelConditions.All(x => x.IsPass(this)); 
    public bool IsAcceptable => acceptionConditions.All(x => x.IsPass(this)); // 퀘스트가 시작될 조건이 모두 통과될 경우 true
    public virtual bool IsSavable => isSavable;

    public event TaskSuccessChangedHandler onTaskSuccessChanged; // 작업 성공횟수가 변경되었을때 실행할 Event
    public event CompletedHandler onCompleted; // 퀘스트를 완료했을때 실행할 Event
    public event CanceledHandler onCanceled; // 퀘스트가 취소됐을때 실행할 Event
    public event NewTaskGroupHandler onNewTaskGroup; // 새로운 작업그룹이 시작되었을때 실행할 Event

    /** 등록되었을때 실행한다 */
    public void OnRegister()
    {
        // 이미 퀘스트가 등록되었을때 오류
        Debug.Assert(!IsRegistered, "This quest has already been registered.");

        foreach (var taskGroup in taskGroups)
        {
            // 소유주를 설정한다
            taskGroup.Setup(this);
            foreach (var task in taskGroup.Tasks)
                task.onSuccessChanged += OnSuccessChanged; // event 등록
        }

        State = QuestState.Running;
        CurrentTaskGroup.Start(); // 작업 시작
    }

    /** 보고를 받는다 */
    public void ReceiveReport(string category, object target, int successCount)
    {
        // 등록된 상태가 아니거나 취소 상태일때 오류
        Debug.Assert(IsRegistered, "This quest has already been registered.");
        Debug.Assert(!IsCancel, "This quest has been canceled.");

        // 퀘스트가 완료상태일 경우에도 보고 받을 상황이 있을 수 있다
        if (IsComplete)
            return;

        // 현재 작업그룹 보고를 받는다
        CurrentTaskGroup.ReceiveReport(category, target, successCount);

        // 현재 작업그룹의 작업들이 완료됐을 경우
        if (CurrentTaskGroup.IsAllTaskComplete)
        {
            // 다음 작업그룹이 없을 경우
            if (currentTaskGroupIndex + 1 == taskGroups.Length)
            {
                State = QuestState.WaitingForCompletion;

                // 자동 완료 퀘스트일 경우 >> 완료
                if (useAutoComplete)
                    Complete();
            }
            else
            {
                // 현재 작업그룹을 가져오고 인덱스를 증가시킨다
                var prevTasKGroup = taskGroups[currentTaskGroupIndex++];
                prevTasKGroup.End(); // 이전 작업그룹을 끝낸다
                CurrentTaskGroup.Start(); // 현재 작업그룹을 시작한다
                onNewTaskGroup?.Invoke(this, CurrentTaskGroup, prevTasKGroup); // 새로운 작업그룹이 시작했다는 event 실행
            }
        }
        else
            State = QuestState.Running;
    }

    /** 퀘스트를 완료한다 */
    public void Complete()
    {
        // 오류 확인
        CheckIsRunning();

        // 작업 그룹들을 완료한다
        foreach (var taskGroup in taskGroups)
            taskGroup.Complete();

        // 완료 상태
        State = QuestState.Complete;

        // 보상 지급
        foreach (var reward in rewards)
            reward.Give(this);

        // 완료 event 실행
        onCompleted?.Invoke(this);

        // event 초기화
        onTaskSuccessChanged = null;
        onCompleted = null;
        onCanceled = null;
        onNewTaskGroup = null;
    }

    public virtual void Cancel()
    {
        CheckIsRunning();
        // 취소 할 수 없을 경우 오류
        Debug.Assert(IsCancelable, "This quest can't be canceled");

        State = QuestState.Cancel;
        onCanceled?.Invoke(this); // 취소 event 실행
    }

    /** 퀘스트 복사본을 생성하고 안에 있는 작업들도 복사본으로 만든다 */
    public Quest Clone()
    {
        var clone = Instantiate(this);
        clone.taskGroups = taskGroups.Select(x => new TaskGroup(x)).ToArray();
        
        return clone;
    }
    
    /** 저장할 데이터를 보낸다 */
    public QuestSaveData ToSaveData()
    {
        return new QuestSaveData
        {
            codeName = codeName,
            state = State,
            taskGroupIndex = currentTaskGroupIndex,
            taskSuccessCounts = CurrentTaskGroup.Tasks.Select(x => x.CurrentSuccess).ToArray()
        };
    }

    /** 저장된 데이터를 가져온다 */
    public void LoadFrom(QuestSaveData saveData)
    {
        State = saveData.state;
        currentTaskGroupIndex = saveData.taskGroupIndex;

        for(int i = 0; i < currentTaskGroupIndex; i++)
        {
            var taskGroup = taskGroups[i];
            taskGroup.Start();
            taskGroup.Complete();
        }

        for(int i = 0; i < saveData.taskSuccessCounts.Length; i++)
        {
            CurrentTaskGroup.Start();
            CurrentTaskGroup.Tasks[i].CurrentSuccess = saveData.taskSuccessCounts[i];
        }
    }

    private void OnSuccessChanged(Task task, int currentSuccess, int prevSuccess)
        => onTaskSuccessChanged?.Invoke(this, task, currentSuccess, prevSuccess);

    /** 오류를 확인한다 */
    [Conditional("UNITY_EDITOR")]
    private void CheckIsRunning()
    {
        Debug.Assert(IsRegistered, "This quest has already been registered");
        Debug.Assert(!IsCancel, "This quest has been canceled.");
        Debug.Assert(!IsComplete, "This quest has already been completed");
    }
}
