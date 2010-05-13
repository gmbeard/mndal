using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Threading;
using System.Reflection;

namespace mnDAL.Database 
{

    [Serializable]
    public class EntityFetcher<T> where T : EntityBase, new() {

        private readonly Expression      m_Expr;
        private readonly SortExpression  m_Sort;
        private readonly int             m_Top;

        public EntityFetcher() 
        {
            m_Top = 0;
        }

        public EntityFetcher(int max)
        {
            m_Top = max;
        }

        public EntityFetcher(SortExpression sort)
        {
            if(null == sort) 
            {
                throw new ArgumentNullException("SortExpression");
            }

            m_Sort = sort;
            m_Top = 0;
        }

        public EntityFetcher(SortExpression sort, int max)
        {
            if(null == sort) 
            {
                throw new ArgumentNullException("SortExpression");
            }

            m_Sort = sort;
            m_Top = max;
        }

        public EntityFetcher(Expression expr, int max)
        {
            if(null == expr)
            {
                throw new ArgumentNullException("Expression");
            }

            m_Expr = expr;
            m_Top = max;
        }

        public EntityFetcher(Expression expr) {
            if(null == expr) {
                throw new ArgumentNullException("Expression");
            }

            m_Expr = expr;
            m_Top = 0;
        }

        public EntityFetcher(Expression expr, SortExpression sort) {
            if(null == expr) {
                throw new ArgumentNullException("Expression");
            }

            if(null == sort) {
                throw new ArgumentNullException("SortExpression");
            }

            m_Expr= expr;
            m_Sort = sort;
            m_Top = 0;
        }

        public EntityFetcher(Expression expr, SortExpression sort, int max)
        {
            if(null == expr) {
                throw new ArgumentNullException("Expression");
            }

            if(null == sort) {
                throw new ArgumentNullException("SortExpression");
            }

            m_Expr= expr;
            m_Sort = sort;
            m_Top = max;
        }

        internal SqlCommand GetSelectCommand() {

            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;

            T ent = new T();
            EntityDbField[] fields = ent.Fields;

            StringBuilder qry = new StringBuilder();
            qry.Append("SELECT ");
            if(m_Top > 0)
            {
                qry.Append("TOP ");
                qry.Append(m_Top);
                qry.Append(" ");
            }

            for(int i = 0; i < fields.Length; ++i) {
                qry.Append(fields[i].DbName);

                if(i < (fields.Length - 1)) {
                    qry.Append(", ");
                }
            }

            qry.Append(" FROM ");
            qry.Append(ent.EntityDbName);

            if(null != m_Expr) {
                qry.Append(" WHERE ");
                qry.Append(m_Expr);

                foreach(Expression exp in m_Expr.Expressions)
                {
                    //  We must ignore adding parameters for expressions
                    //  comparing DBNull because the expression compiles
                    //  into 'IS [NOT] NULL'
                    if(exp.Value.GetType() != DBNull.Value.GetType())
                    {
                        cmd.Parameters.Add("@" + exp.ExpressionID, exp.DbField.DbType, exp.DbField.DbLength).Value = exp.Value;
                    }
                }
            }

            if(null != m_Sort) {
                qry.Append(m_Sort);
            }

            cmd.CommandText = qry.ToString();

            return cmd;
        }

        internal void ReadEntities(SqlDataReader reader, List<T> entities) {
            
            while(reader.Read()) {

                T ent = new T();
                EntityDbField[] fields = ent.Fields;

                for(int i = 0; i < fields.Length; ++i) {
                    if(reader.IsDBNull(i)) {
                        ent.SetValueForDbField(fields[i], null);
                    }
                    else {
                        ent.SetValueForDbField(fields[i], reader.GetValue(i));
                    }
                }
                entities.Add(ent);
            }
        }
    }

    public enum UpdateAction {
        Insert,
        Update,
        Delete
    }

    [Serializable]
    public class EntityUpdateException : ApplicationException {
        public EntityUpdateException(string msg) : base(msg) {}
        public EntityUpdateException(string msg, Exception innerException) : base(msg, innerException){}
    }

    [Serializable]
    public class EntityUpdater {

        private readonly EntityBase         m_Entity;
        private readonly UpdateAction       m_Action;
        private readonly EntityDbField[]    m_EntityFields;
        private readonly EntityDbField      m_AutoIncrementField;

