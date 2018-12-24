﻿/***********************************
 * Coder：EminemJK
 * Date：2018-11-21
 * 
 * Last Update：2018-12-24
 **********************************/

using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Banana.Utility.Redis
{
    public class RedisUtils
    {
        /// <summary>  
        /// redis配置文件信息  
        /// </summary>  
        public static string RedisPath = "172.16.3.82:6379";
        
        private static object _locker = new Object();
        private static ConnectionMultiplexer _instance = null;

        /// <summary>
        /// 使用一个静态属性来返回已连接的实例，如下列中所示。这样，一旦 ConnectionMultiplexer 断开连接，便可以初始化新的连接实例。
        /// </summary>
        public static ConnectionMultiplexer Instance
        {
            get
            {
                //if (_instance == null)
                //{
                //    lock (_locker)
                //    {
                        if (_instance == null || !_instance.IsConnected)
                        {
                            _instance = ConnectionMultiplexer.Connect(RedisPath);
                            //注册如下事件
                            _instance.ConnectionFailed += MuxerConnectionFailed;
                            _instance.ConnectionRestored += MuxerConnectionRestored;
                            _instance.ErrorMessage += MuxerErrorMessage;
                            _instance.HashSlotMoved += MuxerHashSlotMoved;
                            _instance.InternalError += MuxerInternalError;
                        }
                //    }
                //}
                return _instance;
            }
        }

        #region Keys
        /// <summary>
        /// 判断键是否存在
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">键值</param>
        /// <returns></returns>
        public static bool KeyExists(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.KeyExists(key);
        }

        /// <summary>
        /// 为指定的键设置失效时间
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">键</param>
        /// <param name="expiry">时间间隔</param>
        /// <returns></returns>
        public static bool SetExpire(int dbIndex, string key, TimeSpan? expiry)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.KeyExpire(key, expiry);
        }

        /// <summary>
        ///  为指定的键设置失效时间
        /// </summary> 
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">键</param>
        /// <param name="timeout">时间间隔（秒）</param>
        /// <returns></returns>
        public static bool SetExpire(int dbIndex, string key, int timeout = 0)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.KeyExpire(key, DateTime.Now.AddSeconds(timeout));
        }

        /// <summary>
        ///  删除键
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static bool KeyDelete(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.KeyDelete(key);
        }

        /// <summary>
        ///  键重命名
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="oldKey">旧值</param>
        /// <param name="newKey">新值</param>
        /// <returns></returns>
        public static bool KeyRename(int dbIndex, string oldKey, string newKey)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.KeyRename(oldKey, newKey);
        }
        #endregion

        #region Strings
        /// <summary>
        /// 获取字符串数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">Redis键</param>
        /// <returns></returns>
        public static string StringGet(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            if (db != null)
            {
                return db.StringGet(key);
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取对象类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">Redis键</param>
        /// <returns></returns>
        public static T StringGet<T>(int dbIndex, string key) where T : class
        {
            T data = default(T);
            var db = Instance.GetDatabase(dbIndex);
            if (db != null)
            {
                var value = db.StringGet(key);
                if (string.IsNullOrWhiteSpace(value))
                    return data;
                return JsonConvert.DeserializeObject<T>(value);
            }
            return data;
        }

        /// <summary>
        /// 设置值类型的值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool StringSet(int dbIndex, string key, RedisValue value, TimeSpan? expiry)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.StringSet(key, value, expiry);
        }

        /// <summary>
        /// 设置对象类型的值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool StringSet<T>(int dbIndex, string key, T value, TimeSpan? expiry) where T : class
        {
            if (value == default(T))
            {
                return false;
            }
            var db = Instance.GetDatabase(dbIndex);
            return db.StringSet(key, JsonConvert.SerializeObject(value), expiry);
        }
        #endregion

        #region Hashes
        /// <summary>
        /// Hash是否存在
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">HashId</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static bool HashExists(int dbIndex, string hashId, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.HashExists(hashId, key);
        }

        /// <summary>
        /// Hash长度
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">HashId</param>
        /// <returns></returns>
        public static long HashLength(int dbIndex, string hashId)
        {
            var db = Instance.GetDatabase(dbIndex);
            var length = db.HashLength(hashId);
            return length;
        }

        /// <summary>
        /// 设置哈希值
        /// </summary>
        /// <typeparam name="T">哈希值类型</typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">哈希ID</param>
        /// <param name="key">键</param>
        /// <param name="t">哈希值</param>
        /// <returns></returns>
        public static bool HashSet<T>(int dbIndex, string hashId, string key, T t)
        {
            var db = Instance.GetDatabase(dbIndex);
            var value = JsonConvert.SerializeObject(t);
            return db.HashSet(hashId, key, value);
        }

        /// <summary>
        ///   获取哈希值
        /// </summary>
        /// <typeparam name="T">哈希值类型</typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">哈希ID</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static T HashGet<T>(int dbIndex, string hashId, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            string value = db.HashGet(hashId, key);
            if (string.IsNullOrWhiteSpace(value))
                return default(T);
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        ///   获取哈希值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">哈希ID</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static string HashGet(int dbIndex, string hashId, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            string value = db.HashGet(hashId, key).ToString();
            return value;
        }

        /// <summary>
        /// 获取哈希值的所有键
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">哈希ID</param>
        /// <returns></returns>
        public static List<string> HashKeys(int dbIndex, string hashId)
        {
            var db = Instance.GetDatabase(dbIndex);
            var result = new List<string>();
            var list = db.HashKeys(hashId).ToList();
            if (list.Count > 0)
            {
                list.ForEach(x =>
                {
                    var value = JsonConvert.DeserializeObject<string>(x);
                    result.Add(value);
                });
            }
            return result;
        }

        /// <summary>
        /// 获取所有哈希值
        /// </summary>
        /// <typeparam name="T">哈希值类型</typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">哈希ID</param>
        /// <returns></returns>
        public static List<T> HashGetAll<T>(int dbIndex, string hashId)
        {
            var db = Instance.GetDatabase(dbIndex);
            var result = new List<T>();
            var list = db.HashGetAll(hashId).ToList();
            if (list.Count > 0)
            {
                list.ForEach(x =>
                {
                    var value = JsonConvert.DeserializeObject<T>(x.Value);
                    result.Add(value);
                });
            }
            return result;
        }

        /// <summary>
        ///  删除哈希值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="hashId">哈希ID</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static bool HashDelete(int dbIndex, string hashId, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.HashDelete(hashId, key);
        }
        #endregion

        #region Lists
        /// <summary>
        /// 集合长度
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="listId">集合ID</param>
        /// <returns></returns>
        public static long ListLength(int dbIndex, string listId)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.ListLength(listId);
        }

        /// <summary>
        /// 向集合中添加元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="listId">集合ID</param>
        /// <param name="list">元素值</param>
        /// <returns></returns>
        public static long AddList<T>(int dbIndex, string listId, List<T> list)
        {
            var db = Instance.GetDatabase(dbIndex);
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    db.ListRightPush(listId, JsonConvert.SerializeObject(item));
                }
            }
            return ListLength(dbIndex, listId);
        }

        /// <summary>
        /// 获取集合元素(默认获取整个集合)
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="listId">集合ID</param>
        /// <param name="start">起始位置(0表示第1个位置)</param>
        /// <param name="stop">结束位置(-1表示倒数第1个位置)</param>
        /// <returns></returns>
        public static List<T> GetList<T>(int dbIndex, string listId, long start = 0, long stop = -1)
        {
            var db = Instance.GetDatabase(dbIndex);
            var result = new List<T>();
            var list = db.ListRange(listId, start, stop).ToList();
            if (list.Count > 0)
            {
                list.ForEach(x =>
                {
                    var value = JsonConvert.DeserializeObject<T>(x);
                    result.Add(value);
                });
            }
            return result;
        }
        #endregion

        #region ZSet

        #region 同步方法

        /// <summary>
        /// 添加一个值到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public static bool SortedSetAdd<T>(int dbIndex, string key, T value, double? score = null)
        {
            var db = Instance.GetDatabase(dbIndex);
            double scoreNum = score ?? _GetScore(key, db);
            return db.SortedSetAdd(key, ConvertJson<T>(value), scoreNum);
        }

        /// <summary>
        /// 添加一个集合到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public static long SortedSetAdd<T>(int dbIndex, string key, List<T> value, double? score = null)
        {
            var db = Instance.GetDatabase(dbIndex);
            double scoreNum = score ?? _GetScore(key, db);
            SortedSetEntry[] rValue = value.Select(o => new SortedSetEntry(ConvertJson<T>(o), scoreNum++)).ToArray();
            return db.SortedSetAdd(key, rValue);
        }

        /// <summary>
        /// 获取集合中的数量
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static long SortedSetLength(int dbIndex,string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.SortedSetLength(key);
        }

        /// <summary>
        /// 获取指定起始值到结束值的集合数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public static long SortedSetLengthByValue<T>(int dbIndex, string key, T startValue, T endValue)
        {
            var db = Instance.GetDatabase(dbIndex);
            var sValue = ConvertJson<T>(startValue);
            var eValue = ConvertJson<T>(endValue);
            return db.SortedSetLengthByValue(key, sValue, eValue);
        }

        /// <summary>
        /// 获取指定Key的排序Score值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double? SortedSetScore<T>(int dbIndex, string key, T value)
        {
            var db = Instance.GetDatabase(dbIndex);
            var rValue = ConvertJson<T>(value);
            return db.SortedSetScore(key, rValue);
        }

        /// <summary>
        /// 获取指定Key中最小Score值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static double SortedSetMinScore(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            double dValue = 0;
            var rValue = db.SortedSetRangeByRankWithScores(key, 0, 0, Order.Ascending).FirstOrDefault();
            dValue = rValue != null ? rValue.Score : 0;
            return dValue;
        }

        /// <summary>
        /// 获取指定Key中最大Score值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static double SortedSetMaxScore(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            double dValue = 0;
            var rValue = db.SortedSetRangeByRankWithScores(key, 0, 0, Order.Descending).FirstOrDefault();
            dValue = rValue != null ? rValue.Score : 0;
            return dValue;
        }

        /// <summary>
        /// 删除Key中指定的值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static long SortedSetRemove<T>(int dbIndex, string key, params T[] value)
        {
            var db = Instance.GetDatabase(dbIndex);
            var rValue = ConvertRedisValue<T>(value);
            return db.SortedSetRemove(key, rValue);
        }

        /// <summary>
        /// 删除指定起始值到结束值的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public static long SortedSetRemoveRangeByValue<T>(int dbIndex, string key, T startValue, T endValue)
        {
            var db = Instance.GetDatabase(dbIndex);
            var sValue = ConvertJson<T>(startValue);
            var eValue = ConvertJson<T>(endValue);
            return db.SortedSetRemoveRangeByValue(key, sValue, eValue);
        }

        /// <summary>
        /// 删除 从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public static long SortedSetRemoveRangeByRank(int dbIndex, string key, long start, long stop)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.SortedSetRemoveRangeByRank(key, start, stop);
        }

        /// <summary>
        /// 根据排序分数Score，删除从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public static long SortedSetRemoveRangeByScore(int dbIndex, string key, double start, double stop)
        {
            var db = Instance.GetDatabase(dbIndex);
            return db.SortedSetRemoveRangeByScore(key, start, stop);
        }

        /// <summary>
        /// 获取从 start 开始的 stop 条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="start">起始数</param>
        /// <param name="stop">-1表示到结束，0为1条</param>
        /// <param name="desc">是否按降序排列</param>
        /// <returns></returns>
        public static List<T> SortedSetRangeByRank<T>(int dbIndex, string key, long start = 0, long stop = -1, bool desc = false)
        {
            var db = Instance.GetDatabase(dbIndex);
            Order orderBy = desc ? Order.Descending : Order.Ascending;
            var rValue = db.SortedSetRangeByRank(key, start, stop, orderBy);
            return ConvetList<T>(rValue);
        }
        #endregion

        #region 异步方法

        /// <summary>
        /// 添加一个值到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        ///  <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public static async Task<bool> SortedSetAddAsync<T>(int dbIndex, string key, T value, double? score = null)
        {
            var db = Instance.GetDatabase(dbIndex);
            double scoreNum = score ?? _GetScore(key, db);
            return await db.SortedSetAddAsync(key, ConvertJson<T>(value), scoreNum);
        }

        /// <summary>
        /// 添加一个集合到Key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        ///  <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="score">排序分数，为空将获取集合中最大score加1</param>
        /// <returns></returns>
        public static async Task<long> SortedSetAddAsync<T>(int dbIndex, string key, List<T> value, double? score = null)
        {
            var db = Instance.GetDatabase(dbIndex);
            double scoreNum = score ?? _GetScore(key, db);
            SortedSetEntry[] rValue = value.Select(o => new SortedSetEntry(ConvertJson<T>(o), scoreNum++)).ToArray();
            return await db.SortedSetAddAsync(key, rValue);
        }

        /// <summary>
        /// 获取集合中的数量
        /// </summary>
        ///  <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static async Task<long> SortedSetLengthAsync(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            return await db.SortedSetLengthAsync(key);
        }

        /// <summary>
        /// 获取指定起始值到结束值的集合数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public static async Task<long> SortedSetLengthByValueAsync<T>(int dbIndex, string key, T startValue, T endValue)
        {
            var db = Instance.GetDatabase(dbIndex);
            var sValue = ConvertJson<T>(startValue);
            var eValue = ConvertJson<T>(endValue);
            return await db.SortedSetLengthByValueAsync(key, sValue, eValue);
        }

        /// <summary>
        /// 获取指定Key中最大Score值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static async Task<double> SortedSetMaxScoreAsync(int dbIndex, string key)
        {
            var db = Instance.GetDatabase(dbIndex);
            double dValue = 0;
            var rValue = (await db.SortedSetRangeByRankWithScoresAsync(key, 0, 0, Order.Descending)).FirstOrDefault();
            dValue = rValue != null ? rValue.Score : 0;
            return dValue;
        }

        /// <summary>
        /// 删除Key中指定的值
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static async Task<long> SortedSetRemoveAsync<T>(int dbIndex, string key, params T[] value)
        {
            var db = Instance.GetDatabase(dbIndex);
            var rValue = ConvertRedisValue<T>(value);
            return await db.SortedSetRemoveAsync(key, rValue);
        }

        /// <summary>
        /// 删除指定起始值到结束值的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="startValue">起始值</param>
        /// <param name="endValue">结束值</param>
        /// <returns></returns>
        public static async Task<long> SortedSetRemoveRangeByValueAsync<T>(int dbIndex, string key, T startValue, T endValue)
        {
            var db = Instance.GetDatabase(dbIndex);
            var sValue = ConvertJson<T>(startValue);
            var eValue = ConvertJson<T>(endValue);
            return await db.SortedSetRemoveRangeByValueAsync(key, sValue, eValue);
        }

        /// <summary>
        /// 删除 从 start 开始的 stop 条数据
        /// </summary>
        /// <param name="dbIndex">数据库</param>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public static async Task<long> SortedSetRemoveRangeByRankAsync(int dbIndex, string key, long start, long stop)
        {
            var db = Instance.GetDatabase(dbIndex);
            return await db.SortedSetRemoveRangeByRankAsync(key, start, stop);
        }

        #endregion

        #region 内部辅助方法

        /// <summary>
        /// 将对象转换成string字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertJson<T>(T value)
        {
            string result = value is string ? value.ToString() :
                JsonConvert.SerializeObject(value, Formatting.None);
            return result;
        }
        /// <summary>
        /// 获取指定Key中最大Score值,
        /// </summary>
        /// <param name="key">key名称，注意要先添加上Key前缀</param>
        /// <returns></returns>
        private static double _GetScore(string key, IDatabase db)
        {
            double dValue = 0;
            var rValue = db.SortedSetRangeByRankWithScores(key, 0, 0, Order.Descending).FirstOrDefault();
            dValue = rValue != null ? rValue.Score : 0;
            return dValue + 1;
        }

        /// <summary>
        /// 将值集合转换成RedisValue集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisValues"></param>
        /// <returns></returns>
        private static RedisValue[] ConvertRedisValue<T>(params T[] redisValues) => redisValues.Select(o => (RedisValue)ConvertJson<T>(o)).ToArray();

        /// <summary>
        /// 将值反系列化成对象集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<T> ConvetList<T>(RedisValue[] values)
        {
            List<T> result = new List<T>();
            foreach (var item in values)
            {
                var model = ConvertObj<T>(item);
                result.Add(model);
            }
            return result;
        }

        /// <summary>
        /// 将值反系列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ConvertObj<T>(RedisValue value)
        {
            return value.IsNullOrEmpty ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// 获取几个集合的交叉并集合,并保存到一个新Key中
        /// </summary>
        /// <param name="db"></param>
        /// <param name="operation">Union：并集  Intersect：交集  Difference：差集  详见 <see cref="SetOperation"/></param>
        /// <param name="destination">保存的新Key名称</param>
        /// <param name="keys">要操作的Key集合</param>
        /// <returns></returns>
        private static long _SortedSetCombineAndStore(IDatabase db, SetOperation operation, string destination, params string[] keys)
        {
            RedisKey[] keyList = ConvertRedisKeysAddSysCustomKey(keys);
            var rValue = db.SortedSetCombineAndStore(operation, destination, keyList);
            return rValue;

        }

        /// <summary>
        /// 将string类型的Key转换成 <see cref="RedisKey"/> 型的Key，并添加前缀字符串
        /// </summary>
        /// <param name="redisKeys"></param>
        /// <returns></returns>
        private static RedisKey[] ConvertRedisKeysAddSysCustomKey(params string[] redisKeys) => redisKeys.Select(redisKey => (RedisKey)redisKey).ToArray();
        #endregion

        #endregion

        #region  当作消息代理中间件使用 一般使用更专业的消息队列来处理这种业务场景
        /// <summary>
        /// 当作消息代理中间件使用
        /// 消息组建中,重要的概念便是生产者,消费者,消息中间件。
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static long Publish(string channel, string message)
        {
            ISubscriber sub = Instance.GetSubscriber();
            //return sub.Publish("messages", "hello");
            return sub.Publish(channel, message);
        }

        /// <summary>
        /// 在消费者端得到该消息并输出
        /// </summary>
        /// <param name="channelFrom"></param>
        /// <returns></returns>
        public static void Subscribe(string channelFrom)
        {
            ISubscriber sub = Instance.GetSubscriber();
            sub.Subscribe(channelFrom, (channel, message) =>
            {
                Console.WriteLine((string)message);
            });
        }
        #endregion

        #region EventHandler
        /// <summary>
        /// 连接失败 ， 如果重新连接成功你将不会收到这个通知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {

        }

        /// <summary>
        /// 重新建立连接之前的错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {

        }

        /// <summary>
        /// 发生错误时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerErrorMessage(object sender, RedisErrorEventArgs e)
        {
        }

        /// <summary>
        /// 更改集群
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerHashSlotMoved(object sender, HashSlotMovedEventArgs e)
        {
            // LogHelper.WriteInfoLog("HashSlotMoved:NewEndPoint" + e.NewEndPoint + ", OldEndPoint" + e.OldEndPoint);
        }

        /// <summary>
        /// redis类库错误
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MuxerInternalError(object sender, InternalErrorEventArgs e)
        {
        } 
        #endregion
    }
}
