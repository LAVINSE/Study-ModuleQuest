using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Achievement", fileName = "Achievement_")]
public class Achievement : Quest
{
    // IsCancelable을 체크했어도 취소가 불가능
    public override bool IsCancelable => false;
    public override bool IsSavable => true;

    public override void Cancel()
    {
        // 취소기능을 사용할 경우 에러
        Debug.LogAssertion("Achievement can't be canceled");
    }
}
