﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Schema;
using XRoadLib.Attributes;
using XRoadLib.Extensions;
using XRoadLib.Header;
using XRoadLib.Serialization;

namespace XRoadLib.Description
{
    public sealed class ProducerDefinition
    {
        private const string STANDARD_HEADER_NAME = "stdhdr";

        private readonly XmlDocument document = new XmlDocument();

        private readonly XRoadProtocol protocol;
        private readonly string producerName;
        private readonly string environmentProducerName;
        private readonly string xroadNamespace;
        private readonly string targetNamespace;
        private readonly uint version;

        private readonly Binding binding;
        private readonly Port servicePort;
        private readonly PortType portType;

        private readonly ICollection<string> requiredHeaders = new SortedSet<string>();

        private readonly IList<Message> messages = new List<Message>();
        private readonly IList<XmlSchemaImport> schemaImports = new List<XmlSchemaImport>();
        private readonly IDictionary<string, XmlSchemaElement> schemaElements = new SortedDictionary<string, XmlSchemaElement>();
        private readonly IDictionary<string, Tuple<Type, XmlSchemaComplexType>> schemaTypes = new SortedDictionary<string, Tuple<Type, XmlSchemaComplexType>>();
        private readonly IDictionary<string, Tuple<MethodInfo, XmlSchemaComplexType, XmlSchemaComplexType>> operationTypes = new SortedDictionary<string, Tuple<MethodInfo, XmlSchemaComplexType, XmlSchemaComplexType>>();

        public string HeaderMessage { private get; set; }
        public string ImportedSchemaPath { private get; set; }
        public string Location { private get; set; }
        public IDictionary<string, string> Title { get; }

        public ProducerDefinition(XRoadProtocol protocol, string producerName, uint version, string environmentProducerName = null)
        {
            this.environmentProducerName = environmentProducerName ?? producerName;
            this.producerName = producerName;
            this.protocol = protocol;
            this.version = version;

            xroadNamespace = NamespaceHelper.GetXRoadNamespace(protocol);
            targetNamespace = NamespaceHelper.GetProducerNamespace(producerName, protocol);

            Title = new SortedDictionary<string, string>();

            portType = new PortType { Name = $"{producerName}PortType"};

            binding = new Binding
            {
                Name = $"{producerName}Binding",
                Type = new XmlQualifiedName(portType.Name, targetNamespace)
            };

            servicePort = new Port
            {
                Name = $"{producerName}Port",
                Binding = new XmlQualifiedName(binding.Name, targetNamespace),
                Extensions = { CreateAddressBindingElement() }
            };
        }

