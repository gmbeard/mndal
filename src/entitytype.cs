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
    [Serializable]
    public class EntityType {
        private readonly string m_EntityName;
        private readonly string m_Database;

        public EntityType(string entityName) {
            if (String.IsNullOrEmpty(entityName)) {
                throw new ArgumentException("entityName can't be null or empty", "entityName");
            }

            m_EntityName = entityName;
            m_Database = String.Empty;
        }

        public EntityType(string entityName, string database) {
            if (String.IsNullOrEmpty(entityName)) {
                throw new ArgumentException("entityName can't be null or empty", "entityName");
            }

            if (String.IsNullOrEmpty(database)) {
                throw new ArgumentException("database can't be null or empty", "database");
            }

            m_EntityName = entityName;
            m_Database = database;
        }

        public string EntityName {
            get { return m_EntityName; }
        }

        public string Database {
            get { return m_Database; }
        }

        public override string ToString() {
            if (!String.IsNullOrEmpty(Database)) {
                return String.Concat(Database, ".", EntityName);
            }
            else {
                return EntityName;
            }
        }

        public static implicit operator string(EntityType rhs) {
            return rhs.ToString();
        }
    }
}
