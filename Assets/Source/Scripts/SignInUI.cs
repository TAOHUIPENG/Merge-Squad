using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 签到界面 - 8天连续签到逻辑
/// </summary>
public class SignInUI : MonoBehaviour
{
    private const string DayIndexKey = "SignIn_DayIndex";
    private const string LastClaimedDateKey = "SignIn_LastClaimedDate";

    [Header("签到Item（共8个）")]
    [SerializeField] private SignInItem[] items;

    [Header("每天奖励金币")]
    [SerializeField] private int[] rewards = { 50, 100, 150, 200, 250, 300, 350, 500 };

    [Header("按钮")]
    [SerializeField] private Button signInButton;
    [SerializeField] private Button closeButton;

    [Header("结果弹窗")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private Text resultText;
    [SerializeField] private Button resultCloseButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private int currentDayIndex;
    private bool hasClaimedToday;

    private void OnEnable()
    {
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
        LoadData();
        RefreshAllItems();
        UpdateSignInButtonState();
    }

    private void Start()
    {
        if (signInButton != null)
            signInButton.onClick.AddListener(OnSignIn);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (resultCloseButton != null)
            resultCloseButton.onClick.AddListener(HideResultPopup);

        if (resultPopup != null)
            resultPopup.SetActive(false);
    }

    private void LoadData()
    {
        currentDayIndex = PlayerPrefs.GetInt(DayIndexKey, 0);
        string lastDate = PlayerPrefs.GetString(LastClaimedDateKey, "");
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        hasClaimedToday = (lastDate == today);
    }

    /// <summary>
    /// 刷新所有Item的显示状态
    /// </summary>
    public void RefreshAllItems()
    {
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            bool isClaimed = i < currentDayIndex;
            bool isCurrent = (i == currentDayIndex) && !hasClaimedToday;
            int reward = (i < rewards.Length) ? rewards[i] : 50;

            items[i].Setup(i + 1, reward, isClaimed, isCurrent);
        }
    }

    private void UpdateSignInButtonState()
    {
        if (signInButton == null) return;
        bool canSignIn = !hasClaimedToday && currentDayIndex < 8;
        signInButton.interactable = canSignIn;
    }

    private void OnSignIn()
    {
        if (hasClaimedToday || currentDayIndex >= 8)
        {
            ShowResult(false);
            return;
        }

        // 发放奖励
        int reward = (currentDayIndex < rewards.Length) ? rewards[currentDayIndex] : 50;
        try
        {
            _db.Money.Value += reward;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SignInUI: 发放奖励失败 - {e.Message}");
        }

        // 保存签到进度
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(LastClaimedDateKey, today);

        currentDayIndex++;
        // 8天签到完毕后重置循环
        if (currentDayIndex >= 8)
            currentDayIndex = 0;

        PlayerPrefs.SetInt(DayIndexKey, currentDayIndex);
        PlayerPrefs.Save();

        hasClaimedToday = true;

        // 刷新所有Item状态
        RefreshAllItems();
        UpdateSignInButtonState();

        ShowResult(true);
    }

    private void ShowResult(bool success)
    {
        if (resultPopup == null) return;

        if (resultText != null)
            resultText.text = success ? "签到成功！" : "今日已签到";

        resultPopup.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(AutoHideResult());
    }

    private IEnumerator AutoHideResult()
    {
        yield return new WaitForSeconds(2f);
        HideResultPopup();
    }

    private void HideResultPopup()
    {
        if (resultPopup != null)
            resultPopup.SetActive(false);
    }
}

