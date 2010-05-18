using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace mnDAL.Tests
{
    public class CheeseEntity : EntityBase
    {
        public static EntityType EntityType = new EntityType("dbo.Cheese");

        public static class CheeseEntityFields
        {
            public static EntityDbField CheeseID = new EntityDbField("CheeseID", SqlDbType.Int, true, EntityType);
            public static EntityDbField Name = new EntityDbField("[Name]", SqlDbType.NVarChar, 50, EntityType);
            public static EntityDbField CountryID = new EntityDbField("CountryID", SqlDbType.Int, EntityType);
        }

        private int m_CheeseID;
        private string m_Name;
        private int? m_CountryID;

        public CheeseEntity()
            : base(EntityType.EntityName)
        {
            Init();
        }

        private void Init()
        {
            AddFieldMapping(CheeseEntityFields.CheeseID, "m_CheeseID");
            AddFieldMapping(CheeseEntityFields.Name, "m_Name");
            AddFieldMapping(CheeseEntityFields.CountryID, "m_CountryID");
        }

        public override EntityDbField GetIdentifierDbField()
        {
            return CheeseEntityFields.CheeseID;
        }

        public int CheeseID
        {
            get { return m_CheeseID; }
            set { m_CheeseID = value; }
        }

        public string Name
        {
            get { return m_Name; }
            set
            {
                SetFieldModified(CheeseEntityFields.Name, GetDbFieldValueChanged(CheeseEntityFields.Name) || value != m_Name);
                m_Name = value;
            }
        }

        public int? CountryID
        {
            get { return m_CountryID; }
            set
            {
                SetFieldModified(CheeseEntityFields.CountryID, GetDbFieldValueChanged(CheeseEntityFields.CountryID) || value != m_CountryID);
                m_CountryID = value;
            }
        }
    }
}
