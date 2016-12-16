﻿using System.Collections.Generic;
using CatFactory.DotNetCore;

namespace CatFactory.EfCore
{
    public static class CodeGenerationExtensions
    {
        public static EfCoreProject GenerateEntities(this EfCoreProject project)
        {
            foreach (var table in project.Database.Tables)
            {
                var codeBuilder = new CSharpClassBuilder()
                {
                    ObjectDefinition = new EntityClassDefinition(table)
                    {
                        Namespace = project.GetEntityLayerNamespace(),
                    },
                    OutputDirectory = project.OutputDirectory
                };

                if ( project.UseDataAnnotations)
                {
                    // todo: add data annotations
                }

                codeBuilder.CreateFile(project.GetEntityLayerDirectory());
            }

            foreach (var view in project.Database.Views)
            {
                var codeBuilder = new CSharpClassBuilder()
                {
                    ObjectDefinition = new EntityClassDefinition(view)
                    {
                        Namespace = project.GetEntityLayerNamespace()
                    },
                    OutputDirectory = project.OutputDirectory
                };

                codeBuilder.CreateFile(project.GetEntityLayerDirectory());
            }

            return project;
        }

        public static EfCoreProject GenerateAppSettings(this EfCoreProject project)
        {
            var codeBuilder = new CSharpClassBuilder()
            {
                ObjectDefinition = new AppSettingsClassDefinition()
                {
                    Namespace = project.GetDataLayerNamespace()
                },
                OutputDirectory = project.OutputDirectory
            };

            codeBuilder.CreateFile(project.GetDataLayerDirectory());

            return project;
        }

        public static EfCoreProject GenerateMappingDependences(this EfCoreProject project)
        {
            if (!project.UseDataAnnotations)
            {
                var codeBuilders = new List<DotNetCodeBuilder>()
                {
                    new CSharpInterfaceBuilder()
                    {
                        ObjectDefinition = new IEntityMapperInterfaceDefinition() { Namespace = project.GetDataLayerMappingNamespace() },
                        OutputDirectory = project.OutputDirectory
                    },
                    new CSharpClassBuilder()
                    {
                        ObjectDefinition = new EntityMapperClassDefinition() { Namespace = project.GetDataLayerMappingNamespace() },
                        OutputDirectory = project.OutputDirectory
                    },
                    new CSharpInterfaceBuilder()
                    {
                        ObjectDefinition = new IEntityMapInterfaceDefinition() { Namespace = project.GetDataLayerMappingNamespace() },
                        OutputDirectory = project.OutputDirectory
                    },
                    new CSharpClassBuilder()
                    {
                        ObjectDefinition = new DbMapperClassDefinition(project.Database) { Namespace = project.GetDataLayerMappingNamespace() },
                        OutputDirectory = project.OutputDirectory
                    },
                };

                foreach (var codeBuilder in codeBuilders)
                {
                    codeBuilder.CreateFile(project.GetDataLayerMappingDirectory());
                }
            }

            return project;
        }

        public static EfCoreProject GenerateMappings(this EfCoreProject project)
        {
            if (!project.UseDataAnnotations)
            {
                foreach (var table in project.Database.Tables)
                {
                    var codeBuilder = new CSharpClassBuilder()
                    {
                        ObjectDefinition = new MappingClassDefinition(table)
                        {
                            Namespace = project.GetDataLayerMappingNamespace()
                        },
                        OutputDirectory = project.OutputDirectory
                    };

                    codeBuilder.ObjectDefinition.Namespaces.Add(project.GetEntityLayerNamespace());

                    codeBuilder.CreateFile(project.GetDataLayerMappingDirectory());
                }

                foreach (var view in project.Database.Views)
                {
                    var codeBuilder = new CSharpClassBuilder()
                    {
                        ObjectDefinition = new MappingClassDefinition(view)
                        {
                            Namespace = project.GetDataLayerMappingNamespace()
                        },
                        OutputDirectory = project.OutputDirectory
                    };

                    codeBuilder.ObjectDefinition.Namespaces.Add(project.GetEntityLayerNamespace());

                    codeBuilder.CreateFile(project.GetDataLayerMappingDirectory());
                }
            }

            return project;
        }

        public static EfCoreProject GenerateDbContext(this EfCoreProject project)
        {
            var codeBuilder = new CSharpClassBuilder()
            {
                ObjectDefinition = new DbContextClassDefinition(project.Database)
                {
                    Namespace = project.GetDataLayerNamespace()
                },
                OutputDirectory = project.OutputDirectory
            };

            codeBuilder.ObjectDefinition.Namespaces.Add(project.GetDataLayerMappingNamespace());

            if (project.DeclareDbSetPropertiesInDbContext)
            {
                // todo: add code for declare DbSet properties in DbContext
            }

            codeBuilder.CreateFile(project.GetDataLayerDirectory());

            return project;
        }

        public static EfCoreProject GenerateContracts(this EfCoreProject project)
        {
            foreach (var projectFeature in project.Features)
            {
                var codeBuilder = new CSharpInterfaceBuilder
                {
                    ObjectDefinition = new RepositoryInterfaceDefinition(projectFeature)
                    {
                        Namespace = project.GetDataLayerContractsNamespace()
                    },
                    OutputDirectory = project.OutputDirectory
                };

                codeBuilder.ObjectDefinition.Namespaces.Add(project.GetEntityLayerNamespace());

                codeBuilder.CreateFile(project.GetDataLayerContractsDirectory());
            }

            return project;
        }

        public static EfCoreProject GenerateRepositories(this EfCoreProject project)
        {
            foreach (var projectFeature in project.Features)
            {
                var codeBuilder = new CSharpClassBuilder
                {
                    ObjectDefinition = new RepositoryClassDefinition(projectFeature)
                    {
                        Namespace = project.GetDataLayerRepositoriesNamespace()
                    },
                    OutputDirectory = project.OutputDirectory
                };

                codeBuilder.ObjectDefinition.Namespaces.Add(project.GetEntityLayerNamespace());
                codeBuilder.ObjectDefinition.Namespaces.Add(project.GetDataLayerContractsNamespace());

                codeBuilder.CreateFile(project.GetDataLayerRepositoriesDirectory());
            }

            return project;
        }
    }
}