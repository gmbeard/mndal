/*
 *	Copyright 2010 Greg Beard
 *
 *	This file is part of mnDAL (http://code.google.com/p/mndal)
 *
 *	mnDAL is free software: you can redistribute it and/or modify
 *	it under the terms of the Lesser GNU General Public License as published by
 *	the Free Software Foundation, either version 3 of the License, or
 *	(at your option) any later version.
 *
 *	mnDAL is distributed in the hope that it will be useful,
 *	but WITHOUT ANY WARRANTY; without even the implied warranty of
 *	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *	Lesser GNU General Public License for more details.
 *
 *	You should have received a copy of the Lesser GNU General Public License
 *	along with mnDAL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace mnDAL {
    public enum JoinType {
        Inner,
        Left,
        Right
    }

    [Serializable]
    public class Join {
        private readonly EntityDbField m_FieldA;
        private readonly EntityDbField m_FieldB;
        private readonly JoinType m_JoinType;
        private Join m_SubJoin;
        private int m_JoinID;

        public Join(EntityDbField fieldA, EntityDbField fieldB, JoinType joinType) {
            m_JoinID = 1;
            m_FieldA = fieldA;
            m_FieldB = fieldB;
            m_JoinType = joinType;
        }

        public Join AddSubJoin(Join subJoin) {
            if (null == m_SubJoin) {
                m_SubJoin = subJoin;
                m_SubJoin.m_JoinID = m_JoinID + 1;
            }
            else {
                m_SubJoin.AddSubJoin(subJoin);
            }
            return this;
        }

        public Join SubJoin {
            get { return m_SubJoin; }
        }

        public string JoinID {
            get { return "t" + m_JoinID.ToString(); }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            Join currentJoin = this;

            while (null != currentJoin) {

                switch (currentJoin.m_JoinType) {
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

                sb.Append(currentJoin.m_FieldB.EntityType);
                sb.Append(" ON ");
                sb.Append(currentJoin.m_FieldA.EntityType.EntityName);
                sb.Append(".");
                sb.Append(currentJoin.m_FieldA.DbName);
                sb.Append(" = ");
                sb.Append(currentJoin.m_FieldB.EntityType.EntityName);
                sb.Append(".");
                sb.Append(currentJoin.m_FieldB.DbName);

                currentJoin = currentJoin.SubJoin;
            }
            return sb.ToString();
        }

        public static implicit operator string(Join rhs) {
            return rhs.ToString();
        }
    }
}