        public EntityUpdater(EntityBase entity, UpdateAction action) {

            if(null == entity) {
                throw new ArgumentNullException("Entity");
            }

            if(!entity.Updatable) {
                throw new ArgumentException("The entity '" + m_Entity.EntityDbName + "' is not updatable");
            }

            m_Action = action;
            m_Entity = entity;

            m_EntityFields = m_Entity.Fields;

            if(null == m_EntityFields || m_EntityFields.Length == 0) {
                throw new ArgumentException("The entity '" + m_Entity.EntityDbName + "' doens''t contain any fields");
            }
            
            foreach(EntityDbField field in m_EntityFields) {
                if(field.IsAutoIncrement) {
                    m_AutoIncrementField = field;
                    break;
                }
            }
        }

        internal SqlCommand GetUpdateCommand() {

            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.Text;
            List<EntityDbField> updatedFields = new List<EntityDbField>();

            foreach(EntityDbField field in Fields) {
                if(Entity.GetDbFieldValueChanged(field)) {
                    updatedFields.Add(field);
                }
            }

            if(updatedFields.Count == 0)
            {
                //  Nothing to update!
                return null;
            }

            StringBuilder cmdText = new StringBuilder();
            cmdText.Append("UPDATE ");
            cmdText.Append(Entity.EntityDbName);
            cmdText.Append(" SET ");

            for(int i = 0; i < updatedFields.Count; ++i) {
                cmdText.Append(updatedFields[i].DbName);
                cmdText.Append(" = @");
                cmdText.Append(updatedFields[i].DbName);

                if( i < (updatedFields.Count - 1)) {
                    cmdText.Append(", ");
                }
                else {
                    cmdText.Append(" ");
                }
            }

            EntityDbField idFld = Entity.GetIdentifierDbField();

            cmdText.Append("WHERE ");
            cmdText.Append(idFld.DbName);
            cmdText.Append(" = @");
            cmdText.Append(idFld.DbName);
            cmd.CommandText = cmdText.ToString();

            foreach(EntityDbField field in updatedFields) {
                cmd.Parameters.Add("@" + field.DbName, field.DbType, field.DbLength).Value = Entity.GetValueForDbField(field);
            }

            cmd.Parameters.Add("@" + idFld.DbName, idFld.DbType, idFld.DbLength).Value = Entity.GetValueForDbField(idFld);

            return cmd;
        }

        internal SqlCommand GetInsertCommand() {

            SqlCommand      cmd = new SqlCommand();
            StringBuilder   cmdTxt = new StringBuilder();

            cmd.CommandType = CommandType.Text;

            // Build SQL
            cmdTxt.Append("INSERT INTO ");
            cmdTxt.Append(Entity.EntityDbName);
            cmdTxt.Append(" (");

            //  Build SQL field list
            for(int i =0; i < Fields.Length; ++i) {
                if(Fields[i].IsAutoIncrement) {
                    continue;
                }
                else {
                    if(i > 0) {
                        cmdTxt.Append(", ");
                    }
                    cmdTxt.Append(Fields[i].DbName);
                }
            }
            cmdTxt.Append(") ");

            cmdTxt.Append("VALUES (");

            // Build field->param list
            for(int i =0; i < Fields.Length; ++i) {
                if(Fields[i].IsAutoIncrement) {
                    continue;
                }
                else {
                    if(i > 0) {
                        cmdTxt.Append(", ");
                    }

                    cmdTxt.Append("@expr");
                    cmdTxt.Append(i.ToString());
                }
            }
            cmdTxt.Append(")");

            if(null != ((object)(AutoIncrementField))) {
                cmdTxt.Append(";SELECT @expr");
                cmdTxt.Append(Fields.Length.ToString());
                cmdTxt.Append(" = SCOPE_IDENTITY()");
            }

            cmd.CommandText = cmdTxt.ToString();

            // Add parameters
            for(int i = 0; i < Fields.Length; ++i) 
            {
                SqlParameter param = cmd.Parameters.Add("@expr" + i.ToString(), Fields[i].DbType, Fields[i].DbLength);
                //if(Fields[i].IsAutoIncrement) {
                //    param.Direction = ParameterDirection.Output;
                //}
                //else {
                param.Value = Entity.GetValueForDbField(Fields[i]);
                if(null == param.Value) {
                    param.Value = DBNull.Value;
                }
                //}
            }

            if (null != ((object)(AutoIncrementField)))
            {
                cmd.Parameters.Add("@expr" + Fields.Length.ToString(), AutoIncrementField.DbType, AutoIncrementField.DbLength).Direction = ParameterDirection.Output;
            }

            return cmd;
        }

