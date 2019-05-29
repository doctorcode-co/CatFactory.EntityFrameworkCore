﻿using System.Collections.Generic;
using System.Linq;
using CatFactory.CodeFactory;
using CatFactory.CodeFactory.Scaffolding;
using CatFactory.Collections;
using CatFactory.NetCore;
using CatFactory.NetCore.ObjectOrientedProgramming;
using CatFactory.ObjectOrientedProgramming;
using CatFactory.ObjectRelationalMapping;

namespace CatFactory.EntityFrameworkCore.Definitions.Extensions
{
    public static class DbContextClassBuilder
    {
        public static DbContextClassDefinition GetDbContextClassDefinition(this EntityFrameworkCoreProject project, ProjectSelection<EntityFrameworkCoreProjectSettings> projectSelection)
        {
            var definition = new DbContextClassDefinition
            {
                Namespaces =
                {
                    "System",
                    "Microsoft.EntityFrameworkCore",
                    project.GetEntityLayerNamespace()
                },
                Namespace = project.GetDataLayerNamespace(),
                AccessModifier = AccessModifier.Public,
                Name = project.GetDbContextName(project.Database),
                BaseClass = "DbContext"
            };

            if (!projectSelection.Settings.UseDataAnnotations)
                definition.Namespaces.Add(project.GetDataLayerConfigurationsNamespace());

            definition.Constructors.Add(new ClassConstructorDefinition
            {
                AccessModifier = AccessModifier.Public,
                Parameters =
                {
                    new ParameterDefinition(string.Format("DbContextOptions<{0}>", definition.Name), "options")
                },
                Invocation = "base(options)"
            });

            definition.Methods.Add(GetOnModelCreatingMethod(project));

            foreach (var table in project.Database.Tables)
            {
                if (!project.Database.HasDefaultSchema(table))
                    definition.Namespaces.AddUnique(project.GetEntityLayerNamespace(table.Schema));

                var existingViews = project.Database.Views.Count(item => item.Name == table.Name);

                var genericTypeName = existingViews == 0 ? project.GetEntityName(table) : project.GetFullEntityName(table);
                var name = existingViews == 0 ? project.GetDbSetPropertyName(table) : project.GetFullDbSetPropertyName(table);

                definition.Properties.Add(
                    new PropertyDefinition
                    {
                        AccessModifier = AccessModifier.Public,
                        Type = string.Format("DbSet<{0}>", genericTypeName),
                        Name = name,
                        IsAutomatic = true
                    }
                );
            }

            foreach (var view in project.Database.Views)
            {
                if (!project.Database.HasDefaultSchema(view))
                    definition.Namespaces.AddUnique(project.GetEntityLayerNamespace(view.Schema));

                var existingTables = project.Database.Tables.Count(item => item.Name == view.Name);

                var genericTypeName = existingTables == 0 ? project.GetEntityName(view) : project.GetFullEntityName(view);
                var name = existingTables == 0 ? project.GetDbSetPropertyName(view) : project.GetFullDbSetPropertyName(view);

                definition.Properties.Add(
                    new PropertyDefinition
                    {
                        AccessModifier = AccessModifier.Public,
                        Type = string.Format("DbSet<{0}>", genericTypeName),
                        Name = name,
                        IsAutomatic = true
                    }
                );
            }

            foreach (var table in project.Database.Tables)
            {
                if (!projectSelection.Settings.UseDataAnnotations && !project.Database.HasDefaultSchema(table))
                    definition.Namespaces.AddUnique(project.GetDataLayerConfigurationsNamespace(table.Schema));
            }

            foreach (var view in project.Database.Views)
            {
                if (!projectSelection.Settings.UseDataAnnotations && !project.Database.HasDefaultSchema(view))
                    definition.Namespaces.AddUnique(project.GetDataLayerConfigurationsNamespace(view.Schema));
            }

            foreach (var scalarFunction in project.Database.ScalarFunctions)
            {
                var parameterType = string.Empty;

                if (project.Database.HasTypeMappedToClr(scalarFunction.Parameters[0]))
                {
                    var clrType = project.Database.GetClrMapForType(scalarFunction.Parameters[0]);

                    parameterType = clrType.AllowClrNullable ? string.Format("{0}?", clrType.GetClrType().Name) : clrType.GetClrType().Name;
                }
                else
                {
                    parameterType = "object";
                }

                var method = new MethodDefinition
                {
                    Attributes =
                    {
                        new MetadataAttribute("DbFunction")
                        {
                            Sets =
                            {
                                new MetadataAttributeSet("FunctionName", string.Format("\"{0}\"", scalarFunction.Name)),
                                new MetadataAttributeSet("Schema", string.Format("\"{0}\"", scalarFunction.Schema))
                            }
                        }
                    },
                    IsStatic = true,
                    Type = parameterType,
                    AccessModifier = AccessModifier.Public,
                    Name = project.GetScalarFunctionMethodName(scalarFunction),
                    Lines =
                    {
                        new CodeLine("throw new Exception();")
                    }
                };

                var parameters = scalarFunction.Parameters.Where(item => !string.IsNullOrEmpty(item.Name)).ToList();

                foreach (var parameter in parameters)
                {
                    var propertyType = project.Database.ResolveDatabaseType(parameter);

                    method.Parameters.Add(new ParameterDefinition(parameterType, project.GetPropertyName(parameter)));
                }

                definition.Methods.Add(method);
            }

            if (projectSelection.Settings.SimplifyDataTypes)
                definition.SimplifyDataTypes();

            return definition;
        }

