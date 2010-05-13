using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace mnDAL
{
    [Flags]
    public enum ExpressionOperator : uint {
        EqualTo = 0x01,
        NotEqualTo = 0x10,
        LessThan = 0x02,
        GreaterThan = 0x04,
        Like = 0x08
    }

    public enum SortDirection {
        Ascending,
        Descending,
        Random
    }

    [Serializable]
    public class SortExpression {
        private readonly EntityDbField  m_SortDbField;
        private readonly SortDirection  m_SorDirection;

        public SortExpression(EntityDbField field, SortDirection direction) {
            m_SortDbField = field;
            m_SorDirection = direction;
        }

        public EntityDbField SortByField {
            get{ return m_SortDbField; }
        }

        public SortDirection Direction {
            get{ return m_SorDirection; }
        }

        public static implicit operator string(SortExpression sort) {
            StringBuilder expr = new StringBuilder();
            expr.Append(" ORDER BY ");

            if(sort.Direction == SortDirection.Random) {
                expr.Append("NEWID()");
            }
            else {
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
        private readonly EntityDbField      m_Field;
        private readonly ExpressionOperator m_Expr;
        private readonly object             m_Value;

        private string  m_ExpressionID;

        protected Expression()
        {
        }

        public Expression(EntityDbField field, ExpressionOperator op, object value) 
        {
            m_Field = field;
            m_Expr = op;
            m_Value = value;
            if(null == m_Value)
            {
                m_Value = DBNull.Value;
            }

            m_ExpressionID = "expr0";
        }

        public virtual IEnumerable<Expression> Expressions
        {
            get{ yield return this; }
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
            if(m_Expr == ExpressionOperator.EqualTo)
            {
                return entity.GetValueForDbField(m_Field).Equals(Value);
            }
            else if ( (m_Expr & ExpressionOperator.GreaterThan) > 0 )
            {
                if( (m_Expr & ExpressionOperator.EqualTo) > 0)
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) >= 0;
                }
                else 
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) > 0;
                }
            }
            else if( (m_Expr & ExpressionOperator.LessThan) > 0)
            {
                if( (m_Expr & ExpressionOperator.EqualTo) > 0)
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) <= 0;
                }
                else
                {
                    return ((IComparable)(entity.GetValueForDbField(m_Field))).CompareTo(Value) < 0;
                }
                
            }
            else if(m_Expr == ExpressionOperator.NotEqualTo)
            {
                return !entity.GetValueForDbField(m_Field).Equals(Value);
            }
            else if( (m_Expr & ExpressionOperator.Like) > 0)
            {
                if( (Value.GetType() != typeof(String)) || (entity.GetValueForDbField(m_Field).GetType() != typeof(String)) )
                {
                    return false;
                }
                else
                {
                    if( (m_Expr & ExpressionOperator.NotEqualTo) > 0 )
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
            expr.Append(m_Field.DbName);
            expr.Append(" ");

            if(m_Expr == ExpressionOperator.NotEqualTo) 
            {
                if( null == m_Value || (m_Value.GetType() == DBNull.Value.GetType()) )
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
                if(m_Expr == ExpressionOperator.Like)
                {
                    expr.Append("LIKE");
                }
                if((m_Expr & ExpressionOperator.LessThan) > 0) 
                {
                    expr.Append("<");
                }
                else if((m_Expr & ExpressionOperator.GreaterThan) > 0) 
                {
                    expr.Append(">");
                }
                if((m_Expr & ExpressionOperator.EqualTo) > 0) 
                {
                    if( null == m_Value || (this.m_Value.GetType() == DBNull.Value.GetType()) )
                    {
                        expr.Append("IS NULL");
                    }
                    else
                    {
                        expr.Append("=");
                    }
                }
            }

            if(null != m_Value && (m_Value.GetType() != DBNull.Value.GetType()) )
            {
                expr.Append(" @");
                expr.Append(ExpressionID);
                if(m_Expr == ExpressionOperator.Like)
                {
                    expr.Append(" + '%'");
                }
            }
            expr.Append(" ");

            return expr.ToString();
        }

        public EntityDbField DbField {
            get{ return m_Field; }
        }

        public object Value {
            get{ return m_Value; }
        }
    }

    [Serializable]
    public class CombinedExpression : Expression
    {
        private string      m_Combine;
        private Expression  m_LHS;
        private Expression  m_RHS;

        public CombinedExpression(Expression lhs, string op, Expression rhs)
        {
            m_LHS = lhs;
            m_Combine = op;
            m_RHS = rhs;

            int id = 0;
            foreach(Expression expr in lhs.Expressions)
            {
                expr.ExpressionID = "expr" + id.ToString();
                id++;
            }

            foreach(Expression expr in rhs.Expressions)
            {
                expr.ExpressionID = "expr" + id.ToString();
                id++;
            }

        }

        public override IEnumerable<Expression> Expressions
        {
            get
            {
                foreach(Expression exp in m_LHS.Expressions)
                {
                    yield return exp;
                }

                foreach(Expression exp in m_RHS.Expressions)
                {
                    yield return exp;
                }
            }
        }

        public override bool Eval(EntityBase entity)
        {
            if(m_Combine.Contains("AND"))
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

    [Serializable]
    public class EntityDbField : IComparable<EntityDbField>, IEquatable<EntityDbField> 
    {
        private readonly string     m_DbName;
        private readonly SqlDbType  m_DbType;
        private readonly int        m_DbLength;
        private readonly bool       m_IsAutoIncrement;

        public EntityDbField(string dbName, SqlDbType dbType) 
        {
            m_DbName = dbName;
            m_DbType = dbType;
            m_DbLength = -1;
            m_IsAutoIncrement = false;
        }

        public EntityDbField(string dbName, SqlDbType dbType, int dbSize) 
        {
            m_DbName = dbName;
            m_DbType = dbType;
            m_DbLength = dbSize;
            m_IsAutoIncrement = false;
        }

        public EntityDbField(string dbName, SqlDbType dbType, bool isAutoIncrement) 
        {
            m_DbName = dbName;
            m_DbType = dbType;
            m_DbLength = -1;
            m_IsAutoIncrement = isAutoIncrement;
        }

        public bool IsAutoIncrement 
        {
            get{ return m_IsAutoIncrement; }
        }

        public string DbName 
        {
            get{ return m_DbName; }
        }

        public SqlDbType DbType 
        {
            get{ return m_DbType; }
        }

        public int DbLength 
        {
            get{ return m_DbLength; }
        }

        public int CompareTo(EntityDbField other) 
        {
            return StringComparer.InvariantCultureIgnoreCase.Compare(DbName, other.DbName);
        }

        public bool Equals(EntityDbField other) 
        {
            return String.Equals(DbName, other.DbName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static Expression operator ==(EntityDbField lhs, object rhs) 
        {
            if(null != rhs && rhs.GetType().IsArray)
            {
                Array       vals = (Array)rhs;
                Expression  expr = null;

                if(vals.Length < 1)
                {
                    throw new ArgumentException("Cannot create an expression with an empty array");
                }
                else 
                {
                    expr = new Expression(lhs, ExpressionOperator.EqualTo, vals.GetValue(0));
                }

                for(int i = 1; i < vals.Length; ++i)
                {
                    expr |= new Expression(lhs, ExpressionOperator.EqualTo, vals.GetValue(i));
                }

                return expr;
            }
            else
            {
                return new Expression(lhs, ExpressionOperator.EqualTo, rhs);
            }
        }

        public static Expression operator !=(EntityDbField lhs, object rhs) 
        {
            return new Expression(lhs, ExpressionOperator.NotEqualTo, rhs);
        }

        public static Expression operator <(EntityDbField lhs, object rhs) 
        {
            return new Expression(lhs, ExpressionOperator.LessThan, rhs);
        }
        public static Expression operator <=(EntityDbField lhs, object rhs)
        {
            return new Expression(lhs, ExpressionOperator.LessThan | ExpressionOperator.EqualTo, rhs);
        }

        public static Expression operator >(EntityDbField lhs, object rhs) 
        {
            return new Expression(lhs, ExpressionOperator.GreaterThan, rhs);
        }

        public static Expression operator >=(EntityDbField lhs, object rhs)
        {
            return new Expression(lhs, ExpressionOperator.GreaterThan | ExpressionOperator.EqualTo, rhs);
        }

        public static Expression operator %(EntityDbField lhs, string rhs)
        {
            return new Expression(lhs, ExpressionOperator.Like, rhs);
        }
    }

    [Serializable]
    public class EntityFieldMapping : IComparable<EntityDbField>, IComparable<EntityFieldMapping> {

        private EntityDbField   m_DbField;
        private FieldInfo       m_EntityField;

        internal EntityFieldMapping(EntityDbField dbField) {
            m_DbField = dbField;
        }

        public EntityFieldMapping(EntityDbField dbField, string entityField, Type entityType) {
            m_DbField = dbField;
            m_EntityField = entityType.GetField(entityField, BindingFlags.Instance | BindingFlags.NonPublic);
            if(null == m_EntityField) {
                throw new FieldAccessException("Couldn't find '" + entityField + "'");
            }
        }

        public EntityDbField DbField {
            get {
                return m_DbField;
            }
        }

        public FieldInfo EntityField {
            get {
                return m_EntityField;
            }
        }

        public int CompareTo(EntityDbField other) {
            return DbField.CompareTo(other);
        }

        public int CompareTo(EntityFieldMapping other) {
            return CompareTo(other.DbField);
        }
    }

    [Serializable]
    public abstract class EntityBase {

        private List<EntityFieldMapping>    m_FieldMap;
        private List<EntityDbField>         m_ModifiedFields;
        private readonly string             m_EntityDbName;

        public EntityBase(string entityName) {
            m_EntityDbName = entityName;
            m_FieldMap = new List<EntityFieldMapping>();
            m_ModifiedFields = new List<EntityDbField>();
        }

        protected void AddFieldMapping(EntityFieldMapping mapping) 
        {
            if(null == m_FieldMap) {
                throw new ArgumentNullException("EntityFieldMapping");
            }

            int pos = m_FieldMap.BinarySearch(mapping);
            if(pos < 0) {
                pos = ~pos;
            }

            m_FieldMap.Insert(pos, mapping);
        }

        protected void AddFieldMapping(EntityDbField dbfield, string objfield, Type objtype)
        {
            AddFieldMapping(new EntityFieldMapping(dbfield, objfield, objtype));
        }

        protected void AddFieldMapping(EntityDbField dbfield, string objfield)
        {
            AddFieldMapping(new EntityFieldMapping(dbfield, objfield, this.GetType()));
        }

        protected void SetFieldModified(EntityDbField field, bool modified) {
            int pos = m_ModifiedFields.BinarySearch(field);

            if(modified) {
                if(pos < 0) {
                    pos = ~pos;
                    m_ModifiedFields.Insert(pos, field);
                }
            }
            else {
                if(pos >= 0) {
                    m_ModifiedFields.RemoveAt(pos);
                }
            }
        }

        public EntityDbField[] Fields {
            get{ 
                EntityDbField[] flds = new EntityDbField[m_FieldMap.Count];
                for(int i = 0; i < m_FieldMap.Count; ++i) {
                    flds[i] = m_FieldMap[i].DbField;
                }

                return flds;
            }
        }

        protected List<EntityFieldMapping> FieldMappings {
            get{ return m_FieldMap; }
        }

        public virtual bool Updatable {
            get { return true; }
        }

        public string EntityDbName {
            get { return m_EntityDbName; }
        }

        public virtual object GetValueForDbField(EntityDbField field) {
            int pos = m_FieldMap.BinarySearch(new EntityFieldMapping(field));
            return m_FieldMap[pos].EntityField.GetValue(this);
        }

        public virtual void SetValueForDbField(EntityDbField field, object value) {
            int pos = m_FieldMap.BinarySearch(new EntityFieldMapping(field));
            m_FieldMap[pos].EntityField.SetValue(this, value);
        }

        public virtual bool GetDbFieldValueChanged(EntityDbField field) {
            return (m_ModifiedFields.BinarySearch(field) >= 0);
        }

        public abstract EntityDbField GetIdentifierDbField();
    }
}
