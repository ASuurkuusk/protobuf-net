using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Examples
{
    public class SchemasGeneration
    {
        [Fact]
        public void GenerateNamespacePackages()
        {
            var exportOptions = new SchemaExportOptions()
            {
                Syntax = ProtoSyntax.Proto3,
                TypeMapper = NamespaceTypeMapper.Default
            };

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(Other1.Test));
            model.Add(typeof(Other2.Test));

            var schemas = model.GetSchemas(exportOptions);

            Assert.Equal(3, schemas.Count);

            Assert.Equal(
@"syntax = ""proto3"";
package examples.other1;
option csharp_namespace=""Examples.Other1"";
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message Test {
   int32 C = 1;
   int32 D = 2;
   .bcl.DateTime Time = 3;
}
",
                schemas.First(s=>s.ProtoFile == "examples_other1.proto" ).Schema );

            Assert.Equal(
@"syntax = ""proto3"";
package examples.other2;
option csharp_namespace=""Examples.Other2"";
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""examples_other1.proto"";

message Test {
   int32 E = 1;
   int32 F = 2;
   .examples.other1.Test OtherTest = 3;
   .bcl.Guid Id = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
}
",
                schemas.First(s => s.ProtoFile == "examples_other2.proto").Schema);

            Assert.Equal(
@"syntax = ""proto3"";
package examples.common;
option csharp_namespace=""Examples.Common"";
import ""examples_other2.proto"";

message Test {
   int32 A = 1;
   int32 B = 2;
   oneof subtype {
      .examples.other2.Test examples_other2_Test = 11;
   }
}
",
                schemas.First(s => s.ProtoFile == "examples_common.proto").Schema);
        }

        [Fact]
        public void GenerateSingleInbuiltType()
        {
            var exportOptions = new SchemaExportOptions()
            {
                Syntax = ProtoSyntax.Proto3,
                Types = new Type[] { typeof(double) },
                TypeMapper = NamespaceTypeMapper.Default
            };

            var model = RuntimeTypeModel.Create();

            var schemas = model.GetSchemas(exportOptions);

            Assert.Equal(1, schemas.Count);

            Assert.Equal(
@"syntax = ""proto3"";

message Double {
   double value = 1;
}
",
            schemas[0].Schema);
        }


        [Fact]
        public void GenerateMultiInbuiltTypes()
        {
            var exportOptions = new SchemaExportOptions()
            {
                Syntax = ProtoSyntax.Proto3,
                Types = new Type[] { typeof(double), typeof(int) },
                TypeMapper = NamespaceTypeMapper.Default
            };

            var model = RuntimeTypeModel.Create();

            var schemas = model.GetSchemas(exportOptions);

            Assert.Equal(1, schemas.Count);

            Assert.Equal(
@"syntax = ""proto3"";

message Double {
   double value = 1;
}
message Int32 {
   int32 value = 1;
}
",
            schemas[0].Schema);
        }

        [Fact]
        public void GenerateMixedTypes()
        {
            var exportOptions = new SchemaExportOptions()
            {
                Syntax = ProtoSyntax.Proto3,
                Types = new Type[] { typeof(double), typeof(ExtendedService.HelloRequest), typeof(int) },
            };

            var model = RuntimeTypeModel.Create();

            var schemas = model.GetSchemas(exportOptions);

            Assert.Equal(1, schemas.Count);

            Assert.Equal(
@"syntax = ""proto3"";
package Examples.ExtendedService;

message Double {
   double value = 1;
}
message Int32 {
   int32 value = 1;
}
message HelloRequest {
   string FirstName = 1;
   string LastName = 2;
}
",
            schemas[0].Schema);
        }


        [Fact]
        public void GenerateServicesInDefaultPackage()
        {
            var exportOptions = new SchemaExportOptions()
            {
                Syntax = ProtoSyntax.Proto3,
                TypeMapper = NamespaceTypeMapper.Default,
                Services = new ServiceDefinition[]
                {
                    new ServiceDefinition
                    {
                        Name="GreetingService",
                        Operations = new OperationDefinition[]
                        {
                            new OperationDefinition
                            {
                                Name = "SayHello",
                                RequestType = typeof(SimpleService.HelloRequest),
                                ResponseType = typeof(SimpleService.HelloResponse),
                            }
                        }
                    },
                    new ServiceDefinition
                    {
                        Name="GreetingServiceEx",
                        Operations = new OperationDefinition[]
                        {
                            new OperationDefinition
                            {
                                Name = "SayHello",
                                RequestType = typeof(ExtendedService.HelloRequest),
                                ResponseType = typeof(ExtendedService.HelloResponse),
                            }
                        }
                    }
                }
            };

            var model = RuntimeTypeModel.Create();
            var schemas = model.GetSchemas(exportOptions);

            Assert.Equal(
@"syntax = ""proto3"";
import ""examples_simpleservice.proto"";
import ""examples_extendedservice.proto"";

service GreetingService {
   rpc SayHello (.examples.simpleservice.HelloRequest) returns (.examples.simpleservice.HelloResponse);
}
service GreetingServiceEx {
   rpc SayHello (.examples.extendedservice.HelloRequest) returns (.examples.extendedservice.HelloResponse);
}
",
                schemas.First(s => s.ProtoFile == "").Schema);

            Assert.Equal(
@"syntax = ""proto3"";
package examples.simpleservice;
option csharp_namespace=""Examples.SimpleService"";

message HelloRequest {
   string Name = 1;
}
message HelloResponse {
   string Greeting = 1;
}
",
                schemas.First(s => s.ProtoFile == "examples_simpleservice.proto").Schema);

            Assert.Equal(
@"syntax = ""proto3"";
package examples.extendedservice;
option csharp_namespace=""Examples.ExtendedService"";

message HelloRequest {
   string FirstName = 1;
   string LastName = 2;
}
message HelloResponse {
   string Greeting = 1;
}
",
                schemas.First(s => s.ProtoFile == "examples_extendedservice.proto").Schema);
        }

        [Fact]
        public void GenerateServicesInOtherPackage()
        {
            ProtoDefinition servicesProto = new ProtoDefinition("services.proto", "services");

            var exportOptions = new SchemaExportOptions()
            {
                Syntax = ProtoSyntax.Proto3,
                TypeMapper = NamespaceTypeMapper.Default,
                Services = new ServiceDefinition[]
                {
                    new ServiceDefinition
                    {
                        Proto = servicesProto,
                        Name="GreetingService",
                        Operations = new OperationDefinition[]
                        {
                            new OperationDefinition
                            {
                                Name = "SayHello",
                                RequestType = typeof(SimpleService.HelloRequest),
                                ResponseType = typeof(SimpleService.HelloResponse),
                            }
                        }
                    },
                    new ServiceDefinition
                    {
                        Proto = servicesProto,
                        Name="GreetingServiceEx",
                        Operations = new OperationDefinition[]
                        {
                            new OperationDefinition
                            {
                                Name = "SayHello",
                                RequestType = typeof(ExtendedService.HelloRequest),
                                ResponseType = typeof(ExtendedService.HelloResponse),
                            }
                        }
                    }
                }
            };

            var model = RuntimeTypeModel.Create();
            var schemas = model.GetSchemas(exportOptions);

            Assert.Equal(3, schemas.Count);
            Assert.Equal(
@"syntax = ""proto3"";
package services;
import ""examples_simpleservice.proto"";
import ""examples_extendedservice.proto"";

service GreetingService {
   rpc SayHello (.examples.simpleservice.HelloRequest) returns (.examples.simpleservice.HelloResponse);
}
service GreetingServiceEx {
   rpc SayHello (.examples.extendedservice.HelloRequest) returns (.examples.extendedservice.HelloResponse);
}
",
                schemas.First(s => s.ProtoFile == "services.proto").Schema);

            Assert.Equal(
@"syntax = ""proto3"";
package examples.simpleservice;
option csharp_namespace=""Examples.SimpleService"";

message HelloRequest {
   string Name = 1;
}
message HelloResponse {
   string Greeting = 1;
}
",
                schemas.First(s => s.ProtoFile == "examples_simpleservice.proto").Schema);

            Assert.Equal(
@"syntax = ""proto3"";
package examples.extendedservice;
option csharp_namespace=""Examples.ExtendedService"";

message HelloRequest {
   string FirstName = 1;
   string LastName = 2;
}
message HelloResponse {
   string Greeting = 1;
}
",
                schemas.First(s => s.ProtoFile == "examples_extendedservice.proto").Schema);
        }

        private static void WriteSchema(IList<ExportedSchema> schemas, string directory )
        {
            foreach( var exportedSchema in schemas)
            {
                var protoFile = !string.IsNullOrEmpty(exportedSchema.ProtoFile) ? exportedSchema.ProtoFile : "default.proto";
                File.WriteAllText(Path.Combine(directory, protoFile), exportedSchema.Schema);
            }
        }

    }

    public static class NamespaceTypeMapper
    {
        public static readonly Func<Type, ProtoDefinition> Default = (type) =>
        {
            string ns = type.Namespace;
            string package = ns.ToLowerInvariant();

            // TODO: Handle proto-files directory structure (i.e. use '/' instead of '_').
            string protoFile = package.Replace('.', '_') + ".proto";

            return new ProtoDefinition(protoFile, package, new ProtoOption[] { new ProtoOption("csharp_namespace", $"\"{ns}\"" ) });
        };
    }

    namespace Common
    {
        [ProtoContract]
        //[ProtoInclude(10, typeof(Other1.Test))]
        [ProtoInclude(11, typeof(Other2.Test))]
        public class Test
        {
            [ProtoMember(1)]
            public int A { get; set; }

            [ProtoMember(2)]
            public int B { get; set; }
        }
    }

    namespace Other1
    {
        [ProtoContract]
        public class Test 
        {
            [ProtoMember(1)]
            public int C { get; set; }

            [ProtoMember(2)]
            public int D { get; set; }

            [ProtoMember(3)]
            public DateTime Time { get; set; }

        }
    }

    namespace Other2
    {
        [ProtoContract]
        public class Test : Common.Test
        {
            [ProtoMember(1)]
            public int E { get; set; }

            [ProtoMember(2)]
            public int F { get; set; }

            [ProtoMember(3)]
            public Other1.Test OtherTest { get; set; }

            [ProtoMember(4)]
            public Guid Id { get; set; }
        }
    }

    namespace SimpleService
    {
        [ProtoContract]
        public class HelloRequest
        {
            [ProtoMember(1)]
            public string Name { get; set; }
        }

        [ProtoContract]
        public class HelloResponse
        {
            [ProtoMember(1)]
            public string Greeting { get; set;  }
        }
    }

    namespace ExtendedService
    {
        [ProtoContract]
        public class HelloRequest
        {
            [ProtoMember(1)]
            public string FirstName { get; set; }

            [ProtoMember(2)]
            public string LastName { get; set; }
        }

        [ProtoContract]
        public class HelloResponse
        {
            [ProtoMember(1)]
            public string Greeting { get; set; }
        }
    }


}
