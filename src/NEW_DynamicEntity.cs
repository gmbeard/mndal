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
using System.Runtime.Serialization;

namespace mnDAL {

    [Serializable]
    public sealed class DynamicEntity : EntityBase, ISerializable {

        private Object[] m_FieldValues;
        List<EntityDbField> m_Fields;

        private DynamicEntity(SerializationInfo info, StreamingContext ctx)
            : base("[__DynamicEntity__" + new Guid().ToString() + "]") {

            m_FieldValues = (Object[])info.GetValue("m_FieldValues", typeof(Object[]));
            m_Fields = (List<EntityDbField>)info.GetValue("m_Fields", typeof(List<EntityDbField>));

            m_Fields.ForEach(
                delegate(EntityDbField field) {
                    AddFieldMapping(field, typeof(Object), FieldMappingAction);
                }
            );
        }

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
                    AddFieldMapping(field, typeof(Object), FieldMappingAction);
                }
            }

            m_FieldValues = new Object[m_Fields.Count];
        }

        private void Init() {

        }

        private void FieldMappingAction(FieldMappingActionArgs args) {
            int pos = m_Fields.BinarySearch(args.DbField);
            if(pos >= 0) {
                switch(args.CallReason) {
                    case FieldMappingActionCallReason.Get:
                        args.Value = m_FieldValues[pos];
                        break;
                    case FieldMappingActionCallReason.Set:
                        m_FieldValues[pos] = args.Value;
                        break;
                }
            }
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
        }

        public override bool Updatable {
            get {
                return false;
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("m_FieldValues", m_FieldValues);
            info.AddValue("m_Fields", m_Fields);
        }
    }
}
