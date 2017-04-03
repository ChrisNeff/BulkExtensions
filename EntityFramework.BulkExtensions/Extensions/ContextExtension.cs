﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace EntityFramework.BulkExtensions.Extensions
{
    /// <summary>
    /// </summary>
    internal static class ContextExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="primaryKeysOnly"></param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        internal static DataTable ToDataTable<TEntity>(this DbContext context, IEnumerable<TEntity> entities, bool primaryKeysOnly = false) where TEntity : class
        {
            var tb = context.CreateDataTable<TEntity>(primaryKeysOnly);
            var tableColumns = primaryKeysOnly
                ? context.GetTablePKs<TEntity>().ToList()
                : context.GetTableColumns<TEntity>().ToList();
            var props = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var item in entities)
            {
                var values = new List<object>();
                foreach (var column in tableColumns)
                {
                    var prop = props.SingleOrDefault(info => info.Name == column.PropertyName);
                    if (prop != null)
                        values.Add(prop.GetValue(item, null));
                }

                tb.Rows.Add(values.ToArray());
            }

            return tb;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static DbContextTransaction InternalTransaction(this DbContext context)
        {
            DbContextTransaction transaction = null;
            if (context.Database.CurrentTransaction == null)
            {
                transaction = context.Database.BeginTransaction();
            }
            return transaction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="context"></param>
        /// <param name="collection"></param>
        internal static void UpdateEntityState<TEntity>(this DbContext context, IEnumerable<TEntity> collection) where TEntity : class
        {
            try
            {
                var list = collection.ToList();
                context.Configuration.AutoDetectChangesEnabled = false;

                foreach (var entity in list)
                {
                    context.Entry(entity).State = EntityState.Unchanged;
                }
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="context"></param>
        /// <param name="collection"></param>
        internal static void DetachEntityFromContext<TEntity>(this DbContext context, IEnumerable<TEntity> collection) where TEntity : class
        {
            try
            {
                var list = collection.ToList();
                var objectContext = ((IObjectContextAdapter)context).ObjectContext;

                context.Configuration.AutoDetectChangesEnabled = false;
                foreach (var entity in list)
                {
                    objectContext.Detach(entity);
                }
            }
            finally
            {
                context.Configuration.AutoDetectChangesEnabled = true;
            }
        }

        private static DataTable CreateDataTable<TEntity>(this DbContext context, bool primaryKeysOnly = false) where TEntity : class
        {
            var table = new DataTable();
            var columns = primaryKeysOnly ? context.GetTablePKs<TEntity>() : context.GetTableColumns<TEntity>();
            foreach (var prop in columns)
            {
                table.Columns.Add(prop.ColumnName, Nullable.GetUnderlyingType(prop.Type) ?? prop.Type);
            }

            table.TableName = nameof(TEntity);
            return table;
        }
    }
}