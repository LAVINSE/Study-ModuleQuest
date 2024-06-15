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
    #region Events
    public delegate void TaskSuccessChangedHandler(Quest quest, Task task, int currentSuccess, int prevSuccess);
    public delegate void CompletedHandler(Quest quest);
    public delegate void CancelHandler(Quest quest);
    public delegate void NewTaskGroupHandler(Quest quest, TaskGroup currentTaskGroup, TaskGroup prevTaskGroup);
    #endregion // Events

    [SerializeField] private Category category;
    [SerializeField] private Sprite icon;

    [Header("Text")]
    [SerializeField] private string codeName;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;

    [Header("Task")]
    [SerializeField] private TaskGroup[] taskGroups;

    [Header("Option")]
    [SerializeField] private bool useAutoComplete;

    private int currentTaskGroupIndex;

    public Category Category => category;
    public Sprite Icon => icon;
    public string CodeName => codeName;
    public string DisplayName => displayName;
    public string Description => description;
    public QuestState State { get; private set; }
    public TaskGroup CurrentTaskGroup => taskGroups[currentTaskGroupIndex];
    public IReadOnlyList<TaskGroup> TaskGroups => taskGroups;
    public bool isRegistered => State != QuestState.Inactive;
    public bool IsComplatable => State != QuestState.WaitingForCompletion;
    public bool IsComplete => State == QuestState.Complete;
    public bool IsCancel => State == QuestState.Cancel;

    public event TaskSuccessChangedHandler onTaskSuccessChanged;
    public event CompletedHandler onCompleted;
    public event CancelHandler onCancelled;
    public event NewTaskGroupHandler onNewTaskGroup;

    public void OnRegister()
    {
        // Assert => ���ڷ� ���� ���� false >> �ڿ� ���� ��� ,���� �Ͼ���� �ȵǴ� ������ �Ͼ���� �����ϴ� �ڵ�
        // ���ɿ� ������ ���� > ����� ���α׷�
        Debug.Assert(!isRegistered, "This quest has alread been registered");

        foreach(var taskGroup in taskGroups)
        {
            taskGroup.Setup(this);
            foreach(var task in taskGroup.Tasks)
                task.onSuccessChanged += OnSuccesesChanged;
        }

        State = QuestState.Running;
        CurrentTaskGroup.Start();
    }

    public void ReceiveReport(string category, object target, int successCount)
    {
        Debug.Assert(!isRegistered, "This quest has alread been registered");
        Debug.Assert(!IsCancel, "This quest has been canceled.");

        if (IsComplete)
            return;
        
        CurrentTaskGroup.ReceiveReport(category, target, successCount);

        if (CurrentTaskGroup.IsAllTaskComplete)
        {
            if (currentTaskGroupIndex + 1 == taskGroups.Length)
            {
                State = QuestState.WaitingForCompletion;
                if (useAutoComplete)
                    Complete();
            }
            else
            {
                var prevTaskGroup = taskGroups[currentTaskGroupIndex++];
                prevTaskGroup.End();
                CurrentTaskGroup.Start();
                onNewTaskGroup?.Invoke(this, CurrentTaskGroup, prevTaskGroup);
            }
        }
        else
            State = QuestState.Running;
    }

    public void Complete()
    {
        Debug.Assert(!isRegistered, "This quest has alread been registered");
        Debug.Assert(!IsCancel, "This quest has been canceled.");
        Debug.Assert(!IsComplete, "This quest has already been completed.");

        foreach(var taskGroup in taskGroups)
            taskGroup.Complete();

        State = QuestState.Complete;

        onCompleted?.Invoke(this);

        onTaskSuccessChanged = null;
        onCompleted = null;
        onCancelled = null;
        onNewTaskGroup = null;
    }

    public void Cancel()
    {

    }

    private void OnSuccesesChanged(Task task, int currentSuccess, int prevSuccess)
        => onTaskSuccessChanged?.Invoke(this, task, currentSuccess, prevSuccess);
}
