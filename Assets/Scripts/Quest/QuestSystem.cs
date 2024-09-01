using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    #region Save Path
    private const string kSaveRootPath = "questSystem";
    private const string kActiveQuestsSavePath = "activeQuests";
    private const string kCompletedQuestsSavePath = "completedQuests";
    private const string kActiveAchievementsSavePath = "activeAchievement";
    private const string kCompletedAchievementsSavePath = "completedAchievement";
    #endregion

    #region Events
    public delegate void QuestRegisteredHandler(Quest newQuest);
    public delegate void QuestCompletedHandler(Quest quest);
    public delegate void QuestCanceledHandler(Quest quest);
    #endregion

    private static QuestSystem instance;
    private static bool isApplicationQuitting;

    public static QuestSystem Instance
    {
        get
        {
            if (!isApplicationQuitting && instance == null)
            {
                instance = FindObjectOfType<QuestSystem>();
                if (instance == null)
                {
                    instance = new GameObject("Quest System").AddComponent<QuestSystem>();
                    DontDestroyOnLoad(instance.gameObject);
                }
            }
            return instance;
        }
    }

    private List<Quest> activeQuests = new List<Quest>(); // Ȱ��ȭ�� ����Ʈ�� 
    private List<Quest> completedQuests = new List<Quest>(); // �Ϸ�� ����Ʈ��

    private List<Quest> activeAchievements = new List<Quest>(); // Ȱ��ȭ�� ������
    private List<Quest> completedAchievements = new List<Quest>(); // �Ϸ�� ������

    private QuestDatabase questDatatabase;
    private QuestDatabase achievementDatabase;

    public event QuestRegisteredHandler onQuestRegistered;
    public event QuestCompletedHandler onQuestCompleted;
    public event QuestCanceledHandler onQuestCanceled;

    public event QuestRegisteredHandler onAchievementRegistered;
    public event QuestCompletedHandler onAchievementCompleted;

    public IReadOnlyList<Quest> ActiveQuests => activeQuests;
    public IReadOnlyList<Quest> CompletedQuests => completedQuests;
    public IReadOnlyList<Quest> ActiveAchievements => activeAchievements;
    public IReadOnlyList<Quest> CompletedAchievements => completedAchievements;

    private void Awake()
    {
        questDatatabase = Resources.Load<QuestDatabase>("QuestDatabase");
        achievementDatabase = Resources.Load<QuestDatabase>("AchievementDatabase");
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    /** ����Ʈ�� �ý��ۿ� ����Ѵ� */
    public Quest Register(Quest quest)
    {
        // ����Ʈ ���纻
        var newQuest = quest.Clone();

        // ������ ���
        if (newQuest is Achievement)
        {
            // �Ϸ� ��������Ʈ ���
            newQuest.onCompleted += OnAchievementCompleted;

            // ���������� �߰�
            activeAchievements.Add(newQuest);

            // ����Ʈ ���
            // ������� ��������Ʈ ����
            newQuest.OnRegister();
            onAchievementRegistered?.Invoke(newQuest);
        }
        else
        {
            // �Ϸ� ��������Ʈ ���
            // ��� ��������Ʈ ���
            newQuest.onCompleted += OnQuestCompleted;
            newQuest.onCanceled += OnQuestCanceled;

            // Ȱ��ȭ������ �߰�
            activeQuests.Add(newQuest);

            // ����Ʈ ���
            // ��� ��������Ʈ ����
            newQuest.OnRegister();
            onQuestRegistered?.Invoke(newQuest);
        }

        // ����Ʈ ��ȯ
        return newQuest;
    }

    /** ���� �޴´� */
    public void ReceiveReport(string category, object target, int successCount)
    {
        // Ȱ��ȭ�� ���� �� ����Ʈ ���� �޴´�
        ReceiveReport(activeQuests, category, target, successCount);
        ReceiveReport(activeAchievements, category, target, successCount);
    }

    /** ���� �޴´� */
    public void ReceiveReport(Category category, TaskTarget target, int successCount)
        => ReceiveReport(category.CodeName, target.Value, successCount);

    /** ���� �޴´� */
    private void ReceiveReport(List<Quest> quests, string category, object target, int successCount)
    {
        // ������ foreach �ϴ� ���� �Ϸ�ó���� �Ǹ� ������ �߻��ϱ� ������
        // ToArray�� �纻���� foreach�Ѵ�
        foreach (var quest in quests.ToArray())
            quest.ReceiveReport(category, target, successCount);
    }

    // ����Ʈ�� ��Ͽ� �ִ��� Ȯ���ϴ� �޼����
    public bool ContainsInActiveQuests(Quest quest) => activeQuests.Any(x => x.CodeName == quest.CodeName);

    public bool ContainsInCompleteQuests(Quest quest) => completedQuests.Any(x => x.CodeName == quest.CodeName);

    public bool ContainsInActiveAchievements(Quest quest) => activeAchievements.Any(x => x.CodeName == quest.CodeName);

    public bool ContainsInCompletedAchievements(Quest quest) => completedAchievements.Any(x => x.CodeName == quest.CodeName);

    #region Callback
    /** ����Ʈ �Ϸ��Ѵ� */
    private void OnQuestCompleted(Quest quest)
    {
        // Ȱ��ȭ ����Ʈ �������� ����
        // �Ϸ� ����Ʈ ������ �߰�
        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        // �Ϸ� ��������Ʈ ����
        onQuestCompleted?.Invoke(quest);
    }

    /** ����Ʈ ����Ѵ� */
    private void OnQuestCanceled(Quest quest)
    {
        // Ȱ��ȭ ����Ʈ �������� ����
        // ��� ��������Ʈ ����
        activeQuests.Remove(quest);
        onQuestCanceled?.Invoke(quest);

        // ���� �����ӿ� ����
        Destroy(quest, Time.deltaTime);
    }

    /** ������ �Ϸ�ȴ� */
    private void OnAchievementCompleted(Quest achievement)
    {
        // ������������ ����
        // �Ϸ� ���������� �߰�
        activeAchievements.Remove(achievement);
        completedAchievements.Add(achievement);

        // �Ϸ� ��������Ʈ ����
        onAchievementCompleted?.Invoke(achievement);
    }
    #endregion
}