        internal void RefreshEntityAutoIncrementValue(SqlCommand cmd) {

            if (Action == UpdateAction.Insert)
            {
                foreach (SqlParameter param in cmd.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output)
                    {
                        Entity.SetValueForDbField(AutoIncrementField, param.Value);
                        break;
                    }
                }
            }

            //if(null != ((object)(AutoIncrementField)) 
            //    && cmd.Parameters.Contains("@" + AutoIncrementField.DbName)
            //    && Action == UpdateAction.Insert) {

            //    Entity.SetValueForDbField(
            //        AutoIncrementField, 
            //        cmd.Parameters["@" + AutoIncrementField.DbName].Value);
            //}
        }

        internal SqlCommand GetDeleteCommand() 
        {
            EntityDbField   identifier = null;

            try
            {
                identifier = Entity.GetIdentifierDbField();
                if(null == (object)identifier)
                {
                    throw new ApplicationException("'" + Entity.EntityDbName + "' doesn't implement an unique field");
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Couldn't delete '" + Entity.EntityDbName + "'. Check inner exception", e);
            }

            SqlCommand      cmd = new SqlCommand();

            cmd.CommandType = CommandType.Text;

            StringBuilder   sql = new StringBuilder("DELETE FROM ");
            sql.Append(Entity.EntityDbName);
            sql.Append(" WHERE ");

            Expression exp = (Entity.GetIdentifierDbField() == Entity.GetValueForDbField(identifier));
            sql.Append(exp.ToString());

            cmd.CommandText = sql.ToString();

            foreach(Expression expr in exp.Expressions)
            {
                cmd.Parameters.Add("@" + expr.ExpressionID, expr.DbField.DbType, expr.DbField.DbLength).Value = expr.Value;
            }

            return cmd;
        }

        public EntityBase Entity {
            get{ return m_Entity; }
            //set{ m_Entity = value; }
        }

        internal UpdateAction Action {
            get{ return m_Action; }
        }

        protected EntityDbField[] Fields {
            get{ return m_EntityFields; }
        }

        protected EntityDbField AutoIncrementField {
            get{ return m_AutoIncrementField; }
        }
    }

    public interface IDatabaseAdapter : IDatabaseAdapterBase
    {
        T[] FetchEntities<T>(EntityFetcher<T> filter) where T : EntityBase, new();
        T FetchOneEntity<T>(EntityFetcher<T> fetcher) where T : EntityBase, new();
        void CommitEntity<T>(ref EntityUpdater adapter) where T : EntityBase, new();
        void CommitEntity<T>(ref EntityUpdater adapter, bool refetch) where T : EntityBase, new();
    }

    public interface IDatabaseAdapterBase : IDisposable
    {
        SqlConnection Connection {get; set;}
        object FetchEntities(object filter);
        void CommitEntity(ref object entity, UpdateAction action, Type entityType);
    }

    public class DatabaseAdapter : IDatabaseAdapter 
    {

        private SqlConnection   m_Connection;
        private bool            m_OwnsConnection;

        private int m_Disposed = 0;

        public DatabaseAdapter() {
            m_Connection = new SqlConnection(
                "Server=DEV-DBS\\SQL_SERVER_2005;Database=CRM;Trusted_Connection=true");
            m_OwnsConnection = true;
        }

        public DatabaseAdapter(SqlConnection connection) {
            if(null == connection) {
                throw new ArgumentNullException("Connection");
            }

            m_Connection = connection;
            m_OwnsConnection = false;
        }

        ~DatabaseAdapter() {
            Dispose(false);
        }

        public SqlConnection Connection
        {
            get{ return m_Connection; }
            set{
                if(null != m_Connection && m_OwnsConnection)
                {
                    m_Connection.Dispose();
                    m_Connection = null;
                }
                m_Connection = value;
                m_OwnsConnection = false;
            }
        }

