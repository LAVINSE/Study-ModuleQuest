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

    private List<Quest> activeQuests = new List<Quest>(); // 활성화된 퀘스트들 
    private List<Quest> completedQuests = new List<Quest>(); // 완료된 퀘스트들

    private List<Quest> activeAchievements = new List<Quest>(); // 활성화된 업적들
    private List<Quest> completedAchievements = new List<Quest>(); // 완료된 업적들

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

    /** 퀘스트를 시스템에 등록한다 */
    public Quest Register(Quest quest)
    {
        // 퀘스트 복사본
        var newQuest = quest.Clone();

        // 업적일 경우
        if (newQuest is Achievement)
        {
            // 완료 델리게이트 등록
            newQuest.onCompleted += OnAchievementCompleted;

            // 업적모음에 추가
            activeAchievements.Add(newQuest);

            // 퀘스트 등록
            // 업적등록 델리게이트 실행
            newQuest.OnRegister();
            onAchievementRegistered?.Invoke(newQuest);
        }
        else
        {
            // 완료 델리게이트 등록
            // 취소 델리게이트 등록
            newQuest.onCompleted += OnQuestCompleted;
            newQuest.onCanceled += OnQuestCanceled;

            // 활성화모음에 추가
            activeQuests.Add(newQuest);

            // 퀘스트 등록
            // 등록 델리게이트 실행
            newQuest.OnRegister();
            onQuestRegistered?.Invoke(newQuest);
        }

        // 퀘스트 반환
        return newQuest;
    }

    /** 보고를 받는다 */
    public void ReceiveReport(string category, object target, int successCount)
    {
        // 활성화된 업적 및 퀘스트 보고를 받는다
        ReceiveReport(activeQuests, category, target, successCount);
        ReceiveReport(activeAchievements, category, target, successCount);
    }

    /** 보고를 받는다 */
    public void ReceiveReport(Category category, TaskTarget target, int successCount)
        => ReceiveReport(category.CodeName, target.Value, successCount);

    /** 보고를 받는다 */
    private void ReceiveReport(List<Quest> quests, string category, object target, int successCount)
    {
        // 원본을 foreach 하는 도중 완료처리가 되면 에러가 발생하기 때문에
        // ToArray로 사본으로 foreach한다
        foreach (var quest in quests.ToArray())
            quest.ReceiveReport(category, target, successCount);
    }

    // 퀘스트가 목록에 있는지 확인하는 메서드들
    public bool ContainsInActiveQuests(Quest quest) => activeQuests.Any(x => x.CodeName == quest.CodeName);

    public bool ContainsInCompleteQuests(Quest quest) => completedQuests.Any(x => x.CodeName == quest.CodeName);

    public bool ContainsInActiveAchievements(Quest quest) => activeAchievements.Any(x => x.CodeName == quest.CodeName);

    public bool ContainsInCompletedAchievements(Quest quest) => completedAchievements.Any(x => x.CodeName == quest.CodeName);

    #region Callback
    /** 퀘스트 완료한다 */
    private void OnQuestCompleted(Quest quest)
    {
        // 활성화 퀘스트 모음에서 제거
        // 완료 퀘스트 모음에 추가
        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        // 완료 델리게이트 실행
        onQuestCompleted?.Invoke(quest);
    }

    /** 퀘스트 취소한다 */
    private void OnQuestCanceled(Quest quest)
    {
        // 활성화 퀘스트 모음에서 제거
        // 취소 델리게이트 실행
        activeQuests.Remove(quest);
        onQuestCanceled?.Invoke(quest);

        // 다음 프레임에 제거
        Destroy(quest, Time.deltaTime);
    }

    /** 업적이 완료된다 */
    private void OnAchievementCompleted(Quest achievement)
    {
        // 업적모음에서 제거
        // 완료 업적모음에 추가
        activeAchievements.Remove(achievement);
        completedAchievements.Add(achievement);

        // 완료 델리게이트 실행
        onAchievementCompleted?.Invoke(achievement);
    }
    #endregion
}
