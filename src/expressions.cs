using System;
using System.Collections.Generic;
using System.Text;

namespace mnDAL
{
    [Flags]
    public enum ExpressionOperator : uint
    {
        EqualTo = 0x01,
        NotEqualTo = 0x10,
        LessThan = 0x02,
        GreaterThan = 0x04,
        Like = 0x08
    }

    public enum SortDirection
    {
        Ascending,
        Descending,
        Random
    }

    [Serializable]
    public class SortExpression
    {
        private readonly EntityDbField m_SortDbField;
        private readonly SortDirection m_SorDirection;

        public SortExpression(EntityDbField field, SortDirection direction)
        {
            m_SortDbField = field;
            m_SorDirection = direction;
        }

        public EntityDbField SortByField
        {
            get { return m_SortDbField; }
        }

        public SortDirection Direction
        {
            get { return m_SorDirection; }
        }

        public static implicit operator string(SortExpression sort)
        {
            StringBuilder expr = new StringBuilder();
            expr.Append(" ORDER BY ");

            if (sort.Direction == SortDirection.Random)
            {
                expr.Append("NEWID()");
            }
            else
            {
                expr.Append(sort.SortByField.EntityType.EntityName);
                expr.Append(".");
                expr.Append(sort.SortByField.DbName);
                expr.Append(" ");
                expr.Append(sort.Direction == SortDirection.Ascending ? "ASC" : "DESC");
            }

            return expr.ToString();
        }
    }

    [Serializable]
    public class Expression
    {
        private readonly EntityDbField m_Field;
        private readonly ExpressionOperator m_Expr;
        private readonly object m_Value;

        private string m_ExpressionID;

        protected Expression()
        {
        }

        public Expression(EntityDbField field, ExpressionOperator op, object value)
        {
            m_Field = field;
            m_Expr = op;
            m_Value = value;
            if (null == m_Value)
            {
                m_Value = DBNull.Value;
            }

            m_ExpressionID = "expr0";
        }

        public virtual IEnumerable<Expression> Expressions
        {
            get { yield return this; }
        }

        public static implicit operator string(Expression expression)
        {
            return expression.ToString();
        }

        public static CombinedExpression operator &(Expression lhs, Expression rhs)
        {
            return new CombinedExpression(lhs, " AND ", rhs);
        }

        public static CombinedExpression operator |(Expression lhs, Expression rhs)
        {
            return new CombinedExpression(lhs, " OR ", rhs);
        }

        public string ExpressionID
        {
            get { return m_ExpressionID; }
            internal set { m_ExpressionID = value; }
        }

        public virtual bool Eval(EntityBase entity)
        {
            if (m_Expr == ExpressionOperator.EqualTo)
            {
                return entity.GetValueForDbField(m_Field).Equals(Value);
            }
            else if ((m_Expr & ExpressionOperator.GreaterThan) > 0)
            {
                if ((m_Expr & ExpressionOperator.EqualTo) > 0)
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) >= 0;
                }
                else
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) > 0;
                }
            }
            else if ((m_Expr & ExpressionOperator.LessThan) > 0)
            {
                if ((m_Expr & ExpressionOperator.EqualTo) > 0)
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) <= 0;
                }
                else
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) < 0;
                }

            }
            else if (m_Expr == ExpressionOperator.NotEqualTo)
            {
                return !entity.GetValueForDbField(m_Field).Equals(Value);
            }
            else if ((m_Expr & ExpressionOperator.Like) > 0)
            {
                if ((Value.GetType() != typeof(String)) || (entity.GetValueForDbField(m_Field).GetType() != typeof(String)))
                {
                    return false;
                }
                else
                {
                    if ((m_Expr & ExpressionOperator.NotEqualTo) > 0)
                    {
                        return !((String)(entity.GetValueForDbField(m_Field))).StartsWith(Value.ToString());
                    }
                    else
                    {
                        return ((String)(entity.GetValueForDbField(m_Field))).StartsWith(Value.ToString());
                    }
                }
            }

            return false;
        }

        public override string ToString()
        {
            StringBuilder expr = new StringBuilder();
            expr.Append(m_Field.EntityType.EntityName);
            expr.Append(".");
            expr.Append(m_Field.DbName);
            expr.Append(" ");

            if (m_Expr == ExpressionOperator.NotEqualTo)
            {
                if (null == m_Value || (m_Value.GetType() == DBNull.Value.GetType()))
                {
                    expr.Append("IS NOT NULL");
                }
                else
                {
                    expr.Append("!=");
                }
            }
            else
            {
                if (m_Expr == ExpressionOperator.Like)
                {
                    expr.Append("LIKE");
                }
                if ((m_Expr & ExpressionOperator.LessThan) > 0)
                {
                    expr.Append("<");
                }
                else if ((m_Expr & ExpressionOperator.GreaterThan) > 0)
                {
                    expr.Append(">");
                }
                if ((m_Expr & ExpressionOperator.EqualTo) > 0)
                {
                    if (null == m_Value || (this.m_Value.GetType() == DBNull.Value.GetType()))
                    {
                        expr.Append("IS NULL");
                    }
                    else
                    {
                        expr.Append("=");
                    }
                }
            }

            if (null != m_Value && (m_Value.GetType() != DBNull.Value.GetType()))
            {
                expr.Append(" @");
                expr.Append(ExpressionID);
                if (m_Expr == ExpressionOperator.Like)
                {
                    expr.Append(" + '%'");
                }
            }
            expr.Append(" ");

            return expr.ToString();
        }

        public EntityDbField DbField
        {
            get { return m_Field; }
        }

        public object Value
        {
            get { return m_Value; }
        }
    }

    [Serializable]
    public class CombinedExpression : Expression
    {
        private string m_Combine;
        private Expression m_LHS;
        private Expression m_RHS;

        public CombinedExpression(Expression lhs, string op, Expression rhs)
        {
            m_LHS = lhs;
            m_Combine = op;
            m_RHS = rhs;

            int id = 0;
            foreach (Expression expr in lhs.Expressions)
            {
                expr.ExpressionID = "expr" + id.ToString();
                id++;
            }

            foreach (Expression expr in rhs.Expressions)
            {
                expr.ExpressionID = "expr" + id.ToString();
                id++;
            }

        }

        public override IEnumerable<Expression> Expressions
        {
            get
            {
                foreach (Expression exp in m_LHS.Expressions)
                {
                    yield return exp;
                }

                foreach (Expression exp in m_RHS.Expressions)
                {
                    yield return exp;
                }
            }
        }

        public override bool Eval(EntityBase entity)
        {
            if (m_Combine.Contains("AND"))
            {
                return m_LHS.Eval(entity) && m_RHS.Eval(entity);
            }
            else
            {
                return m_LHS.Eval(entity) || m_RHS.Eval(entity);
            }
        }

        public static CombinedExpression operator &(CombinedExpression lhs, Expression rhs)
        {
            return new CombinedExpression(lhs, " AND ", rhs);
        }

        public static CombinedExpression operator |(CombinedExpression lhs, Expression rhs)
        {
            return new CombinedExpression(lhs, " OR ", rhs);
        }

        public static CombinedExpression operator &(Expression lhs, CombinedExpression rhs)
        {
            return new CombinedExpression(lhs, " AND ", rhs);
        }

        public static CombinedExpression operator |(Expression lhs, CombinedExpression rhs)
        {
            return new CombinedExpression(lhs, " OR ", rhs);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("( ");
            sb.Append(m_LHS);
            sb.Append(m_Combine);
            sb.Append(m_RHS);
            sb.Append(" ) ");

            return sb.ToString();
        }

    }
}