        public void SaveTo(Stream stream)
        {
            var startTime = DateTime.Now;
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = "\r\n" }))
            {
                writer.WriteStartDocument();

                writer.WriteComment($" WSDL document generated by {GetType().FullName} ");
                writer.WriteComment($" WSDL document generated at {startTime:dd.MM.yyyy HH:mm:ss} ");
                writer.WriteComment($" {HeaderMessage} ");

                WriteServiceDescription(writer);
                writer.Flush();

                writer.WriteComment($" WSDL document generated in {(DateTime.Now - startTime).TotalMilliseconds} ms. ");

                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        private ProducerDefinition AddRequiredHeader<THeader, T>(Expression<Func<THeader, T>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException($"MemberExpression expected, but was {expression.Body.GetType().Name} ({GetType().Name}).");

            if (memberExpression.Expression != expression.Parameters[0])
                throw new ArgumentException($"Only parameter members should be used in mapping definition ({GetType().Name}).");

            var elementName = memberExpression.Member.GetElementName();
            if (!string.IsNullOrWhiteSpace(elementName))
                requiredHeaders.Add(elementName);

            return this;
        }

        private void WriteServiceDescription(XmlWriter writer)
        {
            var serviceDescription = new ServiceDescription { TargetNamespace = targetNamespace };
            AddServiceDescriptionNamespaces(serviceDescription);

            var schema = new XmlSchema { TargetNamespace = targetNamespace };
            CreateXmlSchemaImports(schema);

            foreach (var schemaType in schemaTypes)
                schema.Items.Add(schemaType.Value.Item2);

            AddOperationTypes(schema);
            AddSchemaElements(schema);

            serviceDescription.Types.Schemas.Add(schema);

            serviceDescription.PortTypes.Add(portType);

            AddSoapBinding();
            serviceDescription.Bindings.Add(binding);

            var standardHeader = new Message { Name = STANDARD_HEADER_NAME };

            foreach (var requiredHeader in requiredHeaders)
                standardHeader.Parts.Add(new MessagePart { Name = requiredHeader, Element = new XmlQualifiedName(requiredHeader, xroadNamespace) });

            serviceDescription.Messages.Add(standardHeader);

            foreach (var message in messages)
                serviceDescription.Messages.Add(message);

            foreach (var title in Title)
                servicePort.Extensions.Add(CreateXRoadTitleElement(title.Key, title.Value));

            servicePort.Extensions.Add(new SoapAddressBinding { Location = Location });

            serviceDescription.Services.Add(new Service
            {
                Name = $"{producerName}Service",
                Ports = { servicePort }
            });

            serviceDescription.Write(writer);
        }

        public void AddTypes<T>(Assembly contractAssembly, Func<PropertyInfo, T> orderBySelector)
        {
            foreach (var type in contractAssembly.GetTypes().Where(type => type.IsXRoadSerializable() && type.ExistsInVersion(version)))
            {
                if (IsExistingType(type.Name))
                    throw new Exception($"Multiple type definitions for same name `{type.Name}`.");

                var schemaType = new XmlSchemaComplexType { Name = type.Name, IsAbstract = type.IsAbstract };
                schemaTypes.Add(type.Name, Tuple.Create(type, schemaType));
            }

            foreach (var value in schemaTypes.Values)
            {
                var type = value.Item1;
                var schemaType = value.Item2;

                var properties = type.GetPropertiesSorted(version, orderBySelector);

                var sequence = new XmlSchemaSequence();
                foreach (var property in properties)
                {
                    var propertyElement = new XmlSchemaElement { Name = property.GetPropertyName(), Annotation = CreateAnnotationElement(property) };
                    AddSchemaType(propertyElement, property.PropertyType, true, property.GetElementType());
                    sequence.Items.Add(propertyElement);
                }

                if (type.BaseType != typeof(XRoadSerializable))
                {
                    var extension = new XmlSchemaComplexContentExtension { BaseTypeName = GetComplexTypeName(type.BaseType), Particle = sequence };
                    var complexContent = new XmlSchemaComplexContent { Content = extension };
                    schemaType.ContentModel = complexContent;
                }
                else schemaType.Particle = sequence;
            }
        }

        public void AddServiceContracts(IDictionary<MethodInfo, IDictionary<string, XRoadServiceAttribute>> serviceContracts, bool useGlobalServiceVersion = false)
        {
            AddMessageTypes(
                serviceContracts.Select(kvp => Tuple.Create(kvp.Key,
                    kvp.Value
                        .Where(v => !v.Value.IsHidden && (!useGlobalServiceVersion || v.Value.IsDefinedInVersion(version)))
                        .ToDictionary(y => y.Key, y => y.Value)))
                    .Where(x => x.Item2.Any())
                    .ToDictionary(x => x.Item1, x => x.Item2.Keys.ToList()));
        }

        public void AddOperation(string name, MethodInfo methodContract, MethodInfo methodImpl, uint? serviceVersion, bool isExported)
        {
            BuildOperationElements(name, methodContract, methodImpl, isExported);

            if (isExported)
                return;

            var operationBinding = new OperationBinding
            {
                Name = name,
                Extensions = { CreateXRoadVersionBindingElement(serviceVersion.GetValueOrDefault(version)) },
                Input = new InputBinding(),
                Output = new OutputBinding()
            };

            BuildOperationBinding(operationBinding, methodContract);

            binding.Operations.Add(operationBinding);
        }

        private void AddSchemaType(XmlSchemaElement schemaElement, Type runtimeType, bool isOptional, string elementDataType)
        {
            var element = schemaElement;
            var type = runtimeType;

            if (isOptional)
                element.MinOccurs = 0;

            while (type.IsArray)
            {
                element.IsNillable = true;

                if (type.GetArrayRank() > 1)
                    throw new NotImplementedException("Multi-dimensional arrays are not supported for DTO types.");

                var itemElement = new XmlSchemaElement { Name = "item", MinOccurs = 0, MaxOccursString = "unbounded" };

                type = type.GetElementType();

                CreateArrayDefinition(element, itemElement, type);

                element = itemElement;
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
            {
                type = nullableType;
                element.IsNillable = true;
            }

            if (type.IsClass)
                element.IsNillable = true;

            if (type == typeof(Stream))
                AddBinaryAttribute(element);

            if (!string.IsNullOrWhiteSpace(elementDataType))
            {
                element.SchemaTypeName = new XmlQualifiedName(elementDataType.Equals("base64") ? "base64Binary" : elementDataType, NamespaceHelper.XSD);
                return;
            }

            var qualifiedName = GetSimpleTypeName(type);
            if (qualifiedName != null)
            {
                element.SchemaTypeName = qualifiedName;
                return;
            }

            element.IsNillable = true;
            element.SchemaTypeName = GetComplexTypeName(type);
        }

        private XmlQualifiedName GetSimpleTypeName(Type type)
        {
            switch (type.FullName)
            {
                case "System.Boolean":
                    return new XmlQualifiedName("boolean", NamespaceHelper.XSD);
                case "System.DateTime":
                    return new XmlQualifiedName("dateTime", NamespaceHelper.XSD);
                case "System.Decimal":
                    return new XmlQualifiedName("decimal", NamespaceHelper.XSD);
                case "System.Int32":
                    return new XmlQualifiedName("int", NamespaceHelper.XSD);
                case "System.Int64":
                    return new XmlQualifiedName("long", NamespaceHelper.XSD);
                case "System.String":
                    return new XmlQualifiedName("string", NamespaceHelper.XSD);
                case "System.IO.Stream":
                    return new XmlQualifiedName("base64Binary", GetBinaryNamespace());
                default:
                    return null;
            }
        }

        private XmlQualifiedName GetComplexTypeName(Type type)
        {
            Tuple<Type, XmlSchemaComplexType> value;
            if (!schemaTypes.TryGetValue(type.Name, out value) || value.Item1 != type)
                throw new Exception($"Unrecognized type `{type.FullName}`");

            return new XmlQualifiedName(type.Name, targetNamespace);
        }

        private void BuildOperationElements(string name, MethodInfo methodContract, MethodInfo methodImpl, bool isExported)
        {
            if (methodContract.IsImportedOperation())
            {
                BuildImportedOperationElements(name, methodContract);
                return;
            }

            var operationTypeName = GetOperationTypeName(name, methodContract, methodImpl);

            var requestElement = new XmlSchemaElement { Name = name, SchemaTypeName = operationTypeName.Item1 };
            var responseElement = new XmlSchemaElement { Name = $"{name}Response", SchemaTypeName = operationTypeName.Item2 };

            schemaElements.Add(requestElement.Name, requestElement);
            schemaElements.Add(responseElement.Name, responseElement);

            var inputMessage = CreateOperationMessage(requestElement, methodContract);
            var outputMessage = CreateOperationMessage(responseElement, methodContract, inputMessage);

            if (!isExported)
            {
                var operation = new Operation { Name = name, DocumentationElement = CreateDocumentationElement(methodContract) };

                operation.Messages.Add(new OperationInput { Message = new XmlQualifiedName(inputMessage.Name, targetNamespace) });
                operation.Messages.Add(new OperationOutput { Message = new XmlQualifiedName(outputMessage.Name, targetNamespace) });

                portType.Operations.Add(operation);
            }

            messages.Add(inputMessage);
            messages.Add(outputMessage);
        }

        private static string GetOperationNameFromMethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo.DeclaringType == null)
                throw new ArgumentException("Declaring type is missing.", nameof(methodInfo));

            if (methodInfo.DeclaringType.Name.StartsWith("I") && methodInfo.DeclaringType.Name.Length > 1 && char.IsUpper(methodInfo.DeclaringType.Name[1]))
                return methodInfo.DeclaringType.Name.Substring(1);

            return methodInfo.DeclaringType.Name;
        }

        private void AddMessageType(string operationName, MethodInfo method)
        {
            if (method.IsImportedOperation())
            {
                schemaImports.Add(new XmlSchemaImport
                {
                    SchemaLocation = GetImportedSchemaPath(operationName),
                    Namespace = GetImportNamespace(operationName)
                });
                return;
            }

            var requestName = operationName;
            var responseName = $"{requestName}Response";

            if (IsExistingType(requestName) || IsExistingType(responseName))
                throw new Exception($"Operation type `{requestName}` already exists with the same name.");

            var requestSequence = CreateOperationRequestSequence(method);
            var requestType = new XmlSchemaComplexType { Name = requestName, Particle = requestSequence };

            var responseType = CreateResponseType(responseName, method, requestSequence);

            operationTypes.Add(requestName, Tuple.Create(method, requestType, responseType));
        }

        private bool IsExistingType(string typeName)
        {
            return schemaTypes.ContainsKey(typeName) || operationTypes.ContainsKey(typeName);
        }

        private XmlAttribute CreateAttribute(string prefix, string name, string @namespace, string value)
        {
            var attribute = document.CreateAttribute(prefix, name, @namespace);
            attribute.Value = value;
            return attribute;
        }

        private XmlElement CreateElement(string prefix, string name, string @namespace)
        {
            return document.CreateElement(prefix, name, @namespace);
        }

        private string GetImportNamespace(string operationName)
        {
            return $"{targetNamespace.TrimEnd('/')}/{operationName}";
        }

        #region Contract definitions that depend on X-Road protocol version

        private string GetImportedSchemaPath(string operationName)
        {
            return $"{(ImportedSchemaPath ?? "").TrimEnd('/')}/{(protocol == XRoadProtocol.Version20 ? "2.0" : "3.1")}/{operationName}.xsd";
        }

        private void BuildImportedOperationElements(string name, MethodInfo methodContract)
        {
            var importNamespace = GetImportNamespace(name);
            var operationTypeName = methodContract.GetImportedOperationTypeNames(importNamespace);
            var extraParts = methodContract.GetExtraMessageParts().ToList();

            var inputMessage = new Message { Name = name };

            inputMessage.Parts.Add(
                protocol == XRoadProtocol.Version20
                    ? new MessagePart { Name = "keha", Type = operationTypeName.Item1 }
                    : new MessagePart { Name = "body", Element = operationTypeName.Item1 });

            var outputMessage = new Message { Name = $"{name}Response"};

            if (protocol == XRoadProtocol.Version20)
                outputMessage.Parts.Add(new MessagePart { Name = "paring", Type = operationTypeName.Item1 });

            outputMessage.Parts.Add(
                protocol == XRoadProtocol.Version20
                    ? new MessagePart { Name = "keha", Type = operationTypeName.Item2 }
                    : new MessagePart { Name = "body", Element = operationTypeName.Item2 });

            if (protocol == XRoadProtocol.Version20)
                foreach (var part in extraParts)
                {
                    var message = part.Direction == MessagePartDirection.Input ? inputMessage : outputMessage;
                    message.Parts.Add(new MessagePart { Name = part.PartName, Type = new XmlQualifiedName(part.TypeName, importNamespace) });
                }

            var operation = new Operation { Name = name, DocumentationElement = CreateDocumentationElement(methodContract) };

            operation.Messages.Add(new OperationInput { Message = new XmlQualifiedName(inputMessage.Name, targetNamespace) });
            operation.Messages.Add(new OperationOutput { Message = new XmlQualifiedName(outputMessage.Name, targetNamespace) });

            portType.Operations.Add(operation);

            messages.Add(inputMessage);
            messages.Add(outputMessage);
        }

        private void CreateArrayDefinition(XmlSchemaElement element, XmlSchemaObject itemElement, Type type)
        {
            if (protocol == XRoadProtocol.Version31)
            {
                element.SchemaType = new XmlSchemaComplexType { Particle = new XmlSchemaSequence { Items = { itemElement } } };
                return;
            }

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                type = nullableType;

            var qualifiedTypeName = GetSimpleTypeName(type) ?? GetComplexTypeName(type);

            var restriction = new XmlSchemaComplexContentRestriction
            {
                BaseTypeName = new XmlQualifiedName("Array", NamespaceHelper.SOAP_ENC),
                Particle = new XmlSchemaSequence { Items = { itemElement } },
                Attributes =
                {
                    new XmlSchemaAttribute
                    {
                        RefName = new XmlQualifiedName("arrayType", NamespaceHelper.SOAP_ENC),
                        UnhandledAttributes = new[]
                        {
                            CreateAttribute(PrefixHelper.WSDL, "arrayType", NamespaceHelper.WSDL, $"{qualifiedTypeName.Namespace}:{qualifiedTypeName.Name}[]")
                        }
                    }
                }
            };

            element.SchemaType = new XmlSchemaComplexType { ContentModel = new XmlSchemaComplexContent { Content = restriction } };
        }

        private void CreateXmlSchemaImports(XmlSchema schema)
        {
            schema.Includes.Add(new XmlSchemaImport { Namespace = xroadNamespace, SchemaLocation = xroadNamespace });

            schema.Includes.Add(
                protocol == XRoadProtocol.Version20
                    ? new XmlSchemaImport { Namespace = NamespaceHelper.SOAP_ENC, SchemaLocation = NamespaceHelper.SOAP_ENC }
                    : new XmlSchemaImport { Namespace = NamespaceHelper.XMIME, SchemaLocation = NamespaceHelper.XMIME });

            foreach (var import in schemaImports)
                schema.Includes.Add(import);
        }

        private void AddServiceDescriptionNamespaces(DocumentableItem serviceDescription)
        {
            serviceDescription.Namespaces.Add(PrefixHelper.MIME, NamespaceHelper.MIME);
            serviceDescription.Namespaces.Add(PrefixHelper.SOAP, NamespaceHelper.SOAP);
            serviceDescription.Namespaces.Add(PrefixHelper.SOAP_ENV, NamespaceHelper.SOAP_ENV);
            serviceDescription.Namespaces.Add(PrefixHelper.WSDL, NamespaceHelper.WSDL);
            serviceDescription.Namespaces.Add(PrefixHelper.XMIME, NamespaceHelper.XMIME);
            serviceDescription.Namespaces.Add(PrefixHelper.GetXRoadPrefix(protocol), xroadNamespace);
            serviceDescription.Namespaces.Add(PrefixHelper.XSD, NamespaceHelper.XSD);
            serviceDescription.Namespaces.Add("", targetNamespace);

            if (protocol == XRoadProtocol.Version20)
                serviceDescription.Namespaces.Add(PrefixHelper.SOAP_ENC, NamespaceHelper.SOAP_ENC);
        }

        private void AddSoapBinding()
        {
            binding.Extensions.Add(new SoapBinding
            {
                Style = protocol == XRoadProtocol.Version20 ? SoapBindingStyle.Rpc : SoapBindingStyle.Document,
                Transport = NamespaceHelper.HTTP
            });
        }

        private void AddSoapOperationBinding(DocumentableItem operationBinding)
        {
            operationBinding.Extensions.Add(new SoapOperationBinding
            {
                Style = protocol == XRoadProtocol.Version20 ? SoapBindingStyle.Rpc : SoapBindingStyle.Document,
                SoapAction = ""
            });
        }

        private SoapBodyBinding CreateSoapBodyBinding()
        {
            return protocol == XRoadProtocol.Version20
                ? new SoapBodyBinding { Use = SoapBindingUse.Encoded, Namespace = targetNamespace, Encoding = NamespaceHelper.SOAP_ENC }
                : new SoapBodyBinding { Use = SoapBindingUse.Literal };
        }

        private SoapHeaderBinding CreateSoapHeaderBinding(string headerName)
        {
            return new SoapHeaderBinding
            {
                Message = new XmlQualifiedName(STANDARD_HEADER_NAME, targetNamespace),
                Part = headerName,
                Use = (protocol == XRoadProtocol.Version20 ? SoapBindingUse.Encoded : SoapBindingUse.Literal),
                Namespace = (protocol == XRoadProtocol.Version20 ? xroadNamespace : null),
                Encoding = (protocol == XRoadProtocol.Version20 ? NamespaceHelper.SOAP_ENC : null)
            };
        }

        private MimePart BuildMultipartOperationBinding()
        {
            var messagePart = new MimePart { Extensions = { CreateSoapBodyBinding() } };

            foreach (var headerBinding in requiredHeaders.Select(CreateSoapHeaderBinding))
            {
                var element = CreateElement(PrefixHelper.SOAP, "header", NamespaceHelper.SOAP);

                element.SetAttribute("message", STANDARD_HEADER_NAME);
                element.SetAttribute("part", headerBinding.Part);
                element.SetAttribute("use", headerBinding.Use == SoapBindingUse.Encoded ? "encoded" : "literal");

                if (headerBinding.Namespace != null)
                    element.SetAttribute("namespace", headerBinding.Namespace);

                if (headerBinding.Encoding != null)
                    element.SetAttribute("encodingStyle", headerBinding.Encoding);

                messagePart.Extensions.Add(element);
            }

            return messagePart;
        }

        private void BuildOperationBinding(OperationBinding operationBinding, MethodInfo methodContract)
        {
            AddSoapOperationBinding(operationBinding);

            if (protocol == XRoadProtocol.Version20 && methodContract.HasMultipartRequest())
            {
                operationBinding.Input.Extensions.Add(
                    new MimeMultipartRelatedBinding
                    {
                        Parts =
                        {
                            BuildMultipartOperationBinding(),
                            new MimePart { Extensions = { new MimeContentBinding { Part = "p1", Type = "application/binary" } } }
                        }
                    });
            }
            else
            {
                operationBinding.Input.Extensions.Add(CreateSoapBodyBinding());
                foreach (var headerBinding in requiredHeaders.Select(CreateSoapHeaderBinding))
                    operationBinding.Input.Extensions.Add(headerBinding);
            }

            if (protocol == XRoadProtocol.Version20 && methodContract.HasMultipartResponse())
            {
                operationBinding.Output.Extensions.Add(
                    new MimeMultipartRelatedBinding
                    {
                        Parts =
                        {
                            BuildMultipartOperationBinding(),
                            new MimePart { Extensions = { new MimeContentBinding { Part = "p2", Type = "application/binary" } } }
                        }
                    });
            }
            else
            {
                operationBinding.Output.Extensions.Add(CreateSoapBodyBinding());
                foreach (var headerBinding in requiredHeaders.Select(CreateSoapHeaderBinding))
                    operationBinding.Output.Extensions.Add(headerBinding);
            }
        }

        private void AddBinaryAttribute(XmlSchemaAnnotated element)
        {
            if (protocol != XRoadProtocol.Version20)
                element.UnhandledAttributes = new[] { CreateAttribute(PrefixHelper.XMIME, "expectedContentTypes", NamespaceHelper.XMIME, "application/octet-stream") };
        }

        private string GetBinaryNamespace()
        {
            return protocol == XRoadProtocol.Version20 ? NamespaceHelper.SOAP_ENC : NamespaceHelper.XSD;
        }

        public void AddMessageTypes(IDictionary<MethodInfo, List<string>> contractMessages)
        {
            Func<KeyValuePair<MethodInfo, List<string>>, IEnumerable<Tuple<string, MethodInfo>>> selector =
                m => protocol == XRoadProtocol.Version20
                    ? m.Value.Select(n => Tuple.Create(n, m.Key))
                    : Enumerable.Repeat(Tuple.Create(GetOperationNameFromMethodInfo(m.Key), m.Key), 1);

            foreach (var operation in contractMessages.SelectMany(selector))
                AddMessageType(operation.Item1, operation.Item2);
        }

        public ProducerDefinition AddEncodedHeader<T>(Expression<Func<IXRoadEncodedHeader, T>> expression)
        {
            return protocol == XRoadProtocol.Version20 ? AddRequiredHeader(expression) : this;
        }

        public ProducerDefinition AddLiteralHeader<T>(Expression<Func<IXRoadLiteralHeader, T>> expression)
        {
            return protocol == XRoadProtocol.Version20 ? this : AddRequiredHeader(expression);
        }

        private Tuple<XmlQualifiedName, XmlQualifiedName> GetOperationTypeName(string operationName, MethodInfo methodContract, MethodInfo methodImpl)
        {
            var name = protocol == XRoadProtocol.Version20 ? operationName : GetOperationNameFromMethodInfo(methodContract);

            Tuple<MethodInfo, XmlSchemaComplexType, XmlSchemaComplexType> value;
            if (!operationTypes.TryGetValue(name, out value) || methodContract != value.Item1)
                throw new Exception($"Unrecognized type `{name}`");

            if (protocol == XRoadProtocol.Version20)
            {
                var requestType = value.Item2;
                var sequence = (XmlSchemaSequence)requestType.Particle;

                var parameterExists = methodContract.GetParameters()
                                                    .Select(x => x.IsParameterInVersion(version))
                                                    .ToList();

                var parameterNames = methodImpl.GetParameters()
                                               .Select(x => x.Name)
                                               .Zip(parameterExists, Tuple.Create)
                                               .Where(x => x.Item2)
                                               .Select(x => x.Item1)
                                               .ToList();

                for (var i = 0; i < parameterNames.Count; i++)
                    ((XmlSchemaElement)sequence.Items[i]).Name = parameterNames[i];
            }

            return Tuple.Create(new XmlQualifiedName(name, targetNamespace),
                                new XmlQualifiedName($"{name}Response", targetNamespace));
        }

        private XmlSchemaSequence CreateOperationRequestSequence(MethodInfo method)
        {
            var requestElement = new XmlSchemaElement { Name = "request" };

            var parameters = method.GetParameters().Where(p => p.IsParameterInVersion(version)).ToList();

            if (protocol == XRoadProtocol.Version20 || parameters.Count > 1)
            {
                var schemaTypeSequence = new XmlSchemaSequence();

                foreach (var parameter in parameters)
                {
                    var parameterAttribute = parameter.GetCustomAttributes(typeof(XRoadParameterAttribute), false)
                                                      .OfType<XRoadParameterAttribute>()
                                                      .SingleOrDefault();

                    var parameterName = !string.IsNullOrWhiteSpace(parameterAttribute?.Name) && protocol != XRoadProtocol.Version20
                        ? parameterAttribute.Name
                        : parameter.Name;

                    var parameterElement = new XmlSchemaElement { Name = parameterName, Annotation = CreateAnnotationElement(parameter) };
                    AddSchemaType(parameterElement, parameter.ParameterType, parameterAttribute != null && parameterAttribute.IsOptional, null);
                    schemaTypeSequence.Items.Add(parameterElement);
                }

                if (protocol == XRoadProtocol.Version20)
                    return schemaTypeSequence;

                requestElement.SchemaType = new XmlSchemaComplexType { Particle = schemaTypeSequence };
            }
            else if (parameters.Count == 1)
                AddSchemaType(requestElement, parameters.Single().ParameterType, false, null);
            else requestElement.SchemaType = new XmlSchemaComplexType();

            return new XmlSchemaSequence { Items = { requestElement } };
        }

        private XmlSchemaComplexType CreateResponseType(string responseName, MethodInfo method, XmlSchemaGroupBase requestSequence)
        {
            if (protocol == XRoadProtocol.Version20)
            {
                if (method.ReturnType == typeof(void) || !method.ReturnType.IsArray)
                    return new XmlSchemaComplexType { Name = responseName, Particle = new XmlSchemaSequence() };

                var tempElement = new XmlSchemaElement();
                AddSchemaType(tempElement, method.ReturnType, false, null);

                var complexContent = (XmlSchemaComplexContent)((XmlSchemaComplexType)tempElement.SchemaType).ContentModel;

                return new XmlSchemaComplexType { Name = responseName, ContentModel = complexContent };
            }

            var responseSequence = new XmlSchemaSequence();

            if (method.ReturnType != typeof(void))
            {
                var okElement = new XmlSchemaElement { Name = "value" };
                AddSchemaType(okElement, method.ReturnType, false, null);
                responseSequence.Items.Add(new XmlSchemaChoice { Items = { CreateFaultSequence(), okElement } });
            }
            else
            {
                var faultSequence = CreateFaultSequence();
                faultSequence.MinOccurs = 0;
                responseSequence.Items.Add(faultSequence);
            }

            var responseElement = new XmlSchemaElement { Name = "response", SchemaType = new XmlSchemaComplexType { Particle = responseSequence } };

            return new XmlSchemaComplexType
            {
                Name = responseName,
                Particle = new XmlSchemaSequence { Items = { requestSequence.Items[0], responseElement } }
            };
        }

        private XmlSchemaSequence CreateFaultSequence()
        {
            return new XmlSchemaSequence
            {
                Items =
                {
                    new XmlSchemaElement { Name = "faultCode", SchemaTypeName = new XmlQualifiedName("faultCode", NamespaceHelper.GetXRoadNamespace(protocol)) },
                    new XmlSchemaElement { Name = "faultString", SchemaTypeName = new XmlQualifiedName("faultString", NamespaceHelper.GetXRoadNamespace(protocol)) }
                }
            };
        }

        private void AddOperationTypes(XmlSchema schema)
        {
            foreach (var operationType in operationTypes.Values.OrderBy(x => x.Item2.Name))
            {
                schema.Items.Add(operationType.Item2);
                if (protocol != XRoadProtocol.Version20 || operationType.Item1.ReturnType.IsArray)
                    schema.Items.Add(operationType.Item3);
            }
        }

        private void AddSchemaElements(XmlSchema schema)
        {
            if (protocol == XRoadProtocol.Version20)
                return;

            foreach (var schemaElement in schemaElements)
                schema.Items.Add(schemaElement.Value);
        }

        private Message CreateOperationMessage(XmlSchemaElement element, MethodInfo methodContract, Message inputMessage = null)
        {
            if (protocol == XRoadProtocol.Version31)
                return new Message
                {
                    Name = element.Name,
                    Parts = { new MessagePart { Name = "body", Element = new XmlQualifiedName(element.Name, targetNamespace) } }
                };

            var message = new Message { Name = element.SchemaTypeName.Name };

            if (inputMessage != null)
                message.Parts.Add(new MessagePart { Name = "paring", Type = inputMessage.Parts[0].Type });

            if (inputMessage != null && !methodContract.ReturnType.IsArray)
                message.Parts.Add(new MessagePart { Name = "keha", Type = GetSimpleTypeName(methodContract.ReturnType) ?? GetComplexTypeName(methodContract.ReturnType) });
            else message.Parts.Add(new MessagePart { Name = "keha", Type = element.SchemaTypeName });

            if (inputMessage == null && methodContract.HasMultipartRequest())
                message.Parts.Add(new MessagePart { Name = "p1", Type = GetSimpleTypeName(typeof(Stream)) });

            if (inputMessage != null && methodContract.HasMultipartResponse())
                message.Parts.Add(new MessagePart { Name = "p2", Type = GetSimpleTypeName(typeof(Stream)) });

            return message;
        }

        private XmlSchemaAnnotation CreateAnnotationElement(ICustomAttributeProvider provider)
        {
            var nodes = provider.GetXRoadTitles()
                                .Where(title => !string.IsNullOrWhiteSpace(title.Item2))
                                .Select(title => CreateXRoadTitleElement(title.Item1, title.Item2))
                                .ToArray();

            return nodes.Length > 0 ? new XmlSchemaAnnotation { Items = { new XmlSchemaAppInfo { Markup = nodes } } } : null;
        }

        private XmlElement CreateDocumentationElement(ICustomAttributeProvider provider)
        {
            var nodes = provider.GetXRoadTitles()
                                .Where(title => !string.IsNullOrWhiteSpace(title.Item2))
                                .Select(title => CreateXRoadTitleElement(title.Item1, title.Item2))
                                .ToArray();

            if (nodes.Length < 1)
                return null;

            var documentationElement = CreateElement("wsdl", "documentation", NamespaceHelper.WSDL);

            foreach (var node in nodes)
                documentationElement.AppendChild(node);

            return documentationElement;
        }

        private XmlNode CreateXRoadTitleElement(string languageCode, string value)
        {
            var titleElement = CreateElement(PrefixHelper.GetXRoadPrefix(protocol), "title", xroadNamespace);
            titleElement.InnerText = value;

            if (!string.IsNullOrWhiteSpace(languageCode))
                titleElement.Attributes.Append(CreateAttribute("xml", "lang", null, languageCode));

            return titleElement;
        }

        private XmlElement CreateAddressBindingElement()
        {
            var addressElement = CreateElement(PrefixHelper.GetXRoadPrefix(protocol), "address", xroadNamespace);
            addressElement.Attributes.Append(CreateAttribute(null, "producer", null, environmentProducerName));
            return addressElement;
        }

        private XmlElement CreateXRoadVersionBindingElement(uint value)
        {
            var addressElement = CreateElement(PrefixHelper.GetXRoadPrefix(protocol), "version", xroadNamespace);
            addressElement.InnerText = $"v{value}";
            return addressElement;
        }

        #endregion
    }
}