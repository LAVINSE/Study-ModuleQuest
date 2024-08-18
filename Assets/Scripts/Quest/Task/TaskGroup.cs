using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/** �۾��׷��� ���� ������ */
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
    public bool IsAllTaskComplete => tasks.All(x => x.IsComplete); // �۾����� ���� �Ϸ� �Ǿ����� Ȯ���ϴ� ����
    public bool IsComplete => State == TaskGroupState.Complete; // �Ϸ� �����϶� �Ϸ� ó���ϴ� ����
    public TaskGroupState State { get; private set; }

    public TaskGroup(TaskGroup copyTarget)
    {
        tasks = copyTarget.Tasks.Select(x => Object.Instantiate(x)).ToArray();
    }

    /** �����ָ� �����Ѵ� */
    public void Setup(Quest owner)
    {
        Owner = owner;
        foreach (var task in tasks)
            task.Setup(owner);
    }

    /** �۾��׷��� ���¸� �����ϰ� �۾����� Start �޼��带 �����Ѵ� */
    public void Start()
    {
        State = TaskGroupState.Running;
        foreach (var task in tasks)
            task.Start();
    }

    /** �۾��׷��� ���¸� �����ϰ� �۾����� End �޼��带 �����Ѵ� */
    public void End()
    {
        State = TaskGroupState.Complete;
        foreach (var task in tasks)
            task.End();
    }

    /** �۾����� ���� Ƚ���� �������ش� */
    public void ReceiveReport(string category, object target, int successCount)
    {
        foreach (var task in tasks)
        {
            // �ش� Ÿ���� ���� ��� >> ����Ƚ���� �����Ѵ�
            if (task.IsTarget(category, target))
                task.ReceiveReport(successCount);
        }
    }

    /** �۾��׷��� ���¸� �����ϰ� �۾����� Complete �޼��带 �����Ѵ� */
    public void Complete()
    {
        if (IsComplete)
            return;

        State = TaskGroupState.Complete;

        foreach (var task in tasks)
        {
            // �۾����� �Ϸ���°� �ƴ϶�� �Ϸ�ó��
            if (!task.IsComplete)
                task.Complete();
        }
    }
}
