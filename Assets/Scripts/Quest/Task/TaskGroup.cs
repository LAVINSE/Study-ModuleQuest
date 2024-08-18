using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** 작업그룹의 상태 열거형 */
public enum TaskGroupState
{
    Inactive,
    Running,
    Complete
}

[System.Serializable]
public class TaskGroup
{
    [SerializeField]
    private Task[] tasks;

    public IReadOnlyList<Task> Tasks => tasks;
    public Quest Owner { get; private set; }
    public bool IsAllTaskComplete => tasks.All(x => x.IsComplete); // 작업들이 전부 완료 되었는지 확인하는 변수
    public bool IsComplete => State == TaskGroupState.Complete; // 완료 상태일때 완료 처리하는 변수
    public TaskGroupState State { get; private set; }

    public TaskGroup(TaskGroup copyTarget)
    {
        tasks = copyTarget.Tasks.Select(x => Object.Instantiate(x)).ToArray();
    }

    /** 소유주를 설정한다 */
    public void Setup(Quest owner)
    {
        Owner = owner;
        foreach (var task in tasks)
            task.Setup(owner);
    }

    /** 작업그룹의 상태를 변경하고 작업들의 Start 메서드를 실행한다 */
    public void Start()
    {
        State = TaskGroupState.Running;
        foreach (var task in tasks)
            task.Start();
    }

    /** 작업그룹의 상태를 변경하고 작업들의 End 메서드를 실행한다 */
    public void End()
    {
        State = TaskGroupState.Complete;
        foreach (var task in tasks)
            task.End();
    }

    /** 작업들의 성공 횟수를 전달해준다 */
    public void ReceiveReport(string category, object target, int successCount)
    {
        foreach (var task in tasks)
        {
            // 해당 타겟이 맞을 경우 >> 성공횟수를 전달한다
            if (task.IsTarget(category, target))
                task.ReceiveReport(successCount);
        }
    }

    /** 작업그룹의 상태를 변경하고 작업들의 Complete 메서드를 실행한다 */
    public void Complete()
    {
        if (IsComplete)
            return;

        State = TaskGroupState.Complete;

        foreach (var task in tasks)
        {
            // 작업들이 완료상태가 아니라면 완료처리
            if (!task.IsComplete)
                task.Complete();
        }
    }
}
