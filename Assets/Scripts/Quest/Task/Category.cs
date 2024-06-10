using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Category", fileName = "Category")]
public class Category : ScriptableObject, IEquatable<Category> // ��ü�� ����� �񱳸� ���� ��� >> �ڽ� ���ϱ�
{
    [SerializeField] private string codeName;
    [SerializeField] private string displayName;

    public string CodeName => codeName;
    public string DisplayName => displayName;

    #region Operator
    public bool Equals(Category other)
    {
        // �ٸ� ��ü�� null���� Ȯ��
        // is �����ڴ� Ÿ���� Ȯ���ϰ�
        // == �����ڴ� ������ ���ϼ��� Ȯ���Ѵ�
        if (other is null)
            return false;
        // �� ������ ������ ��ü�� ����Ű���� Ȯ��
        if (ReferenceEquals(other, this))
            return true;
        // �� ��ü�� ������ Ÿ������ Ȯ��
        if (GetType() != other.GetType())
            return false;

        // codeName �ʵ尡 ������ Ȯ��
        return codeName == other.codeName;
    }

    // �ؽ� �ڵ� ����
    public override int GetHashCode() => (CodeName, DisplayName).GetHashCode();

    // object Ÿ���� ��� �񱳸� ����
    public override bool Equals(object other) => Equals(other as Category);

    public static bool operator ==(Category lhs, string rhs)
    {
        // null ���� Ȯ��
        if(lhs is null)
            return ReferenceEquals(rhs, null); // rhs�� null �̸� true
        return lhs.codeName == rhs || lhs.displayName == rhs;
    }

    public static bool operator !=(Category lhs, string rhs) => !(lhs == rhs);
    #endregion // Operator
}
