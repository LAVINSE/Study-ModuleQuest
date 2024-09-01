using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestSystemTest : MonoBehaviour
{
    [SerializeField]
    private Quest quest;
    [SerializeField]
    private Category category;
    [SerializeField]
    private TaskTarget target;

    void Start()
    {
        // �ν��Ͻ�
        var questSystem = QuestSystem.Instance;

        // ��������Ʈ �߰�
        questSystem.onQuestRegistered += (quest) =>
        {
            print($"New Quest:{quest.CodeName} Registered");
            print($"Active Quests Count:{questSystem.ActiveQuests.Count}");
        };

        questSystem.onQuestCompleted += (quest) =>
        {
            print($"Quest:{quest.CodeName} Completed");
            print($"Completed Quests Count:{questSystem.CompletedQuests.Count}");
        };

        // ����Ʈ ���
        var newQuest = questSystem.Register(quest);
        newQuest.onTaskSuccessChanged += (quest, task, currentSuccess, prevSuccess) =>
        {
            print($"Quest:{quest.CodeName}, Task:{task.CodeName}, CurrentSuccess:{currentSuccess}");
        };
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            QuestSystem.Instance.ReceiveReport(category, target, 1);
        }   

        if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Test");
        }
    }
}
