using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

using Debug = UnityEngine.Debug;

/** ����Ʈ�� ���� ������*/
public enum QuestState
{
    Inactive,
    Running,
    Complete,
    Cancel,
    WaitingForCompletion // �ڵ����� �Ϸ�Ǵ� ����Ʈ or ������ ����Ʈ �ϷḦ �������ϴ� ����Ʈ
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
    private TaskGroup[] taskGroups; // �ϳ��� ����Ʈ�� ���� �۾��׷��� ���� �� �ִ�

    [Header("Reward")]
    [SerializeField]
    private Reward[] rewards; // ����

    [Header("Option")]
    [SerializeField]
    private bool useAutoComplete; // �ڵ��Ϸ�Ǵ� ����Ʈ���� Ȯ���ϴ� ����
    [SerializeField]
    private bool isCancelable; // ��� �� �� ���� ����Ʈ���� Ȯ���ϴ� ����
    [SerializeField]
    private bool isSavable;

    [Header("Condition")]
    [SerializeField]
    private Condition[] acceptionConditions; // ����Ʈ�� ���۵� ����
    [SerializeField]
    private Condition[] cancelConditions; // ����Ʈ�� ����� �� �ִ� ����

    private int currentTaskGroupIndex; // ���� ���° �۾��׷����� �Ǵ��ϴ� ����

    public Category Category => category;
    public Sprite Icon => icon;
    public string CodeName => codeName;
    public string DisplayName => displayName;
    public string Description => description;
    public QuestState State { get; private set; }
    public TaskGroup CurrentTaskGroup => taskGroups[currentTaskGroupIndex]; // ���� �۾��׷� ����
    public IReadOnlyList<TaskGroup> TaskGroups => taskGroups;
    public IReadOnlyList<Reward> Rewards => rewards;
    public bool IsRegistered => State != QuestState.Inactive; // ��Ȱ��ȭ ���°� �ƴҰ�� ���
    public bool IsComplatable => State == QuestState.WaitingForCompletion; // �Ϸ� ��� �����ϰ�� �Ϸᰡ��
    public bool IsComplete => State == QuestState.Complete; // �Ϸ� ���� �ϰ�� �Ϸ�
    public bool IsCancel => State == QuestState.Cancel; // ��� ���� �ϰ�� ��� 

    // ��� �� �� �ִ� ����Ʈ && ����Ʈ�� ��ҵ� ������ ���� ���������� true
    public virtual bool IsCancelable => isCancelable && cancelConditions.All(x => x.IsPass(this)); 
    public bool IsAcceptable => acceptionConditions.All(x => x.IsPass(this)); // ����Ʈ�� ���۵� ������ ��� ����� ��� true
    public virtual bool IsSavable => isSavable;

    public event TaskSuccessChangedHandler onTaskSuccessChanged; // �۾� ����Ƚ���� ����Ǿ����� ������ Event
    public event CompletedHandler onCompleted; // ����Ʈ�� �Ϸ������� ������ Event
    public event CanceledHandler onCanceled; // ����Ʈ�� ��ҵ����� ������ Event
    public event NewTaskGroupHandler onNewTaskGroup; // ���ο� �۾��׷��� ���۵Ǿ����� ������ Event

    /** ��ϵǾ����� �����Ѵ� */
    public void OnRegister()
    {
        // �̹� ����Ʈ�� ��ϵǾ����� ����
        Debug.Assert(!IsRegistered, "This quest has already been registered.");

        foreach (var taskGroup in taskGroups)
        {
            // �����ָ� �����Ѵ�
            taskGroup.Setup(this);
            foreach (var task in taskGroup.Tasks)
                task.onSuccessChanged += OnSuccessChanged; // event ���
        }

        State = QuestState.Running;
        CurrentTaskGroup.Start(); // �۾� ����
    }

    /** ���� �޴´� */
    public void ReceiveReport(string category, object target, int successCount)
    {
        // ��ϵ� ���°� �ƴϰų� ��� �����϶� ����
        Debug.Assert(IsRegistered, "This quest has already been registered.");
        Debug.Assert(!IsCancel, "This quest has been canceled.");

        // ����Ʈ�� �Ϸ������ ��쿡�� ���� ���� ��Ȳ�� ���� �� �ִ�
        if (IsComplete)
            return;

        // ���� �۾��׷� ���� �޴´�
        CurrentTaskGroup.ReceiveReport(category, target, successCount);

        // ���� �۾��׷��� �۾����� �Ϸ���� ���
        if (CurrentTaskGroup.IsAllTaskComplete)
        {
            // ���� �۾��׷��� ���� ���
            if (currentTaskGroupIndex + 1 == taskGroups.Length)
            {
                State = QuestState.WaitingForCompletion;

                // �ڵ� �Ϸ� ����Ʈ�� ��� >> �Ϸ�
                if (useAutoComplete)
                    Complete();
            }
            else
            {
                // ���� �۾��׷��� �������� �ε����� ������Ų��
                var prevTasKGroup = taskGroups[currentTaskGroupIndex++];
                prevTasKGroup.End(); // ���� �۾��׷��� ������
                CurrentTaskGroup.Start(); // ���� �۾��׷��� �����Ѵ�
                onNewTaskGroup?.Invoke(this, CurrentTaskGroup, prevTasKGroup); // ���ο� �۾��׷��� �����ߴٴ� event ����
            }
        }
        else
            State = QuestState.Running;
    }

    /** ����Ʈ�� �Ϸ��Ѵ� */
    public void Complete()
    {
        // ���� Ȯ��
        CheckIsRunning();

        // �۾� �׷���� �Ϸ��Ѵ�
        foreach (var taskGroup in taskGroups)
            taskGroup.Complete();

        // �Ϸ� ����
        State = QuestState.Complete;

        // ���� ����
        foreach (var reward in rewards)
            reward.Give(this);

        // �Ϸ� event ����
        onCompleted?.Invoke(this);

        // event �ʱ�ȭ
        onTaskSuccessChanged = null;
        onCompleted = null;
        onCanceled = null;
        onNewTaskGroup = null;
    }

    public virtual void Cancel()
    {
        CheckIsRunning();
        // ��� �� �� ���� ��� ����
        Debug.Assert(IsCancelable, "This quest can't be canceled");

        State = QuestState.Cancel;
        onCanceled?.Invoke(this); // ��� event ����
    }

    /** ����Ʈ ���纻�� �����ϰ� �ȿ� �ִ� �۾��鵵 ���纻���� ����� */
    public Quest Clone()
    {
        var clone = Instantiate(this);
        clone.taskGroups = taskGroups.Select(x => new TaskGroup(x)).ToArray();
        
        return clone;
    }
    
    /** ������ �����͸� ������ */
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

    /** ����� �����͸� �����´� */
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

    /** ������ Ȯ���Ѵ� */
    [Conditional("UNITY_EDITOR")]
    private void CheckIsRunning()
    {
        Debug.Assert(IsRegistered, "This quest has already been registered");
        Debug.Assert(!IsCancel, "This quest has been canceled.");
        Debug.Assert(!IsComplete, "This quest has already been completed");
    }
}
