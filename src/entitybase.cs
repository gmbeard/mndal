using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace mnDAL
{
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
        private EntityType m_DbType;

        public EntityBase(string entityName)
        {
            m_DbType = new EntityType(entityName);
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
            get { return GetDbType().EntityName; }
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

        public virtual EntityType GetDbType()
        {
            return m_DbType;
        }
    }
}
