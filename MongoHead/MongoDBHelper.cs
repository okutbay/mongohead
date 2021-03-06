﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoHead
{
    public class MongoDBHelper
    {
        readonly MongoDBConfig _Config;
        private IMongoDatabase Db { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// "BsonDocumentIDFieldName" is used to access specific "_id" field property name of the BSON document to access it in run-time for insert, update or delete purposes
        /// </summary>
        public static string BsonDocumentIDFieldName { get { return "_id"; } }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="EntityType"></param>
        public MongoDBHelper(MongoDBConfig Config, Type EntityType)
        {
            _Config = Config;

            this.CollectionName = EntityType.Name;

            this.Db = this.GetDBInstance();
        }

        /// <summary>
        /// Returns an instance of Mongo Database with database name defined in the config object. 
        /// </summary>
        /// <returns></returns>
        private IMongoDatabase GetDBInstance()
        {
            string connectionString = string.Empty;
            string dbName = string.Empty;

            connectionString = this._Config.ConnectionString;
            dbName = this._Config.DatabaseName;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("MongoHead.MongoDBHelper config error: invalid or undefined connection string setting");
            }

            if (string.IsNullOrEmpty(dbName))
            {
                throw new Exception("MongoHead.MongoDBHelper config error: DBName is not set. Please check your DefaultDatabaseName setting or PreferredDBName");
            }

            MongoClient client = new MongoClient(connectionString);
            IMongoDatabase _db = client.GetDatabase(dbName);

            return _db;
        }


        //SAVE DATA METHODS **************************************************************
        #region SAVE

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ObjectToSave"></param>
        /// <returns></returns>
        public ObjectId Insert(object ObjectToSave)
        {
            BsonDocument document = ObjectToSave.ToBsonDocument(); //conversion to BsonDocument adds _t as type of the object to the 
            document.Remove("_t"); //we dont want this just remove
            ObjectId newId = this.Insert(document);

            return newId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BsonDocumentToSave"></param>
        /// <returns></returns>
        public ObjectId Insert(BsonDocument BsonDocumentToSave)
        {
            IMongoCollection<BsonDocument> collection = Db.GetCollection<BsonDocument>(CollectionName);

            collection.InsertOne(BsonDocumentToSave);

            string id = BsonDocumentToSave[MongoDBHelper.BsonDocumentIDFieldName].ToString();
            ObjectId newId = new ObjectId(id);

            return newId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ObjectToReplace"></param>
        /// <returns></returns>
        public ObjectId Replace(object ObjectToReplace, ObjectId Id)
        {
            BsonDocument document = ObjectToReplace.ToBsonDocument(); //conversion to BsonDocument adds _t as type of the object to the 
            document.Remove("_t"); //we dont want this just remove
            ObjectId id = this.Replace(document, Id);

            return id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BsonDocumentToReplace"></param>
        /// <returns></returns>
        public ObjectId Replace(BsonDocument BsonDocumentToReplace, ObjectId Id)
        {
            //string id = BsonDocumentToReplace[MongoDBHelper.BsonDocumentIDFieldName].ToString();
            //ObjectId updateId = new ObjectId(id);

            IMongoCollection<BsonDocument> collection = Db.GetCollection<BsonDocument>(CollectionName);
            var filter = Builders<BsonDocument>.Filter.Eq(MongoDBHelper.BsonDocumentIDFieldName, Id);
            collection.ReplaceOne(filter, BsonDocumentToReplace);

            return Id;
        }

        #endregion


        // GET LIST DATA METHODS **************************************************************
        #region GET LIST

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetList<T>()
        {
            //TODO bunun icinde asagidaki gibi bir cagriyla cozebilir miyiz test edelim
            //return GetList<T>(null);

            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

            var filter = new BsonDocument();

            List<T> list = new List<T>();
            foreach (var item in collection.Find(filter).ToEnumerable())
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Returns documents matches ObjectId value in the field with specified KeyFieldName. This field may be default _id field or another field with ObjectId
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="KeyFieldName"></param>
        /// <returns></returns>
        public List<T> GetList<T>(ObjectId Key, string KeyFieldName)
        {
            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);
            var filter = Builders<T>.Filter.Eq(KeyFieldName, Key);
            var foundItems = collection.Find(filter).ToList();
            return foundItems;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Filter"></param>
        /// <returns></returns>
        public List<T> GetList<T>(List<Filter> Filter)
        {
            List<T> foundItems = GetList<T>(Filter, true);
            return foundItems;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Filter"></param>
        /// <param name="UseAndLogic"></param>
        /// <returns></returns>
        public List<T> GetList<T>(List<Filter> Filter, bool UseAndLogic = true)
        {
            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

            var exp = ExpressionBuilder.GetExpression<T>(Filter, UseAndLogic);
            var query = Builders<T>.Filter.Where(exp);

            IEnumerable<T> cursor = collection.Find(query).ToEnumerable();

            List<T> list = new List<T>();
            foreach (var item in cursor)
            {
                list.Add(item);
            }

            return list;
        }

        #endregion


        // GET DATA METHODS **************************************************************
        #region GET

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Filter"></param>
        /// <returns></returns>
        public T Get<T>(List<Filter> Filter)
        {
            T foundItem = Get<T>(Filter, true);
            return foundItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Filter"></param>
        /// <param name="UseAndLogic"></param>
        /// <returns></returns>
        public T Get<T>(List<Filter> Filter, bool UseAndLogic = true)
        {
            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

            var exp = ExpressionBuilder.GetExpression<T>(Filter, UseAndLogic);
            var query = Builders<T>.Filter.Where(exp);

            var foundItem = collection.Find(query).FirstOrDefault();

            return foundItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_id"></param>
        /// <returns></returns>
        public T GetByObjectId<T>(ObjectId _id)
        {
            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);
            var filter = Builders<T>.Filter.Eq(MongoDBHelper.BsonDocumentIDFieldName /*"_id"*/, _id);
            var foundItem = (T)collection.Find(filter).FirstOrDefault();
            return foundItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="FieldName"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public T GetByFieldValue<T>(string FieldName, object Value)
        {
            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

            List<Filter> filter = new List<Filter>()
            {
                new Filter { PropertyName = FieldName , Operation = Op.Equals, Value = Value }
            };

            var exp = ExpressionBuilder.GetExpression<T>(filter);
            var query = Builders<T>.Filter.Where(exp);

            var foundItem = (T)collection.Find(query).FirstOrDefault();
            return foundItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="SortFieldName"></param>
        /// <returns></returns>
        public T GetLast<T>(string SortFieldName)
        {
            //TODO bunun icinde asagidaki gibi bir cagriyla cozebilir miyiz test edelim
            //return GetLast<T>(null, FieldName);

            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

            var filter = new BsonDocument();
            var sortBy = Builders<T>.Sort.Descending(SortFieldName);
            var foundItem = collection.Find(filter).Sort(sortBy).Limit(1).FirstOrDefault();

            return foundItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Filter"></param>
        /// <param name="SortFieldName"></param>
        /// <returns></returns>
        public T GetLast<T>(List<Filter> Filter, string SortFieldName)
        {
            var foundItem = GetLast<T>(Filter, SortFieldName, true);
            return foundItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Filter"></param>
        /// <param name="SortFieldName"></param>
        /// <param name="UseAndLogic"></param>
        /// <returns></returns>
        public T GetLast<T>(List<Filter> Filter, string SortFieldName, bool UseAndLogic = true)
        {
            IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

            var exp = ExpressionBuilder.GetExpression<T>(Filter, UseAndLogic);
            var query = Builders<T>.Filter.Where(exp);
            var sortBy = Builders<T>.Sort.Descending(SortFieldName);

            var foundItem = collection.Find(query).Sort(sortBy).Limit(1).FirstOrDefault();
            return foundItem;
        }

        #endregion


        //DELETE DATA METHODS **************************************************************
        #region DELETE

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_id"></param>
        /// <returns></returns>
        public bool Delete<T>(ObjectId _id)
        {
            try
            {
                IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

                List<Filter> filter = new List<Filter>()
                {
                    new Filter { PropertyName = /*"_id"*/ MongoDBHelper.BsonDocumentIDFieldName, Operation = Op.Equals, Value = _id }
                };

                var exp = ExpressionBuilder.GetExpression<T>(filter);
                var query = Builders<T>.Filter.Where(exp);

                DeleteResult result = collection.DeleteOne(query);

                return true;
            }
            catch (Exception ex)
            {
                //log exception code 
                return false;

                //throw ex;
            }
        }

        /// <summary>
        /// Deletes all documents that matches the value in named field.
        /// </summary>
        /// <typeparam name="T">Related Entity (Collection) type</typeparam>
        /// <param name="FieldName">Field Name to match the Value</param>
        /// <param name="Value">Value to match in field named with FieldName</param>
        /// <param name="DeletedCount">Number of deleted documents</param>
        /// <returns></returns>
        public bool DeleteByFieldValue<T>(string FieldName, object Value, out long DeletedCount)
        {
            try
            {
                IMongoCollection<T> collection = Db.GetCollection<T>(CollectionName);

                List<Filter> filter = new List<Filter>()
                {
                    new Filter { PropertyName = FieldName, Operation = Op.Equals, Value = Value }
                };

                var exp = ExpressionBuilder.GetExpression<T>(filter);
                var query = Builders<T>.Filter.Where(exp);

                DeleteResult result = collection.DeleteMany(query);
                DeletedCount = result.DeletedCount;

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                //log exception code 
                DeletedCount = 0;
                return false;
                //throw ex;
            }
        }

        #endregion


        // GENERAL DATABASE METHODS ********************************************************
        public bool CollectionExists(string CollectionName)
        {
            var filter = new BsonDocument("name", CollectionName);
            var options = new ListCollectionNamesOptions { Filter = filter };
            return Db.ListCollectionNames(options).Any();
        }

        public async Task<bool> CollectionExistsAsync(string CollectionName)
        {
            var filter = new BsonDocument("name", CollectionName);
            var collections = await Db.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }

    }

    /// <summary>
    /// Used to store parameter values to connect and operate on MongoDB
    /// 
    /// This class also contains KeyName static properties to access values of the settings file
    /// </summary>
    public class MongoDBConfig
    {
        /// <summary>
        /// Key name for JSON Settings file
        /// </summary>
        public static string KeyNameConnectionString = "MongoDBConfig:ConnectionString";

        /// <summary>
        /// Key name for JSON Settings file
        /// </summary>
        public static string KeyNameDatabaseName = "MongoDBConfig:DatabaseName";

        /// <summary>
        /// This parameter must contain MongoDB connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// This is the database name to work with
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Default construtor with two required parameteters
        /// </summary>
        /// <param name="ConnectionString">Check class definition for details</param>
        /// <param name="DatabaseName">Check class definition for details</param>
        public MongoDBConfig(string ConnectionString, string DatabaseName)
        {
            this.ConnectionString = ConnectionString;
            this.DatabaseName = DatabaseName;
        }
    }
}
