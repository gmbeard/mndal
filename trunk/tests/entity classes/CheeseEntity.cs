using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace mnDAL.Tests
{
    public class CheeseEntity : EntityBase
    {
        public static class CheeseEntityFields
        {
            public static EntityDbField CheeseID = new EntityDbField("CheeseID", SqlDbType.Int, true);
            public static EntityDbField Name = new EntityDbField("[Name]", SqlDbType.NVarChar, 50);
        }

        private int m_CheeseID;
        private string m_Name;

        public CheeseEntity()
            : base("dbo.Cheese")
        {
            Init();
        }

        private void Init()
        {
            AddFieldMapping(CheeseEntityFields.CheeseID, "m_CheeseID");
            AddFieldMapping(CheeseEntityFields.Name, "m_Name");
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
                SetFieldModified(CheeseEntityFields.Name, GetDbFieldValueChanged(CheeseEntityFields.Name) | value != m_Name);
                m_Name = value;
            }
        }
    }
}
