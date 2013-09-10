using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{

    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary, ICollection, IEnumerable
    {
        private static readonly bool s_isValueWriteAtomic = ConcurrentDictionary<TKey, TValue>.IsValueWriteAtomic();
        private volatile ConcurrentDictionary<TKey, TValue>.Tables m_tables;
        private readonly IEqualityComparer<TKey> m_comparer;
        private readonly bool m_growLockArray;
        private int m_budget;
        private KeyValuePair<TKey, TValue>[] m_serializationArray;
        private int m_serializationConcurrencyLevel;
        private int m_serializationCapacity;
        private const int DEFAULT_CONCURRENCY_MULTIPLIER = 4;
        private const int DEFAULT_CAPACITY = 31;
        private const int MAX_LOCK_NUMBER = 1024;


        public TValue this[TKey key]
        {

            get
            {
                TValue obj;
                if (!this.TryGetValue(key, out obj))
                    throw new KeyNotFoundException();
                else
                    return obj;
            }

            set
            {
                if ((object)key == null)
                    throw new ArgumentNullException("key");
                TValue resultingValue;
                this.TryAddInternal(key, value, true, true, out resultingValue);
            }
        }

        //todo:correct this
        public object this[object objkey]
        {
            get
            {
                if (!(objkey is TKey))
                    throw new InvalidCastException();
                TKey key = (TKey)objkey;
                TValue obj;
                if (!this.TryGetValue(key, out obj))
                    throw new KeyNotFoundException();
                else
                    return obj;
            }
            set
            {
                if (!(objkey is TKey))
                    throw new InvalidCastException();
                if (objkey == null)
                    throw new ArgumentNullException("key");
                if (!(value is TValue))
                    throw new InvalidCastException();

                TKey key = (TKey)objkey;
                TValue resultingValue;
                this.TryAddInternal(key, (TValue)value, true, true, out resultingValue);
            }
        }
        public int Count
        {

            get
            {
                int num = 0;
                int locksAcquired = 0;
                try
                {
                    this.AcquireAllLocks(ref locksAcquired);
                    for (int index = 0; index < this.m_tables.m_countPerLock.Length; ++index)
                        num += this.m_tables.m_countPerLock[index];
                }
                finally
                {
                    this.ReleaseLocks(0, locksAcquired);
                }
                return num;
            }
        }


        public bool IsEmpty
        {

            get
            {
                int locksAcquired = 0;
                try
                {
                    this.AcquireAllLocks(ref locksAcquired);
                    for (int index = 0; index < this.m_tables.m_countPerLock.Length; ++index)
                    {
                        if (this.m_tables.m_countPerLock[index] != 0)
                            return false;
                    }
                }
                finally
                {
                    this.ReleaseLocks(0, locksAcquired);
                }
                return true;
            }
        }


        public ICollection<TKey> Keys
        {

            get
            {
                return (ICollection<TKey>)this.GetKeys();
            }
        }


        public ICollection<TValue> Values
        {

            get
            {
                return (ICollection<TValue>)this.GetValues();
            }
        }


        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {

            get
            {
                return false;
            }
        }


        bool IDictionary.IsFixedSize
        {

            get
            {
                return false;
            }
        }


        bool IDictionary.IsReadOnly
        {

            get
            {
                return false;
            }
        }


        ICollection IDictionary.Keys
        {

            get
            {
                return (ICollection)this.GetKeys();
            }
        }


        ICollection IDictionary.Values
        {

            get
            {
                return (ICollection)this.GetValues();
            }
        }


        bool ICollection.IsSynchronized
        {

            get
            {
                return false;
            }
        }


        object ICollection.SyncRoot
        {

            get
            {
                throw new NotSupportedException("ConcurrentCollection_SyncRoot_NotSupported");
            }
        }

        private static int DefaultConcurrencyLevel
        {
            get
            {
                return 4;
            }
        }

        static ConcurrentDictionary()
        {
        }


        public ConcurrentDictionary()
            : this(ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel, 31, true, (IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
        {
        }


        public ConcurrentDictionary(int concurrencyLevel, int capacity)
            : this(concurrencyLevel, capacity, false, (IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
        {
        }


        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(collection, (IEqualityComparer<TKey>)EqualityComparer<TKey>.Default)
        {
        }


        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
            : this(ConcurrentDictionary<TKey, TValue>.DefaultConcurrencyLevel, 31, true, comparer)
        {
        }


        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(comparer)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            this.InitializeFromCollection(collection);
        }


        public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, 31, false, comparer)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            this.InitializeFromCollection(collection);
        }



        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, capacity, false, comparer)
        {
        }

        internal ConcurrentDictionary(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<TKey> comparer)
        {
            if (concurrencyLevel < 1)
                throw new ArgumentOutOfRangeException("concurrencyLevel", this.GetResource("ConcurrentDictionary_ConcurrencyLevelMustBePositive"));
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", this.GetResource("ConcurrentDictionary_CapacityMustNotBeNegative"));
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            if (capacity < concurrencyLevel)
                capacity = concurrencyLevel;
            object[] locks = new object[concurrencyLevel];
            for (int index = 0; index < locks.Length; ++index)
                locks[index] = new object();
            int[] countPerLock = new int[locks.Length];
            ConcurrentDictionary<TKey, TValue>.Node[] buckets = new ConcurrentDictionary<TKey, TValue>.Node[capacity];
            this.m_tables = new ConcurrentDictionary<TKey, TValue>.Tables(buckets, locks, countPerLock);
            this.m_comparer = comparer;
            this.m_growLockArray = growLockArray;
            this.m_budget = buckets.Length / locks.Length;
        }

        private static bool IsValueWriteAtomic()
        {
            Type type = typeof(TValue);
            bool flag = type.IsClass || type == typeof(bool) || (type == typeof(char) || type == typeof(byte)) || (type == typeof(sbyte) || type == typeof(short) || (type == typeof(ushort) || type == typeof(int))) || type == typeof(uint) || type == typeof(float);
            if (!flag && IntPtr.Size == 8)
                flag = ((flag ? 1 : 0) | (type == typeof(double) ? 1 : (type == typeof(long) ? 1 : 0))) != 0;
            return flag;
        }

        private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach (KeyValuePair<TKey, TValue> keyValuePair in collection)
            {
                if ((object)keyValuePair.Key == null)
                    throw new ArgumentNullException("key");
                TValue resultingValue;
                if (!this.TryAddInternal(keyValuePair.Key, keyValuePair.Value, false, false, out resultingValue))
                    throw new ArgumentException(this.GetResource("ConcurrentDictionary_SourceContainsDuplicateKeys"));
            }
            if (this.m_budget != 0)
                return;
            this.m_budget = this.m_tables.m_buckets.Length / this.m_tables.m_locks.Length;
        }


        public bool TryAdd(TKey key, TValue value)
        {
            if ((object)key == null)
            {
                throw new ArgumentNullException("key");
            }
            else
            {
                TValue resultingValue;
                return this.TryAddInternal(key, value, false, true, out resultingValue);
            }
        }


        public bool ContainsKey(TKey key)
        {
            if ((object)key == null)
            {
                throw new ArgumentNullException("key");
            }
            else
            {
                TValue obj;
                return this.TryGetValue(key, out obj);
            }
        }


        public bool TryRemove(TKey key, out TValue value)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            else
                return this.TryRemoveInternal(key, out value, false, default(TValue));
        }

        private bool TryRemoveInternal(TKey key, out TValue value, bool matchValue, TValue oldValue)
        {
        label_0:
            ConcurrentDictionary<TKey, TValue>.Tables tables = this.m_tables;
            int bucketNo;
            int lockNo;
            this.GetBucketAndLockNo(this.m_comparer.GetHashCode(key), out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
            lock (tables.m_locks[lockNo])
            {
                if (tables == this.m_tables)
                {
                    ConcurrentDictionary<TKey, TValue>.Node local_3 = (ConcurrentDictionary<TKey, TValue>.Node)null;
                    for (ConcurrentDictionary<TKey, TValue>.Node local_4 = tables.m_buckets[bucketNo]; local_4 != null; local_4 = local_4.m_next)
                    {
                        if (this.m_comparer.Equals(local_4.m_key, key))
                        {
                            if (matchValue && !EqualityComparer<TValue>.Default.Equals(oldValue, local_4.m_value))
                            {
                                value = default(TValue);
                                return false;
                            }
                            else
                            {
                                if (local_3 == null)
                                    Volatile.Write<ConcurrentDictionary<TKey, TValue>.Node>(ref tables.m_buckets[bucketNo], local_4.m_next);
                                else
                                    local_3.m_next = local_4.m_next;
                                value = local_4.m_value;
                                --tables.m_countPerLock[lockNo];
                                return true;
                            }
                        }
                        else
                            local_3 = local_4;
                    }
                }
                else
                    goto label_0;
            }
            value = default(TValue);
            return false;
        }


        public bool TryGetValue(TKey key, out TValue value)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            ConcurrentDictionary<TKey, TValue>.Tables tables = this.m_tables;
            int bucketNo;
            int lockNo;
            this.GetBucketAndLockNo(this.m_comparer.GetHashCode(key), out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
            for (ConcurrentDictionary<TKey, TValue>.Node node = Volatile.Read<ConcurrentDictionary<TKey, TValue>.Node>(ref tables.m_buckets[bucketNo]); node != null; node = node.m_next)
            {
                if (this.m_comparer.Equals(node.m_key, key))
                {
                    value = node.m_value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }


        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            int hashCode = this.m_comparer.GetHashCode(key);
            IEqualityComparer<TValue> equalityComparer = (IEqualityComparer<TValue>)EqualityComparer<TValue>.Default;
        label_3:
            ConcurrentDictionary<TKey, TValue>.Tables tables = this.m_tables;
            int bucketNo;
            int lockNo;
            this.GetBucketAndLockNo(hashCode, out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
            lock (tables.m_locks[lockNo])
            {
                if (tables == this.m_tables)
                {
                    ConcurrentDictionary<TKey, TValue>.Node local_5 = (ConcurrentDictionary<TKey, TValue>.Node)null;
                    for (ConcurrentDictionary<TKey, TValue>.Node local_6 = tables.m_buckets[bucketNo]; local_6 != null; local_6 = local_6.m_next)
                    {
                        if (this.m_comparer.Equals(local_6.m_key, key))
                        {
                            if (!equalityComparer.Equals(local_6.m_value, comparisonValue))
                                return false;
                            if (ConcurrentDictionary<TKey, TValue>.s_isValueWriteAtomic)
                            {
                                local_6.m_value = newValue;
                            }
                            else
                            {
                                ConcurrentDictionary<TKey, TValue>.Node local_7 = new ConcurrentDictionary<TKey, TValue>.Node(local_6.m_key, newValue, hashCode, local_6.m_next);
                                if (local_5 == null)
                                    tables.m_buckets[bucketNo] = local_7;
                                else
                                    local_5.m_next = local_7;
                            }
                            return true;
                        }
                        else
                            local_5 = local_6;
                    }
                    return false;
                }
                else
                    goto label_3;
            }
        }


        public void Clear()
        {
            int locksAcquired = 0;
            try
            {
                this.AcquireAllLocks(ref locksAcquired);
                ConcurrentDictionary<TKey, TValue>.Tables tables = new ConcurrentDictionary<TKey, TValue>.Tables(new ConcurrentDictionary<TKey, TValue>.Node[31], this.m_tables.m_locks, new int[this.m_tables.m_countPerLock.Length]);
                this.m_tables = tables;
                this.m_budget = Math.Max(1, tables.m_buckets.Length / tables.m_locks.Length);
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }


        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", this.GetResource("ConcurrentDictionary_IndexIsNegative"));
            int locksAcquired = 0;
            try
            {
                this.AcquireAllLocks(ref locksAcquired);
                int num = 0;
                for (int index1 = 0; index1 < this.m_tables.m_locks.Length && num >= 0; ++index1)
                    num += this.m_tables.m_countPerLock[index1];
                if (array.Length - num < index || num < 0)
                    throw new ArgumentException(this.GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
                this.CopyToPairs(array, index);
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }


        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            int locksAcquired = 0;
            try
            {
                this.AcquireAllLocks(ref locksAcquired);
                int length = 0;
                int index = 0;
                while (index < this.m_tables.m_locks.Length)
                {
                    checked { length += this.m_tables.m_countPerLock[index]; }
                    checked { ++index; }
                }
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[length];
                this.CopyToPairs(array, 0);
                return array;
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }

        private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
        {
            foreach (ConcurrentDictionary<TKey, TValue>.Node node1 in this.m_tables.m_buckets)
            {
                for (ConcurrentDictionary<TKey, TValue>.Node node2 = node1; node2 != null; node2 = node2.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(node2.m_key, node2.m_value);
                    ++index;
                }
            }
        }

        private void CopyToEntries(DictionaryEntry[] array, int index)
        {
            foreach (ConcurrentDictionary<TKey, TValue>.Node node1 in this.m_tables.m_buckets)
            {
                for (ConcurrentDictionary<TKey, TValue>.Node node2 = node1; node2 != null; node2 = node2.m_next)
                {
                    array[index] = new DictionaryEntry((object)node2.m_key, (object)node2.m_value);
                    ++index;
                }
            }
        }

        private void CopyToObjects(object[] array, int index)
        {
            foreach (ConcurrentDictionary<TKey, TValue>.Node node1 in this.m_tables.m_buckets)
            {
                for (ConcurrentDictionary<TKey, TValue>.Node node2 = node1; node2 != null; node2 = node2.m_next)
                {
                    array[index] = (object)new KeyValuePair<TKey, TValue>(node2.m_key, node2.m_value);
                    ++index;
                }
            }
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (ConcurrentDictionary<TKey, TValue>.Node location in this.m_tables.m_buckets)
            {
                ConcurrentDictionary<TKey, TValue>.Node location1 = location;
                for (ConcurrentDictionary<TKey, TValue>.Node current = Volatile.Read<ConcurrentDictionary<TKey, TValue>.Node>(ref location1); current != null; current = current.m_next)
                    yield return new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
            }
        }

        private bool TryAddInternal(TKey key, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
        {
            int hashCode = this.m_comparer.GetHashCode(key);
        label_1:
            ConcurrentDictionary<TKey, TValue>.Tables tables = this.m_tables;
            int bucketNo;
            int lockNo;
            this.GetBucketAndLockNo(hashCode, out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
            bool flag = false;
            bool lockTaken = false;
            try
            {
                if (acquireLock)
                    Monitor.Enter(tables.m_locks[lockNo], ref lockTaken);
                if (tables == this.m_tables)
                {
                    ConcurrentDictionary<TKey, TValue>.Node node1 = (ConcurrentDictionary<TKey, TValue>.Node)null;
                    for (ConcurrentDictionary<TKey, TValue>.Node node2 = tables.m_buckets[bucketNo]; node2 != null; node2 = node2.m_next)
                    {
                        if (this.m_comparer.Equals(node2.m_key, key))
                        {
                            if (updateIfExists)
                            {
                                if (ConcurrentDictionary<TKey, TValue>.s_isValueWriteAtomic)
                                {
                                    node2.m_value = value;
                                }
                                else
                                {
                                    ConcurrentDictionary<TKey, TValue>.Node node3 = new ConcurrentDictionary<TKey, TValue>.Node(node2.m_key, value, hashCode, node2.m_next);
                                    if (node1 == null)
                                        tables.m_buckets[bucketNo] = node3;
                                    else
                                        node1.m_next = node3;
                                }
                                resultingValue = value;
                            }
                            else
                                resultingValue = node2.m_value;
                            return false;
                        }
                        else
                            node1 = node2;
                    }
                    Volatile.Write<ConcurrentDictionary<TKey, TValue>.Node>(ref tables.m_buckets[bucketNo], new ConcurrentDictionary<TKey, TValue>.Node(key, value, hashCode, tables.m_buckets[bucketNo]));
                    checked { ++tables.m_countPerLock[lockNo]; }
                    if (tables.m_countPerLock[lockNo] > this.m_budget)
                        flag = true;
                }
                else
                    goto label_1;
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(tables.m_locks[lockNo]);
            }
            if (flag)
                this.GrowTable(tables);
            resultingValue = value;
            return true;
        }


        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");
            TValue resultingValue;
            if (this.TryGetValue(key, out resultingValue))
                return resultingValue;
            this.TryAddInternal(key, valueFactory(key), false, true, out resultingValue);
            return resultingValue;
        }


        public TValue GetOrAdd(TKey key, TValue value)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            TValue resultingValue;
            this.TryAddInternal(key, value, false, true, out resultingValue);
            return resultingValue;
        }


        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            if (addValueFactory == null)
                throw new ArgumentNullException("addValueFactory");
            if (updateValueFactory == null)
                throw new ArgumentNullException("updateValueFactory");
            TValue comparisonValue;
            TValue newValue;
            do
            {
                while (!this.TryGetValue(key, out comparisonValue))
                {
                    TValue obj = addValueFactory(key);
                    TValue resultingValue;
                    if (this.TryAddInternal(key, obj, false, true, out resultingValue))
                        return resultingValue;
                }
                newValue = updateValueFactory(key, comparisonValue);
            }
            while (!this.TryUpdate(key, newValue, comparisonValue));
            return newValue;
        }


        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if ((object)key == null)
                throw new ArgumentNullException("key");
            if (updateValueFactory == null)
                throw new ArgumentNullException("updateValueFactory");
            TValue comparisonValue;
            TValue newValue;
            do
            {
                while (!this.TryGetValue(key, out comparisonValue))
                {
                    TValue resultingValue;
                    if (this.TryAddInternal(key, addValue, false, true, out resultingValue))
                        return resultingValue;
                }
                newValue = updateValueFactory(key, comparisonValue);
            }
            while (!this.TryUpdate(key, newValue, comparisonValue));
            return newValue;
        }


        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!this.TryAdd(key, value))
                throw new ArgumentException(this.GetResource("ConcurrentDictionary_KeyAlreadyExisted"));
        }


        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            TValue obj;
            return this.TryRemove(key, out obj);
        }


        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (!this.TryAdd(keyValuePair.Key, keyValuePair.Value))
                throw new ArgumentException(this.GetResource("ConcurrentDictionary_KeyAlreadyExisted"));
        }


        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TValue x;
            if (!this.TryGetValue(keyValuePair.Key, out x))
                return false;
            else
                return EqualityComparer<TValue>.Default.Equals(x, keyValuePair.Value);
        }


        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if ((object)keyValuePair.Key == null)
            {
                throw new ArgumentNullException(this.GetResource("ConcurrentDictionary_ItemKeyIsNull"));
            }
            else
            {
                TValue obj;
                return this.TryRemoveInternal(keyValuePair.Key, out obj, true, keyValuePair.Value);
            }
        }



        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }


        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (!(key is TKey))
                throw new ArgumentException(this.GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
            TValue obj;
            try
            {
                obj = (TValue)value;
            }
            catch (InvalidCastException ex)
            {
                throw new ArgumentException(this.GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
            }
            if (!this.TryAdd((TKey)key, obj))
                throw new ArgumentException(this.GetResource("ConcurrentDictionary_KeyAlreadyExisted"));
        }


        bool IDictionary.Contains(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (key is TKey)
                return this.ContainsKey((TKey)key);
            else
                return false;
        }


        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return (IDictionaryEnumerator)new ConcurrentDictionary<TKey, TValue>.DictionaryEnumerator(this);
        }


        void IDictionary.Remove(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (!(key is TKey))
                return;
            TValue obj;
            this.TryRemove((TKey)key, out obj);
        }


        //object IDictionary.get_Item(object key)
        //{
        //    if (key == null)
        //        throw new ArgumentNullException("key");
        //    TValue obj;
        //    if (key is TKey && this.TryGetValue((TKey)key, out obj))
        //        return (object)obj;
        //    else
        //        return (object)null;
        //}


        //void IDictionary.set_Item(object key, object value)
        //{
        //    if (key == null)
        //        throw new ArgumentNullException("key");
        //    if (!(key is TKey))
        //        throw new ArgumentException(this.GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
        //    if (!(value is TValue))
        //        throw new ArgumentException(this.GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
        //    this[(TKey)key] = (TValue)value;
        //}


        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", this.GetResource("ConcurrentDictionary_IndexIsNegative"));
            int locksAcquired = 0;
            try
            {
                this.AcquireAllLocks(ref locksAcquired);
                ConcurrentDictionary<TKey, TValue>.Tables tables = this.m_tables;
                int num = 0;
                for (int index1 = 0; index1 < tables.m_locks.Length && num >= 0; ++index1)
                    num += tables.m_countPerLock[index1];
                if (array.Length - num < index || num < 0)
                    throw new ArgumentException(this.GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
                KeyValuePair<TKey, TValue>[] array1 = array as KeyValuePair<TKey, TValue>[];
                if (array1 != null)
                {
                    this.CopyToPairs(array1, index);
                }
                else
                {
                    DictionaryEntry[] array2 = array as DictionaryEntry[];
                    if (array2 != null)
                    {
                        this.CopyToEntries(array2, index);
                    }
                    else
                    {
                        object[] array3 = array as object[];
                        if (array3 == null)
                            throw new ArgumentException(this.GetResource("ConcurrentDictionary_ArrayIncorrectType"), "array");
                        this.CopyToObjects(array3, index);
                    }
                }
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }

        private void GrowTable(ConcurrentDictionary<TKey, TValue>.Tables tables)
        {
            int locksAcquired = 0;
            try
            {
                this.AcquireLocks(0, 1, ref locksAcquired);
                if (tables != this.m_tables)
                    return;
                long num = 0L;
                for (int index = 0; index < tables.m_countPerLock.Length; ++index)
                    num += (long)tables.m_countPerLock[index];
                if (num < (long)(tables.m_buckets.Length / 4))
                {
                    this.m_budget = 2 * this.m_budget;
                    if (this.m_budget >= 0)
                        return;
                    this.m_budget = int.MaxValue;
                }
                else
                {
                    int length1 = 0;
                    bool flag = false;
                    try
                    {
                        length1 = checked(tables.m_buckets.Length * 2 + 1);
                        while (length1 % 3 == 0 || length1 % 5 == 0 || length1 % 7 == 0)
                            checked { length1 += 2; }
                        if (length1 > 2146435071)
                            flag = true;
                    }
                    catch (OverflowException ex)
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        length1 = 2146435071;
                        this.m_budget = int.MaxValue;
                    }
                    this.AcquireLocks(1, tables.m_locks.Length, ref locksAcquired);
                    object[] locks = tables.m_locks;
                    if (this.m_growLockArray && tables.m_locks.Length < 1024)
                    {
                        locks = new object[tables.m_locks.Length * 2];
                        Array.Copy((Array)tables.m_locks, (Array)locks, tables.m_locks.Length);
                        for (int length2 = tables.m_locks.Length; length2 < locks.Length; ++length2)
                            locks[length2] = new object();
                    }
                    ConcurrentDictionary<TKey, TValue>.Node[] buckets = new ConcurrentDictionary<TKey, TValue>.Node[length1];
                    int[] countPerLock = new int[locks.Length];
                    for (int index = 0; index < tables.m_buckets.Length; ++index)
                    {
                        ConcurrentDictionary<TKey, TValue>.Node node2;
                        for (ConcurrentDictionary<TKey, TValue>.Node node1 = tables.m_buckets[index]; node1 != null;
                          node1 = node2
                        )
                        {
                            node2 = node1.m_next;
                            int bucketNo;
                            int lockNo;
                            this.GetBucketAndLockNo(node1.m_hashcode, out bucketNo, out lockNo, buckets.Length, locks.Length);
                            buckets[bucketNo] = new ConcurrentDictionary<TKey, TValue>.Node(node1.m_key, node1.m_value, node1.m_hashcode, buckets[bucketNo]);
                            checked { ++countPerLock[lockNo]; }
                        }
                    }
                    this.m_budget = Math.Max(1, buckets.Length / locks.Length);
                    this.m_tables = new ConcurrentDictionary<TKey, TValue>.Tables(buckets, locks, countPerLock);
                }
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }

        private void GetBucketAndLockNo(int hashcode, out int bucketNo, out int lockNo, int bucketCount, int lockCount)
        {
            bucketNo = (hashcode & int.MaxValue) % bucketCount;
            lockNo = bucketNo % lockCount;
        }

        private void AcquireAllLocks(ref int locksAcquired)
        {
            //if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            //    CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(this.m_tables.m_buckets.Length);
            this.AcquireLocks(0, 1, ref locksAcquired);
            this.AcquireLocks(1, this.m_tables.m_locks.Length, ref locksAcquired);
        }

        private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
        {
            object[] objArray = this.m_tables.m_locks;
            for (int index = fromInclusive; index < toExclusive; ++index)
            {
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(objArray[index], ref lockTaken);
                }
                finally
                {
                    if (lockTaken)
                        ++locksAcquired;
                }
            }
        }

        private void ReleaseLocks(int fromInclusive, int toExclusive)
        {
            for (int index = fromInclusive; index < toExclusive; ++index)
                Monitor.Exit(this.m_tables.m_locks[index]);
        }

        private ReadOnlyCollection<TKey> GetKeys()
        {
            int locksAcquired = 0;
            try
            {
                this.AcquireAllLocks(ref locksAcquired);
                List<TKey> list = new List<TKey>();
                for (int index = 0; index < this.m_tables.m_buckets.Length; ++index)
                {
                    for (ConcurrentDictionary<TKey, TValue>.Node node = this.m_tables.m_buckets[index]; node != null; node = node.m_next)
                        list.Add(node.m_key);
                }
                return new ReadOnlyCollection<TKey>((IList<TKey>)list);
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }

        private ReadOnlyCollection<TValue> GetValues()
        {
            int locksAcquired = 0;
            try
            {
                this.AcquireAllLocks(ref locksAcquired);
                List<TValue> list = new List<TValue>();
                for (int index = 0; index < this.m_tables.m_buckets.Length; ++index)
                {
                    for (ConcurrentDictionary<TKey, TValue>.Node node = this.m_tables.m_buckets[index]; node != null; node = node.m_next)
                        list.Add(node.m_value);
                }
                return new ReadOnlyCollection<TValue>((IList<TValue>)list);
            }
            finally
            {
                this.ReleaseLocks(0, locksAcquired);
            }
        }

        [Conditional("DEBUG")]
        private void Assert(bool condition)
        {
        }

        private string GetResource(string key)
        {
            return key;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            ConcurrentDictionary<TKey, TValue>.Tables tables = this.m_tables;
            this.m_serializationArray = this.ToArray();
            this.m_serializationConcurrencyLevel = tables.m_locks.Length;
            this.m_serializationCapacity = tables.m_buckets.Length;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            KeyValuePair<TKey, TValue>[] keyValuePairArray = this.m_serializationArray;
            ConcurrentDictionary<TKey, TValue>.Node[] buckets = new ConcurrentDictionary<TKey, TValue>.Node[this.m_serializationCapacity];
            int[] countPerLock = new int[this.m_serializationConcurrencyLevel];
            object[] locks = new object[this.m_serializationConcurrencyLevel];
            for (int index = 0; index < locks.Length; ++index)
                locks[index] = new object();
            this.m_tables = new ConcurrentDictionary<TKey, TValue>.Tables(buckets, locks, countPerLock);
            this.InitializeFromCollection((IEnumerable<KeyValuePair<TKey, TValue>>)keyValuePairArray);
            this.m_serializationArray = (KeyValuePair<TKey, TValue>[])null;
        }

        private class Tables
        {
            internal readonly ConcurrentDictionary<TKey, TValue>.Node[] m_buckets;
            internal readonly object[] m_locks;
            internal volatile int[] m_countPerLock;

            internal Tables(ConcurrentDictionary<TKey, TValue>.Node[] buckets, object[] locks, int[] countPerLock)
            {
                this.m_buckets = buckets;
                this.m_locks = locks;
                this.m_countPerLock = countPerLock;
            }
        }

        private class Node
        {
            internal TKey m_key;
            internal TValue m_value;
            internal volatile ConcurrentDictionary<TKey, TValue>.Node m_next;
            internal int m_hashcode;

            internal Node(TKey key, TValue value, int hashcode, ConcurrentDictionary<TKey, TValue>.Node next)
            {
                this.m_key = key;
                this.m_value = value;
                this.m_next = next;
                this.m_hashcode = hashcode;
            }
        }

        private class DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
        {
            private IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator;

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry((object)this.m_enumerator.Current.Key, (object)this.m_enumerator.Current.Value);
                }
            }

            public object Key
            {
                get
                {
                    return (object)this.m_enumerator.Current.Key;
                }
            }

            public object Value
            {
                get
                {
                    return (object)this.m_enumerator.Current.Value;
                }
            }

            public object Current
            {
                get
                {
                    return (object)this.Entry;
                }
            }

            internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                this.m_enumerator = dictionary.GetEnumerator();
            }

            public bool MoveNext()
            {
                return this.m_enumerator.MoveNext();
            }

            public void Reset()
            {
                this.m_enumerator.Reset();
            }
        }
    }
}