        public T[] FetchEntities<T>(EntityFetcher<T> fetcher) where T : EntityBase, new() {
            
            List<T> entities = new List<T>();
            using(SqlCommand cmd = fetcher.GetSelectCommand()) {
                cmd.Connection = m_Connection;

                if(m_Connection.State == ConnectionState.Closed) {
                    m_Connection.Open();
                }

                using(SqlDataReader reader = cmd.ExecuteReader(m_OwnsConnection ? CommandBehavior.CloseConnection : CommandBehavior.Default)) {
                    fetcher.ReadEntities(reader, entities);
                    reader.Close();
                }
            }

            return entities.ToArray();
        }

        public T FetchOneEntity<T>(EntityFetcher<T> fetcher) where T : EntityBase, new() {
            
            T[] entities = FetchEntities<T>(fetcher);
            if(entities.Length > 0) {
                return entities[0];
            }
            else {
                return new T();
            }
        }

        public T FetchOneEntity<T>(int entityId) where T : EntityBase, new() {
            throw new NotImplementedException();
        }

        public void CommitEntity<T>(ref EntityUpdater adapter) where T : EntityBase, new() {
            CommitEntity<T>(ref adapter, false);
        }

        //  This is a bit clumsy. We only need
        //  this to be templated because of the refetch.
        public void CommitEntity<T>(ref EntityUpdater adapter, bool refetch) where T : EntityBase, new() {

            if(refetch) {
                throw new NotImplementedException("Refetching an entity is not yet supported");
            }

            SqlCommand cmd = null;
            switch(adapter.Action) {
                case UpdateAction.Delete:
                    cmd = adapter.GetDeleteCommand();
                    break;
                case UpdateAction.Insert:
                    cmd = adapter.GetInsertCommand();
                    break;
                case UpdateAction.Update:
                    cmd = adapter.GetUpdateCommand();
                    break;
            }

            //  TODO:
            //      If any of the GetXXXCommand methods
            //      can't build a suitable command then they return
            //      null.  Instead of ignoring the update, we should
            //      probably throw a suitable error message.
            if(null != cmd)
            {
                cmd.Connection = m_Connection;

                if(m_Connection.State == ConnectionState.Closed) {
                    m_Connection.Open();
                }

                try {
                    cmd.ExecuteNonQuery();
                    adapter.RefreshEntityAutoIncrementValue(cmd);

                    if(refetch) {
                        //  TODO:
                        //      EntityUpdater.Entity property should be readonly
                        //      to enforce rules within EntityUpdater.
                        //      Is a refetch even necessary if we've already
                        //      taken care of IDENTITY fields?
                        //adapter.Entity = 
                        //    FetchOneEntity(
                        //        new EntityFetcher<T>(adapter.Entity.GetIdentifierDbField() == adapter.Entity.GetValueForDbField(adapter.Entity.GetIdentifierDbField())));
                    }
                }
                finally {
                    if(m_OwnsConnection && m_Connection != null && m_Connection.State != ConnectionState.Closed) {
                        m_Connection.Close();
                    }
                }

                cmd.Dispose();
            }
        }

        protected void Dispose(bool disposing) {
            if(Interlocked.CompareExchange(ref m_Disposed, 1, 0) == 0) {
                if(null != m_Connection && m_OwnsConnection) {
                    m_Connection.Dispose();
                    m_Connection = null;
                }
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public object FetchEntities(object filter)
        {
            Type[] t = filter.GetType().GetGenericArguments();
            Type filterType = typeof(EntityFetcher<>);

            MethodInfo[] methods = GetType().GetMethods();
            foreach(MethodInfo mi in methods)
            {
                if(mi.Name == "FetchEntities" && mi.IsGenericMethod)
                {
                    MethodInfo genericMi = mi.MakeGenericMethod(new Type[] {t[0]});
                    return genericMi.Invoke(this, new object[] {filter});
                    //return mi.Invoke(this, new object[]{filter});
                }
            }

            throw new MissingMethodException("FetchEntities`1");
        }

        public void CommitEntity(ref object entity, UpdateAction action, Type entityType)
        {
            EntityUpdater update = new EntityUpdater((EntityBase)entity, action);
            MethodInfo[] methods = GetType().GetMethods();
            foreach(MethodInfo mi in methods)
            {
                if(mi.Name == "CommitEntity" && mi.IsGenericMethod && mi.GetParameters().Length == 1)
                {
                    MethodInfo genericMi = mi.MakeGenericMethod(new Type[] {entityType});
                    genericMi.Invoke(this, new object[] {update});
                    break;
                }
            }

            entity = update.Entity;
        }
    }
}
