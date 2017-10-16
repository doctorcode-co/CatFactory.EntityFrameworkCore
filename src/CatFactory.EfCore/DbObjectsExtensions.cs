﻿using System;
using System.Linq;
using CatFactory.CodeFactory;
using CatFactory.DotNetCore;
using CatFactory.Mapping;

namespace CatFactory.EfCore
{
    public static class DbObjectsExtensions
    {
        private static ICodeNamingConvention namingConvention;

        static DbObjectsExtensions()
        {
            namingConvention = new DotNetNamingConvention();
        }

        public static String GetSingularName(this IDbObject dbObject)
            => NamingService.GetSingularName(dbObject.GetEntityName());

        public static String GetSingularName(this DbObject dbObject)
            => NamingService.GetSingularName(dbObject.GetEntityName());

        public static String GetPluralName(this DbObject dbObject)
            => NamingService.GetPluralName(dbObject.GetEntityName());

        public static String GetPluralName(this IDbObject dbObject)
            => NamingService.GetPluralName(dbObject.GetEntityName());

        public static String GetEntityName(this IDbObject dbObject)
            => String.Format("{0}", namingConvention.GetClassName(dbObject.Name));

        public static String GetEntityName(this DbObject dbObject)
            => String.Format("{0}", namingConvention.GetClassName(dbObject.Name));

        public static String GetViewModelName(this IDbObject dbObject)
            => String.Format("{0}ViewModel", namingConvention.GetClassName(dbObject.GetSingularName()));

        public static String GetDataContractName(this IDbObject dbObject)
            => String.Format("{0}Dto", namingConvention.GetClassName(dbObject.Name));

        public static String GetMapName(this IDbObject dbObject)
            => namingConvention.GetClassName(String.Format("{0}Map", dbObject.GetSingularName()));

        public static String GetDbContextName(this Database db)
            => namingConvention.GetClassName(String.Format("{0}DbContext", db.Name));

        public static String GetDbEntityMapperName(this Database db)
            => namingConvention.GetClassName(String.Format("{0}EntityMapper", db.Name));

        public static String GetGetAllRepositoryMethodName(this IDbObject dbObject)
            => String.Format("Get{0}", dbObject.GetPluralName());

        public static String GetGetByUniqueRepositoryMethodName(this IDbObject dbObject, Unique unique)
            => String.Format("Get{0}By{1}Async", dbObject.GetSingularName(), String.Join("And", unique.Key.Select(item => namingConvention.GetPropertyName(item))));

        public static String GetGetRepositoryMethodName(this IDbObject dbObject)
            => String.Format("Get{0}Async", dbObject.GetSingularName());

        public static String GetAddRepositoryMethodName(this ITable dbObject)
            => String.Format("Add{0}Async", dbObject.GetSingularName());

        public static String GetUpdateRepositoryMethodName(this ITable dbObject)
            => String.Format("Update{0}Async", dbObject.GetSingularName());

        public static String GetRemoveRepositoryMethodName(this ITable dbObject)
            => String.Format("Remove{0}Async", dbObject.GetSingularName());

        public static String GetFullColumnName(this ITable table, Column column)
            => String.Join(".", new String[] { table.Schema, table.Name, column.Name });

        public static Boolean HasDefaultSchema(this IDbObject table)
            => String.IsNullOrEmpty(table.Schema) || String.Compare(table.Schema, "dbo", true) == 0;

        public static Boolean IsPrimaryKeyGuid(this Table table)
            => table.PrimaryKey != null && table.PrimaryKey.Key.Count == 1 && table.Columns[0].Type == "uniqueidentifier" ? true : false;
    }
}
