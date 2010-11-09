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
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace mnDAL {
    [Serializable]
    public class EntityFieldMapping :
        IComparable<EntityDbField>,
        IComparable<EntityFieldMapping> {

        private EntityDbField m_DbField;
        private FieldInfo m_EntityField;

        internal EntityFieldMapping(EntityDbField dbField) {
            m_DbField = dbField;
        }

        public EntityFieldMapping(EntityDbField dbField, string entityField, Type entityType) {
            m_DbField = dbField;
            m_EntityField = entityType.GetField(entityField, BindingFlags.Instance | BindingFlags.NonPublic);
            if (null == m_EntityField) {
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
    public abstract class EntityBase : IXmlSerializable {

        private List<EntityFieldMapping> m_FieldMap;
        private List<EntityDbField> m_ModifiedFields;
        private EntityType m_DbType;

        public EntityBase(string entityName) {
            m_DbType = new EntityType(entityName);
            m_FieldMap = new List<EntityFieldMapping>();
            m_ModifiedFields = new List<EntityDbField>();
        }

        protected void AddFieldMapping(EntityFieldMapping mapping) {
            if (null == m_FieldMap) {
                throw new ArgumentNullException("EntityFieldMapping");
            }

            int pos = m_FieldMap.BinarySearch(mapping);
            if (pos < 0) {
                pos = ~pos;
            }

            m_FieldMap.Insert(pos, mapping);
        }

        protected void AddFieldMapping(EntityDbField dbfield, string objfield, Type objtype) {
            AddFieldMapping(new EntityFieldMapping(dbfield, objfield, objtype));
        }

        protected void AddFieldMapping(EntityDbField dbfield, string objfield) {
            AddFieldMapping(new EntityFieldMapping(dbfield, objfield, this.GetType()));
        }

        protected void SetFieldModified(EntityDbField field, bool modified) {
            int pos = m_ModifiedFields.BinarySearch(field);

            if (modified) {
                if (pos < 0) {
                    pos = ~pos;
                    m_ModifiedFields.Insert(pos, field);
                }
            }
            else {
                if (pos >= 0) {
                    m_ModifiedFields.RemoveAt(pos);
                }
            }
        }

        public EntityDbField[] Fields {
            get {
                EntityDbField[] flds = new EntityDbField[m_FieldMap.Count];
                for (int i = 0; i < m_FieldMap.Count; ++i) {
                    flds[i] = m_FieldMap[i].DbField;
                }

                return flds;
            }
        }

        protected List<EntityFieldMapping> FieldMappings {
            get { return m_FieldMap; }
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

        public abstract EntityDbField[] GetIdentifierDbFields();

        public virtual EntityType GetDbType() {
            return m_DbType;
        }

        public System.Xml.Schema.XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader r) {

            using (XmlReader reader = r.ReadSubtree()) {

                if (reader.Read()) {
                    while (reader.Read()) {
                        if (reader.NodeType == System.Xml.XmlNodeType.Element && !reader.IsEmptyElement) {
                            EntityDbField field = new EntityDbField(
                                XmlConvert.EncodeLocalName(reader.LocalName),
                                (SqlDbType)Enum.Parse(typeof(SqlDbType), reader["dbType"]),
                                Int32.Parse(reader["length"]),
                                GetDbType());

                            int pos;
                            if ((pos = m_FieldMap.BinarySearch(new EntityFieldMapping(field))) >= 0) {
                                if (m_FieldMap[pos].EntityField.FieldType == typeof(Byte[])) {
                                    using (MemoryStream ms = new MemoryStream()) {
                                        Byte[] buffer = new Byte[1024];
                                        int read = 0;
                                        while ((read = reader.ReadElementContentAsBase64(buffer, 0, 1024)) > 0) {
                                            ms.Write(buffer, 0, read);
                                        }

                                        m_FieldMap[pos].EntityField.SetValue(this, ms.ToArray());
                                        ms.Close();
                                    }
                                }
                                else if (m_FieldMap[pos].EntityField.FieldType.IsGenericType && m_FieldMap[pos].EntityField.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                                    if (m_FieldMap[pos].EntityField.FieldType.GetGenericArguments()[0] == typeof(Guid)) {
                                        m_FieldMap[pos].EntityField.SetValue(this, new Guid(reader.ReadElementContentAsString()));
                                    }
                                    else {
                                        m_FieldMap[pos].EntityField.SetValue(this, reader.ReadElementContentAs(m_FieldMap[pos].EntityField.FieldType.GetGenericArguments()[0], null));
                                    }
                                }
                                else {
                                    m_FieldMap[pos].EntityField.SetValue(this, reader.ReadElementContentAs(m_FieldMap[pos].EntityField.FieldType, null));
                                }
                            }
                        }
                    }
                }
            }

            r.Read();
        }

        public void WriteXml(System.Xml.XmlWriter writer) {

            Array.ForEach(
                Fields,
                delegate(EntityDbField item) {
                    writer.WriteStartElement(XmlConvert.EncodeLocalName(item.DbName));
                    writer.WriteAttributeString("length", item.DbLength.ToString());
                    writer.WriteAttributeString("dbType", item.DbType.ToString());
                    object val = GetValueForDbField(item);
                    if (null != val) {
                        try {
                            writer.WriteValue(GetValueForDbField(item));
                        }
                        catch {
                            writer.WriteString(
                                typeof(XmlConvert).InvokeMember(
                                    "ToString",
                                    BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                                    null,
                                    null,
                                    new Object[] { GetValueForDbField(item) }).ToString());
                        }
                    }
                    writer.WriteEndElement();
                });
        }
    }
}
