using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using mnDAL.Database;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

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
                @"Data Source=.\SQLEXPRESS;AttachDbFilename=""C:\Documents and Settings\Greg\My Documents\Visual Studio 2008\Projects\mnDAL\mnDAL\trunk\db\mnDAL.mdf"";Integrated Security=True;User Instance=True");
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

        [Test]
        public void TestJoin()
        {
            using (IDatabaseAdapter adapter = new DatabaseAdapter(m_Connection))
            {
                CountryEntity country = new CountryEntity();
                country.Name = "Switzerland";

                EntityUpdater insert = new EntityUpdater(country, UpdateAction.Insert);
                adapter.CommitEntity<CountryEntity>(ref insert);

                CheeseEntity emental = new CheeseEntity();
                emental.Name = "Emmental";
                emental.CountryID = country.CountryID;

                insert = new EntityUpdater(emental, UpdateAction.Insert);
                adapter.CommitEntity<CheeseEntity>(ref insert);
                adapter.CommitEntity<CheeseEntity>(ref insert);
                adapter.CommitEntity<CheeseEntity>(ref insert);

                CheeseEntity[] swissCheese = adapter.FetchEntities<CheeseEntity>(
                    new EntityFetcher<CheeseEntity>(CountryEntity.CountryEntityFields.CountryID == country.CountryID)
                    .AddJoinPath(CheeseEntity.CheeseEntityFields.CountryID.Join(JoinType.Inner, CountryEntity.CountryEntityFields.CountryID)));

                Assert.GreaterOrEqual(swissCheese.Length, 3);
            }
        }
    }
}
