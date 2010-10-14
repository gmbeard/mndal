using System;
using System.Collections.Generic;
using System.Text;

namespace mnDAL {
    public sealed class DynamicEntity : EntityBase {

        private Object[] m_FieldValues;
        List<EntityDbField> m_Fields;

        internal DynamicEntity(EntityDbField[] fields)
            : base("[__DynamicEntity__" + new Guid().ToString() + "]") {
            if (null == fields || fields.Length == 0) {
                throw new ArgumentException("DynamicEntity must be created with at least one EntityDbField object");
            }

            m_Fields = new List<EntityDbField>();
            int pos = 0;

            foreach (EntityDbField field in fields) {
                pos = m_Fields.BinarySearch(field);
                if (pos >= 0) {
                    throw new ArgumentException("A duplicate field definition was encountered");
                }
                else {
                    m_Fields.Insert(~pos, field);
                    AddFieldMapping(field, "m_FieldValues");
                }
            }

            m_FieldValues = new Object[m_Fields.Count];
        }

        public Object this[string fieldName] {
            get {
                int pos = m_Fields.BinarySearch(new EntityDbField(fieldName, System.Data.SqlDbType.Variant, GetDbType()));
                if (pos >= 0) {
                    return m_FieldValues[pos];
                }
                else {
                    throw new ArgumentException(
                        String.Format(
                            "DynamicEntity object doesn't contain the field '{0}'",
                            fieldName));
                }
            }
            set {
                int pos = m_Fields.BinarySearch(new EntityDbField(fieldName, System.Data.SqlDbType.Variant, GetDbType()));
                if (pos >= 0) {
                    m_FieldValues[pos] = value;
                }
                else {
                    throw new ArgumentException(
                        String.Format(
                            "DynamicEntity object doesn't contain the field '{0}'",
                            fieldName));
                }
            }
        }

        public override EntityDbField[] GetIdentifierDbFields() {
            return new EntityDbField[] { };
            //return m_Fields.Find(delegate(EntityDbField item) {
            //    return item.IsAutoIncrement;
            //});
        }

        public override object GetValueForDbField(EntityDbField field) {
            return m_FieldValues[m_Fields.BinarySearch(field)];
        }

        public override void SetValueForDbField(EntityDbField field, object value) {
            m_FieldValues[m_Fields.BinarySearch(field)] = value;
        }

        public override bool Updatable {
            get {
                return false;
            }
        }
    }
}
