using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace mnDAL.Tests
{
    [Serializable]
    public class CountryEntity : EntityBase
    {
        public static EntityType EntityType = new EntityType("dbo.Country");

        public static class CountryEntityFields
        {
            public static EntityDbField CountryID = new EntityDbField("CountryID", SqlDbType.Int, true, EntityType);
            public static EntityDbField Name = new EntityDbField("[Name]", SqlDbType.NVarChar, 50, EntityType);
        }

        private int m_CountryID;
        private string m_Name;

        public CountryEntity()
            : base(EntityType.EntityName)
        {
            Init();
        }

        private void Init()
        {
            AddFieldMapping(CountryEntityFields.CountryID, "m_CountryID");
            AddFieldMapping(CountryEntityFields.Name, "m_Name");
        }

        public override EntityDbField GetIdentifierDbField()
        {
            return CountryEntityFields.CountryID;
        }

        public int CountryID
        {
            get { return m_CountryID; }
            protected set { m_CountryID = value; }
        }

        public string Name
        {
            get { return m_Name; }
            set 
            {
                SetFieldModified(CountryEntityFields.Name, GetDbFieldValueChanged(CountryEntityFields.Name) || value != m_Name);
                m_Name = value; 
            }
        }
    }
}
