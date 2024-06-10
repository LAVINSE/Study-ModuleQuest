using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Category", fileName = "Category")]
public class Category : ScriptableObject, IEquatable<Category> // 객체의 동등성을 비교를 위한 방법 >> 박싱 피하기
{
    [SerializeField] private string codeName;
    [SerializeField] private string displayName;

    public string CodeName => codeName;
    public string DisplayName => displayName;

    #region Operator
    public bool Equals(Category other)
    {
        // 다른 객체가 null인지 확인
        // is 연산자는 타입을 확인하고
        // == 연산자는 참조의 동일성을 확인한다
        if (other is null)
            return false;
        // 두 참조가 동일한 객체를 가리키는지 확인
        if (ReferenceEquals(other, this))
            return true;
        // 두 객체가 동일한 타입인지 확인
        if (GetType() != other.GetType())
            return false;

        // codeName 필드가 같은지 확인
        return codeName == other.codeName;
    }

    // 해시 코드 생성
    public override int GetHashCode() => (CodeName, DisplayName).GetHashCode();

    // object 타입의 동등성 비교를 수행
    public override bool Equals(object other) => Equals(other as Category);

    public static bool operator ==(Category lhs, string rhs)
    {
        // null 인지 확인
        if(lhs is null)
            return ReferenceEquals(rhs, null); // rhs도 null 이면 true
        return lhs.codeName == rhs || lhs.displayName == rhs;
    }

    public static bool operator !=(Category lhs, string rhs) => !(lhs == rhs);
    #endregion // Operator
}
