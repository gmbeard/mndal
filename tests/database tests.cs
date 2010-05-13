using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using mnDAL.Database;
using System.Data.SqlClient;

namespace mnDAL.Tests
{
    [TestFixture]
    public class DatabaseTestSuite
    {
        private SqlConnection m_Connection;

        [TestFixtureSetUp]
        public void SuiteInit()
        {
            m_Connection = new SqlConnection(
                "Data Source=.\\SQLEXPRESS;" +
                "AttachDbFilename=\"db\\mnDAL.mdf\";" +
                "Integrated Security=True;" +
                "User Instance=True");
            m_Connection.Open();
        }

        [TestFixtureTearDown]
        public void SuiteUninit()
        {
            if (null != m_Connection)
            {
                m_Connection.Dispose();
            }
        }

        [Test]
        public void TestCreateDatabaseAdapter()
        {
            IDatabaseAdapter adapter = new DatabaseAdapter(m_Connection);
        }

        [Test]
        public void TestFetch()
        {
            using (IDatabaseAdapter adapter = new DatabaseAdapter(m_Connection))
            {
                CheeseEntity[] entities = adapter.FetchEntities(
                    new EntityFetcher<CheeseEntity>(CheeseEntity.CheeseEntityFields.CheeseID == 1));
            }
        }

        [Test]
        public void TestInsert()
        {
            CheeseEntity entity = new CheeseEntity();
            entity.Name = "Emental";

            using (IDatabaseAdapter adapter = new DatabaseAdapter(m_Connection))
            {
                EntityUpdater insert = new EntityUpdater(entity, UpdateAction.Insert);
                adapter.CommitEntity<CheeseEntity>(ref insert);
            }

            Assert.Greater(entity.CheeseID, 0);
        }

        [Test]
        public void TestDelete()
        {
            TestInsert();

            using (IDatabaseAdapter adapter = new DatabaseAdapter(m_Connection))
            {
                CheeseEntity[] cheeses = adapter.FetchEntities(
                    new EntityFetcher<CheeseEntity>(CheeseEntity.CheeseEntityFields.Name == "Emental"));

                Assert.Greater(cheeses.Length, 0, "No cheese to get rid of!");

                Array.ForEach(cheeses, delegate(CheeseEntity item)
                {
                    EntityUpdater delete = new EntityUpdater(item, UpdateAction.Delete);
                    adapter.CommitEntity<CheeseEntity>(ref delete);
                });

                cheeses = adapter.FetchEntities(
                    new EntityFetcher<CheeseEntity>(CheeseEntity.CheeseEntityFields.Name == "Emental"));
                Assert.AreEqual(cheeses.Length, 0, "We didn't get rid of every cheese!");
            }
        }
    }
}