        private static MethodDefinition GetOnModelCreatingMethod(EntityFrameworkCoreProject project)
        {
            var lines = new List<ILine>();

            var selection = project.GlobalSelection();

            if (selection.Settings.UseDataAnnotations)
            {
                var primaryKeys = project
                    .Database
                    .Tables
                    .Where(item => item.PrimaryKey != null)
                    .Select(item => item.GetColumnsFromConstraint(item.PrimaryKey).Select(key => key.Name).First())
                    .ToList();

                foreach (var view in project.Database.Views)
                {
                    var result = view.Columns.Where(item => primaryKeys.Contains(item.Name)).ToList();

                    if (result.Count == 0)
                    {
                        lines.Add(
                            new CodeLine("modelBuilder.Entity<{0}>().HasKey(e => new {{ {1} }});", project.GetEntityName(view), string.Join(", ", view.Columns.Select(item => string.Format("e.{0}", project.GetPropertyName(view, item))))));

                        lines.Add(new EmptyLine());
                    }
                    else
                    {
                        lines.Add(
                            new CodeLine("modelBuilder.Entity<{0}>().HasKey(e => new {{ {1} }});", project.GetEntityName(view), string.Join(", ", result.Select(item => string.Format("e.{0}", project.GetPropertyName(view, item))))));

                        lines.Add(new EmptyLine());
                    }
                }
            }
            else
            {
                if (project.Database.Tables.Count > 0)
                {
                    lines.Add(new CommentLine(" Apply all configurations for tables"));
                    lines.Add(new EmptyLine());

                    lines.Add(new CodeLine("modelBuilder"));

                    foreach (var table in project.Database.Tables)
                    {
                        var existingViews = project.Database.Views.Count(item => item.Name == table.Name);

                        var genericTypeName = existingViews == 0 ? project.GetEntityName(table) : project.GetFullEntityName(table);
                        var name = existingViews == 0 ? project.GetEntityConfigurationName(table) : project.GetFullEntityConfigurationName(table);

                        lines.Add(new CodeLine(1, ".ApplyConfiguration(new {0}())", name));
                    }

                    lines.Add(new CodeLine(";"));

                    lines.Add(new EmptyLine());
                }

                if (project.Database.Views.Count > 0)
                {
                    lines.Add(new CommentLine(" Apply all configurations for views"));
                    lines.Add(new EmptyLine());

                    lines.Add(new CodeLine("modelBuilder"));

                    foreach (var view in project.Database.Views)
                    {
                        var existingTables = project.Database.Tables.Count(item => item.Name == view.Name);

                        var genericTypeName = existingTables == 0 ? project.GetEntityName(view) : project.GetFullEntityName(view);
                        var name = existingTables == 0 ? project.GetEntityConfigurationName(view) : project.GetFullEntityConfigurationName(view);

                        lines.Add(new CodeLine(1, ".ApplyConfiguration(new {0}())", name));
                    }

                    lines.Add(new CodeLine(";"));
                    lines.Add(new EmptyLine());
                }

                if (project.Database.TableFunctions.Count > 0)
                {
                    lines.Add(new CommentLine(" Register query types for table functions"));
                    lines.Add(new EmptyLine());

                    foreach (var view in project.Database.TableFunctions)
                    {
                        lines.Add(new CodeLine("modelBuilder.Query<{0}>();", project.GetEntityResultName(view)));
                    }

                    lines.Add(new EmptyLine());
                }

                if (project.Database.StoredProcedures.Count > 0)
                {
                    lines.Add(new CommentLine(" Register query types for stored procedures"));
                    lines.Add(new EmptyLine());

                    foreach (var view in project.Database.StoredProcedures)
                    {
                        lines.Add(new CodeLine("modelBuilder.Query<{0}>();", project.GetEntityResultName(view)));
                    }

                    lines.Add(new EmptyLine());
                }
            }

            lines.Add(new CodeLine("base.OnModelCreating(modelBuilder);"));

            return new MethodDefinition
            {
                AccessModifier = AccessModifier.Protected,
                Type = "void",
                Name = "OnModelCreating",
                Parameters =
                {
                    new ParameterDefinition("ModelBuilder", "modelBuilder")
                },
                IsOverride = true,
                Lines = lines
            };
        }
    }
}
