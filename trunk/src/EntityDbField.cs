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
using System.Data;

namespace mnDAL {
    [Serializable]
    public class EntityDbField : IComparable<EntityDbField>, IEquatable<EntityDbField> {
        private readonly string m_DbName;
        private readonly SqlDbType m_DbType;
        private readonly int m_DbLength;
        private readonly bool m_IsAutoIncrement;
        private readonly EntityType m_EntityType;

        public EntityDbField(string dbName, SqlDbType dbType, EntityType entityType) {
            m_DbName = dbName;
            m_DbType = dbType;
            m_DbLength = -1;
            m_IsAutoIncrement = false;
            m_EntityType = entityType;
        }

        public EntityDbField(string dbName, SqlDbType dbType, int dbSize, EntityType entityType) {
            m_DbName = dbName;
            m_DbType = dbType;
            m_DbLength = dbSize;
            m_IsAutoIncrement = false;
            m_EntityType = entityType;
        }

        public EntityDbField(string dbName, SqlDbType dbType, bool isAutoIncrement, EntityType entityType) {
            m_DbName = dbName;
            m_DbType = dbType;
            m_DbLength = -1;
            m_IsAutoIncrement = isAutoIncrement;
            m_EntityType = entityType;
        }

        public EntityType EntityType {
            get { return m_EntityType; }
        }

        public bool IsAutoIncrement {
            get { return m_IsAutoIncrement; }
        }

        public string DbName {
            get { return m_DbName; }
        }

        public SqlDbType DbType {
            get { return m_DbType; }
        }

        public int DbLength {
            get { return m_DbLength; }
        }

        public int CompareTo(EntityDbField other) {
            return StringComparer.InvariantCultureIgnoreCase.Compare(DbName, other.DbName);
        }

        public bool Equals(EntityDbField other) {
            return String.Equals(DbName, other.DbName, StringComparison.InvariantCultureIgnoreCase);
        }

        public Join Join(JoinType joinType, EntityDbField rhs) {
            return new Join(this, rhs, joinType);
        }

        public static Expression operator ==(EntityDbField lhs, object rhs) {
            if (null != rhs && rhs.GetType().IsArray) {
                Array vals = (Array)rhs;
                Expression expr = null;

                if (vals.Length < 1) {
                    throw new ArgumentException("Cannot create an expression with an empty array");
                }
                else {
                    expr = new Expression(lhs, ExpressionOperator.EqualTo, vals.GetValue(0));
                }

                for (int i = 1; i < vals.Length; ++i) {
                    expr |= new Expression(lhs, ExpressionOperator.EqualTo, vals.GetValue(i));
                }

                return expr;
            }
            else {
                return new Expression(lhs, ExpressionOperator.EqualTo, rhs);
            }
        }

        public Expression EqualTo(object rhs) {
            return this == rhs;
        }

        public static Expression operator !=(EntityDbField lhs, object rhs) {
            return new Expression(lhs, ExpressionOperator.NotEqualTo, rhs);
        }

        public Expression NotEqualTo(object rhs) {
            return this != rhs;
        }

        public static Expression operator <(EntityDbField lhs, object rhs) {
            return new Expression(lhs, ExpressionOperator.LessThan, rhs);
        }

        public Expression LessThan(object rhs) {
            return this < rhs;
        }

        public static Expression operator <=(EntityDbField lhs, object rhs) {
            return new Expression(lhs, ExpressionOperator.LessThan | ExpressionOperator.EqualTo, rhs);
        }

        public Expression LessThanOrEqualTo(object rhs) {
            return this <= rhs;
        }

        public static Expression operator >(EntityDbField lhs, object rhs) {
            return new Expression(lhs, ExpressionOperator.GreaterThan, rhs);
        }

        public Expression GreaterThan(object rhs) {
            return this > rhs;
        }

        public static Expression operator >=(EntityDbField lhs, object rhs) {
            return new Expression(lhs, ExpressionOperator.GreaterThan | ExpressionOperator.EqualTo, rhs);
        }

        public Expression GreaterThanOrEqualTo(object rhs) {
            return this >= rhs;
        }

        public static Expression operator %(EntityDbField lhs, string rhs) {
            return new Expression(lhs, ExpressionOperator.Like, rhs);
        }

        public Expression Like(string rhs) {
            return new Expression(this, ExpressionOperator.Like, rhs);
        }
    }
}
