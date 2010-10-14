using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace mnDAL {
    public class EntityDbFieldIndex : IDisposable {

        private readonly EntityDbField m_IndexedColumn;
        private readonly List<Object> m_Keys = new List<Object>();
        private readonly List<List<int>> m_Index = new List<List<int>>();

        public EntityDbFieldIndex(EntityDbField indexedColumn) {
            if (null == (Object)indexedColumn) {
                throw new ArgumentNullException("indexedColumn");
            }

            m_IndexedColumn = indexedColumn;
        }

        public void Add(int index, EntityBase item) {
            int pos = m_Keys.BinarySearch(item.GetValueForDbField(m_IndexedColumn));
            if (pos < 0) {
                pos = ~pos;
                m_Keys.Insert(pos, item.GetValueForDbField(m_IndexedColumn));
                m_Index.Insert(pos, new List<int>());
            }

            m_Index[pos].Add(index);
        }

        public void Remove(int index) {

            for (int i = 0; i < m_Index.Count; ++i) {
                if (m_Index[i] == null) {
                    continue;
                }

                for (int j = 0; j < m_Index[i].Count; ++j) {
                    if (m_Index[i][j] == index) {
                        m_Index[i].RemoveAt(j);
                    }
                }

                if (m_Index[i].Count == 0) {
                    m_Keys.RemoveAt(i);
                }
            }
        }

        public int[] Seek(Object value) {

            int[] positions = null;
            int pos = m_Keys.BinarySearch(value);
            if (pos < 0) {
                positions = new int[] { };
            }
            else {
                positions = m_Index[pos].ToArray();
            }

            return positions;
        }

        #region IDisposable Members

        public void Dispose() {
            m_Index.Clear();
            m_Keys.Clear();

            GC.SuppressFinalize(this);
        }

        #endregion

        public EntityDbField IndexedColumn {
            get { return m_IndexedColumn; }
        }
    }

    public class EntityCollection : IEnumerable {

        private List<EntityBase> m_Items = new List<EntityBase>();
        private EntityDbFieldIndex m_Index;
        private bool m_IsIndexed;

        public EntityCollection() {
            m_IsIndexed = false;
        }

        public EntityCollection(EntityDbField indexColumn) {
            m_Index = new EntityDbFieldIndex(indexColumn);
            m_IsIndexed = true;
        }

        public void AddRange(EntityBase[] items) {
            Array.ForEach(
                items,
                delegate(EntityBase item) {
                    Add(item);
                }
            );
        }

        public void Add(EntityBase item) {
            int latestIndex = m_Items.Count;
            m_Index.Add(latestIndex, item);
            m_Items.Add(item);
        }

        public void RemoveAt(int index) {
            if (m_IsIndexed) {
                m_Index.Remove(index);
            }

            m_Items.RemoveAt(index);
        }

        public EntityBase[] Find(Object value) {
            if (!m_IsIndexed) {
                return m_Items.FindAll(
                    new Predicate<EntityBase>(
                        delegate(EntityBase item) {

                            for (int i = 0; i < item.Fields.Length; ++i) {
                                if (Object.Equals(item.GetValueForDbField(item.Fields[i]), value)) {
                                    return true;
                                }
                            }

                            return false;
                        }
                    )
                ).ToArray();
            }
            else {
                int[] positions = m_Index.Seek(value);
                EntityBase[] results = new EntityBase[positions.Length];

                int i = 0;
                Array.ForEach(
                    positions,
                    delegate(int item) {
                        results[i++] = m_Items[item];
                    }
                );

                return results;
            }
        }

        public void Clear() {
            m_Items.Clear();
            if (m_IsIndexed) {
                m_Index.Dispose();
                m_Index = new EntityDbFieldIndex(m_Index.IndexedColumn);
            }
        }

        public int Count {
            get { return m_Items.Count; }
        }

        public EntityBase this[int index] {
            get { return m_Items[index]; }
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator() {
            return new Enumerator(this);
        }

        public class Enumerator : IEnumerator {

            private EntityCollection m_Parent;
            private int m_Current;

            internal Enumerator(EntityCollection parent) {
                m_Parent = parent;
                m_Current = -1;
            }

            #region IEnumerator<EntityBase> Members

            public EntityBase Current {
                get {
                    if (m_Current < 0) {
                        return null;
                    }
                    else {
                        return m_Parent[m_Current];
                    }
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose() { }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current {
                get { return this.Current; }
            }

            public bool MoveNext() {
                return ++m_Current < m_Parent.Count;
            }

            public void Reset() {
                m_Current = -1;
            }

            #endregion
        }

        #endregion
    }

    public class EntityCollection<T> : EntityCollection, IEnumerable<T> where T : EntityBase {

        public EntityCollection() { }

        public EntityCollection(EntityDbField indexColumn) : base(indexColumn) { }

        public void AddRange(T[] items) {
            Array.ForEach(
                items,
                delegate(T item) {
                    Add(item);
                }
            );
        }

        public void Add(T item) {
            base.Add(item);
        }

        public new T[] Find(Object value) {
            EntityBase[] results = base.Find(value);
            T[] castedResults = new T[results.Length];
            Array.Copy(results, castedResults, results.Length);
            return castedResults;
        }

        public new T this[int index] {
            get { return (T)base[index]; }
        }

        #region IEnumerable<T> Members

        public class TypedEnumerator : EntityCollection.Enumerator, IEnumerator<T> {

            internal TypedEnumerator(EntityCollection parent) : base(parent) { }

            public new T Current {
                get {
                    return (T)base.Current;
                }
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new TypedEnumerator(this);
        }

        #endregion
    }
}
