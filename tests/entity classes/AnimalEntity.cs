using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace mnDAL.Tests
{
    public class AnimalEntity : EntityBase
    {
        public static EntityType EntityType = new EntityType("dbo.Animal");

        public static class AnimalEntityFields
        {
            public static EntityDbField AnimalID = new EntityDbField("AnimalID", SqlDbType.Int, true, EntityType);
            public static EntityDbField Name = new EntityDbField("[Name]", SqlDbType.NVarChar, 50, EntityType);
        }

        private int m_AnimalID;
        private string m_Name;

        public AnimalEntity()
            : base(EntityType)
        {
            Init();
        }

        private void Init()
        {
            AddFieldMapping(AnimalEntityFields.AnimalID, "m_AnimalID");
            AddFieldMapping(AnimalEntityFields.Name, "m_Name");
        }

        public override EntityDbField GetIdentifierDbField()
        {
            return AnimalEntityFields.AnimalID;
        }

        public int AnimalID
        {
            get { return m_AnimalID; }
            protected set { m_AnimalID = value; }
        }

        public string Name
        {
            get { return m_Name; }
            set
            {
                SetFieldModified(AnimalEntityFields.Name, GetDbFieldValueChanged(AnimalEntityFields.Name) || value != m_Name);
                m_Name = value;
            }
        }
    }
}
