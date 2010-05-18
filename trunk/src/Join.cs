using System;
using System.Collections.Generic;
using System.Text;

namespace mnDAL
{
    public enum JoinType
    {
        Inner,
        Left,
        Right
    }

    [Serializable]
    public class Join
    {
        private readonly EntityType m_JoinTo;
        private readonly EntityDbField m_FieldA;
        private readonly EntityDbField m_FieldB;
        private readonly JoinType m_JoinType;
        private Join m_SubJoin;
        private int m_JoinID;

        public Join(EntityDbField fieldA, EntityDbField fieldB, JoinType joinType)
        {
            m_JoinID = 1;
            m_FieldA = fieldA;
            m_FieldB = fieldB;
            m_JoinType = joinType;
        }

        public Join AddSubJoin(Join subJoin)
        {
            m_SubJoin = subJoin;
            m_SubJoin.m_JoinID = m_JoinID + 1;
            return m_SubJoin;
        }

        public Join SubJoin
        {
            get { return m_SubJoin; }
        }

        public string JoinID
        {
            get { return "t" + m_JoinID.ToString(); }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Join currentJoin = this;

            while (null != currentJoin)
            {

                switch (m_JoinType)
                {
                    case JoinType.Inner:
                        sb.Append(" INNER JOIN ");
                        break;
                    case JoinType.Left:
                        sb.Append(" LEFT JOIN ");
                        break;
                    case JoinType.Right:
                        sb.Append(" RIGHT JOIN ");
                        break;
                }

                sb.Append(m_FieldB.EntityType);
                sb.Append(" ON ");
                sb.Append(m_FieldA.EntityType.EntityName);
                sb.Append(".");
                sb.Append(m_FieldA.DbName);
                sb.Append(" = ");
                sb.Append(m_FieldB.EntityType.EntityName);
                sb.Append(".");
                sb.Append(m_FieldB.DbName);

                currentJoin = SubJoin;
            }
            return sb.ToString();
        }

        public static implicit operator string(Join rhs)
        {
            return rhs.ToString();
        }
    }
}
