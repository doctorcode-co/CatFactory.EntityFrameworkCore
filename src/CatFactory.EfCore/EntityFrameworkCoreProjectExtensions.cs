﻿using System;
using System.IO;
using System.Linq;
using CatFactory.CodeFactory;
using CatFactory.DotNetCore;
using CatFactory.Mapping;
using CatFactory.OOP;

namespace CatFactory.EfCore
{
    public static class EntityFrameworkCoreProjectExtensions
    {
        private static ICodeNamingConvention namingConvention;

        static EntityFrameworkCoreProjectExtensions()
        {
            namingConvention = new DotNetNamingConvention();
        }

        public static string GetEntityLayerNamespace(this EntityFrameworkCoreProject project)
            => string.Join(".", namingConvention.GetClassName(project.Name), namingConvention.GetNamespace(project.Namespaces.EntityLayer));

        public static string GetEntityLayerNamespace(this EntityFrameworkCoreProject project, string ns)
            => string.IsNullOrEmpty(ns) ? GetEntityLayerNamespace(project) : string.Join(".", project.Name, project.Namespaces.EntityLayer, ns);

        public static string GetDataLayerNamespace(this EntityFrameworkCoreProject project)
            => string.Join(".", new string[] { namingConvention.GetClassName(project.Name), project.Namespaces.DataLayer });

        public static string GetDataLayerConfigurationsNamespace(this EntityFrameworkCoreProject project)
            => string.Join(".", namingConvention.GetClassName(project.Name), project.Namespaces.DataLayer, project.Namespaces.Configurations);

        public static string GetDataLayerContractsNamespace(this EntityFrameworkCoreProject project)
            => string.Join(".", namingConvention.GetClassName(project.Name), project.Namespaces.DataLayer, project.Namespaces.Contracts);

        public static string GetDataLayerDataContractsNamespace(this EntityFrameworkCoreProject project)
            => string.Join(".", namingConvention.GetClassName(project.Name), project.Namespaces.DataLayer, project.Namespaces.DataContracts);

        public static string GetDataLayerRepositoriesNamespace(this EntityFrameworkCoreProject project)
            => string.Join(".", namingConvention.GetClassName(project.Name), project.Namespaces.DataLayer, project.Namespaces.Repositories);

        public static string GetEntityLayerDirectory(this EntityFrameworkCoreProject project)
            => Path.Combine(project.OutputDirectory, project.Namespaces.EntityLayer);

        public static string GetDataLayerDirectory(this EntityFrameworkCoreProject project)
            => Path.Combine(project.OutputDirectory, project.Namespaces.DataLayer);

        public static string GetDataLayerConfigurationsDirectory(this EntityFrameworkCoreProject project)
            => Path.Combine(project.OutputDirectory, project.Namespaces.DataLayer, project.Namespaces.Configurations);

        public static string GetDataLayerContractsDirectory(this EntityFrameworkCoreProject project)
            => Path.Combine(project.OutputDirectory, project.Namespaces.DataLayer, project.Namespaces.Contracts);

        public static string GetDataLayerDataContractsDirectory(this EntityFrameworkCoreProject project)
            => Path.Combine(project.OutputDirectory, project.Namespaces.DataLayer, project.Namespaces.DataContracts);

        public static string GetDataLayerRepositoriesDirectory(this EntityFrameworkCoreProject project)
            => Path.Combine(project.OutputDirectory, project.Namespaces.DataLayer, project.Namespaces.Repositories);

        public static PropertyDefinition GetChildNavigationProperty(this EntityFrameworkCoreProject project, ProjectSelection<EntityFrameworkCoreProjectSettings> projectSelection, ITable table, ForeignKey foreignKey)
        {
            var propertyType = string.Format("{0}<{1}>", projectSelection.Settings.NavigationPropertyEnumerableType, table.GetSingularName());

            return new PropertyDefinition(propertyType, table.GetPluralName())
            {
                IsVirtual = projectSelection.Settings.DeclareNavigationPropertiesAsVirtual
            };
        }

        public static EntityFrameworkCoreProject GlobalSelection(this EntityFrameworkCoreProject project, Action<EntityFrameworkCoreProjectSettings> action = null)
        {
            var settings = new EntityFrameworkCoreProjectSettings();

            var selection = project.Selections.FirstOrDefault(item => item.IsGlobal);

            if (selection == null)
            {
                selection = new ProjectSelection<EntityFrameworkCoreProjectSettings>
                {
                    Pattern = ProjectSelection<EntityFrameworkCoreProjectSettings>.GlobalPattern,
                    Settings = settings
                };

                project.Selections.Add(selection);
            }
            else
            {
                settings = selection.Settings;
            }

            action?.Invoke(settings);

            return project;
        }

        public static ProjectSelection<EntityFrameworkCoreProjectSettings> GlobalSelection(this EntityFrameworkCoreProject project)
            => project.Selections.FirstOrDefault(item => item.IsGlobal);

        public static EntityFrameworkCoreProject Select(this EntityFrameworkCoreProject project, string pattern, Action<EntityFrameworkCoreProjectSettings> action = null)
        {
            var selection = project.Selections.FirstOrDefault(item => item.Pattern == pattern);

            if (selection == null)
            {
                selection = new ProjectSelection<EntityFrameworkCoreProjectSettings>
                {
                    Pattern = pattern,
                    Settings = project.GlobalSelection().Settings
                };

                project.Selections.Add(selection);
            }

            action?.Invoke(selection.Settings);

            return project;
        }
    }
}
