using System;
using System.Collections.Generic;
using System.Text;

namespace mnDAL
{
    [Serializable]
    public class EntityType
    {
        private readonly string m_EntityName;
        private readonly string m_Database;

        public EntityType(string entityName)
        {
            if (String.IsNullOrEmpty(entityName))
            {
                throw new ArgumentException("entityName can't be null or empty", "entityName");
            }

            m_EntityName = entityName;
            m_Database = String.Empty;
        }

        public EntityType(string entityName, string database)
        {
            if (String.IsNullOrEmpty(entityName))
            {
                throw new ArgumentException("entityName can't be null or empty", "entityName");
            }

            if (String.IsNullOrEmpty(database))
            {
                throw new ArgumentException("database can't be null or empty", "database");
            }

            m_EntityName = entityName;
            m_Database = database;
        }

        public string EntityName
        {
            get { return m_EntityName; }
        }

        public string Database
        {
            get { return m_Database; }
        }

        public override string ToString()
        {
            if (!String.IsNullOrEmpty(Database))
            {
                return String.Concat(Database, ".", EntityName);
            }
            else
            {
                return EntityName;
            }
        }

        public static implicit operator string(EntityType rhs)
        {
            return rhs.ToString();
        }
    }
}
