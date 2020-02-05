using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProtoBuf.Meta.RuntimeTypeModel;

namespace ProtoBuf.Meta
{
    internal sealed class ProtoImports
    {
        internal ProtoImports(ProtoDefinition protoDefinition, Func<Type, ProtoDefinition> typeMapper )
        {
            this.ProtoDefinition = protoDefinition;
            this.TypeMapper = typeMapper;

        }
        internal ProtoDefinition ProtoDefinition { get; }

        internal Func<Type, ProtoDefinition> TypeMapper { get; }

        internal List<ProtoDefinition> Imports { get; } = new List<ProtoDefinition>();

        internal CommonImports CommonImports { get; set; }

        internal ProtoDefinition GetImportDefinition(Type type)
        {
            var importDefinition = this.TypeMapper?.Invoke(type);
            return AddImportDefinition(importDefinition) ? importDefinition : null;
        }

        internal bool AddImportDefinition(ProtoDefinition importDefinition)
        {
            if (importDefinition != null && !importDefinition.Equals(this.ProtoDefinition))
            {
                if (!this.Imports.Contains(importDefinition))
                {
                    this.Imports.Add(importDefinition);
                }

                return true;
            }

            return false;
        }
    }

    internal sealed class ProtoBuilder
    {
        internal ProtoBuilder(ProtoDefinition protoDefinition, ProtoSyntax syntax, Func<Type, ProtoDefinition> typeMapper)
        {
            this.Syntax = syntax;
            this.ProtoDefinition = protoDefinition;

            switch (syntax)
            {
                case ProtoSyntax.Proto2:
                    this.Header.AppendLine(@"syntax = ""proto2"";");
                    break;
                case ProtoSyntax.Proto3:
                    this.Header.AppendLine(@"syntax = ""proto3"";");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(syntax));
            }

            if (!string.IsNullOrEmpty(protoDefinition.Package))
            {
                this.Header.Append("package ").Append(protoDefinition.Package).Append(';').AppendLine();
            }

            foreach( var option in protoDefinition.Options)
            {
                this.Header.Append($"option {option.Name}={option.Value};").AppendLine();
            }

            this.Imports = new ProtoImports(protoDefinition, typeMapper);
        }

        internal ProtoDefinition ProtoDefinition { get; }

        internal ProtoSyntax Syntax { get; }

        internal StringBuilder Header { get; } = new StringBuilder();

        internal StringBuilder Body { get; } = new StringBuilder();

        internal ProtoImports Imports { get; }

        internal string GetSchema()
        {
            return $"{this.Header}{this.Body}{Environment.NewLine}";
        }

        internal void ApplyImports()
        {
            var commonImports = this.Imports.CommonImports;
            var header = this.Header;

            if ((commonImports & CommonImports.Bcl) != 0)
            {
                header.Append("import \"protobuf-net/bcl.proto\"; // schema for protobuf-net's handling of core .NET types").AppendLine();
            }
            if ((commonImports & CommonImports.Protogen) != 0)
            {
                header.Append("import \"protobuf-net/protogen.proto\"; // custom protobuf-net options").AppendLine();
            }
            if ((commonImports & CommonImports.Timestamp) != 0)
            {
                header.Append("import \"google/protobuf/timestamp.proto\";").AppendLine();
            }
            if ((commonImports & CommonImports.Duration) != 0)
            {
                header.Append("import \"google/protobuf/duration.proto\";").AppendLine();
            }

            foreach( var import in this.Imports.Imports)
            {
                header.Append($"import \"{import.ProtoFile}\";").AppendLine();
            }
        }
    }
}
