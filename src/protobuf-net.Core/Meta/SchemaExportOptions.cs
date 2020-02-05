using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoBuf.Meta
{
    public sealed class ExportedSchema
    {        
        internal ExportedSchema(string protoFile, string schema )
        {
            this.ProtoFile = protoFile;
            this.Schema = schema;
        }
        
        public string ProtoFile { get; }

        public string Schema { get; }
    }

    public sealed class SchemaExportOptions
    {
        public ProtoSyntax Syntax { get; set; }

        public string DefaultProtoFile { get; set; }

        /// <summary>
        /// Default package name. If assigned and no type mapping is available, takes precedence over any other logic.
        /// </summary>
        public string DefaultPackage { get; set; }

        public ICollection<Type> Types { get; set; }

        public Func<Type, ProtoDefinition> TypeMapper { get; set; }

        public IList<ServiceDefinition> Services { get; set; }

    }

    public sealed class ProtoDefinition : IEquatable<ProtoDefinition>
    {
        public ProtoDefinition(string protoFile, string package, IEnumerable<ProtoOption> options = null)
        {
            this.ProtoFile = protoFile ?? "";
            this.Package = package ?? "";
            this.Options = options != null ? (IReadOnlyList<ProtoOption>)options.ToList() : Array.Empty<ProtoOption>();
        }

        public bool IsDefault => string.IsNullOrEmpty(this.ProtoFile);

        public string ProtoFile { get; }

        public string Package { get; }

        public IReadOnlyList<ProtoOption> Options { get; }

        public bool IsExternal { get; }

        public bool Equals(ProtoDefinition other)
        {
            return other != null
                && this.ProtoFile == other.ProtoFile
                && this.Package == other.Package
                && this.IsExternal == other.IsExternal
                && this.Options.SequenceEqual(other.Options);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as ProtoDefinition);
        }

        public override int GetHashCode()
        {
            return this.ProtoFile.GetHashCode() + this.Package.GetHashCode();
        }
    }

    public struct ProtoOption
    {
        public ProtoOption(string name, string value)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name { get; }

        public string Value { get; }
    }

    public sealed class ServiceDefinition
    {
        public ProtoDefinition Proto { get; set; }

        public string Name { get; set; }

        public IList<OperationDefinition> Operations { get; set; }

        /// <summary>
        /// Service level options.
        /// </summary>
        public IList<ProtoOption> Options { get; set; }
    }

    public sealed class OperationDefinition
    {
        public string Name { get; set; }

        public Type RequestType { get; set; }

        public bool IsRequestStream { get; set; }

        public Type ResponseType { get; set; }

        public bool IsResponseStream { get; set; }

        /// <summary>
        /// Operation level options.
        /// </summary>
        public IList<ProtoOption> Options { get; set; }
    }
}


