#### 1.0.0-beta042 - 22.02.2017
* Fix NuGet publishing.
* Remove duplicate entries from API reference.

#### 1.0.0-beta041 - 22.02.2017
* Improve extensibility of base handlers.

#### 1.0.0-beta040 - 30.11.2016
* Fix Content-Type header value of `application/xop+xml` message root part.

#### 1.0.0-beta039 - 29.11.2016
* Extension package to support Google Protocol Buffers serialization.

#### 1.0.0-beta038 - 22.11.2016
* Allow overriding of default IHttpHandler.ProcessRequest method.

#### 1.0.0-beta037 - 21.11.2016
* Fix base64 encoding excess memory allocation problem.

#### 1.0.0-beta036 - 13.10.2016
* Fix regression when using meta services.

#### 1.0.0-beta035 - 13.10.2016
* Use explicit target namespace prefix instead of default in service description.
* Use highest supported version of serializers for meta services.
* Fix base64 attachment serialization bug: insert line breaks after every 76 characters.

#### 1.0.0-beta034 - 29.09.2016
* Fix X-Road header serialization error when serializing required header with missing value.
* Fix X-Road protocol name not assigned bug.

#### 1.0.0-beta033 - 29.09.2016
* Improved customization support for X-Road message headers.

#### 1.0.0-beta032 - 28.09.2016
* Refactor all customizations to ISchemaExporter.

#### 1.0.0-beta031 - 23.09.2016
* Improve namespace dependency management in service descriptions.
* Fix parsing of X-Road message protocol version 4.0 response headers.

#### 1.0.0-beta030 - 22.09.2016
* Fix regression in listMethods service.

#### 1.0.0-beta029 - 22.09.2016
* New X-Road fault structure for message protocol version 4.0.
* Fix regression: remove type attribute from RPC/Encoded root element.
* Deprecate custom CData-based escaping in preference to HTML encoded special characters.
* Fix SOAP Fault parsing when details contains XML.

#### 1.0.0-beta028 - 20.09.2016
* Use customized type name when searching runtime types from contract assembly.

#### 1.0.0-beta027 - 20.09.2016
* Configuration option to ignore type attributes in content definitions.

#### 1.0.0-beta026 - 19.09.2016
* Fix request serialization/deserialization issues for inherited types.

#### 1.0.0-beta025 - 19.09.2016
* Separate explicit type attribute serialization logic for request elements.

#### 1.0.0-beta024 - 16.08.2016
* Trim quotes from content type header charset.

#### 1.0.0-beta023 - 08.08.2016
* Add schemaLocation attribute to Xml Schema import element.

#### 1.0.0-beta022 - 17.07.2016
* Make .NET Core features available for .NET 4.5.1.

#### 1.0.0-beta021 - 12.07.2016
* Add .NET Core support.
* Validate presence of required elements inside sequences.
* Optional namespace override for service root element.
* Fixes for non-seekable input stream.

#### 1.0.0-beta020 - 19.04.2016
* Fix service description for rootless list type response element.

#### 1.0.0-beta019 - 13.04.2016
* Better validation for XML Schema date types.

#### 1.0.0-beta018 - 07.04.2016
* Fix serialization of RPC/Encoded style header elements.

#### 1.0.0-beta017 - 02.04.2016
* Fix parameter name of XRoadProtocol40 constructor.
* Add informational version number to released assembly.
* Remove byte order mark from outgoing messages.

#### 1.0.0-beta016 - 01.04.2016
* Fix identifiers ToString() methods.
* Add optional inner exception argument to XRoadFaultException type constructor.

#### 1.0.0-beta015 - 29.03.2016
* Fix serialization bug of testSystem response when X-Road message protocol version is not supported.

#### 1.0.0-beta014 - 23.03.2016
* Fix deserialization bug when element with xsi:nil attribute has content.

#### 1.0.0-beta013 - 22.03.2016
* Fix X-Road namespace import when title attributes are used.

#### 1.0.0-beta012 - 17.03.2016
* Fix object reference exception when no SchemaExporter is defined for protocol.
* Check type or operation namespace before creating type map (currently it allowed to use wrong namespaces for composite types).

#### 1.0.0-beta011 - 15.03.2016
* Fix types in RPC encoded messages.
* Remove contentType from xop:Include element.

#### 1.0.0-beta010 - 10.03.2016
* Add content length to XRoadMessage for write and read operations.
* Add definition property to allow optionally ignore array item element names.
* Fix MIME message output for Linux systems.

#### 1.0.0-beta009 - 06.03.2016
* Fix regression of 1.0.0-beta008.
* Refactor to use string interpolation instead of string.Format in error messages.

#### 1.0.0-beta008 - 06.03.2016
* Remove unused X-Road exceptions.
* Replace exception message of unsuccessful service detection.

#### 1.0.0-beta007 - 04.03.2016
* Fix XRoadRequest helper class.
* Add helper method to generate X-Road request ID-s.

#### 1.0.0-beta006 - 04.03.2016
* Fix XRoadRequest class to make POST requests instead of GET.
* Refactor X-Road meta services to handle them separately.

#### 1.0.0-beta005 - 04.03.2016
* Fix Xop binary content serialization bug.
* Fix deserialization of merged empty root element.

#### 1.0.0-beta004 - 03.03.2016
* Fix serialization/deserialization in reverse direction.

#### 1.0.0-beta003 - 03.03.2016
* Add X-Road v4.0 style non-technical faults.
* Alternative explicit non-technical fault representations.

#### 1.0.0-beta002 - 19.02.2016
* Multiple schemas in single WSDL file.
* Detect type dependency to filter out unused types from service description.

#### 1.0.0-beta001 - February 14 2016
* Redesign X-Road protocol concept.
* Default protocol implementations for X-Road message protocol versions 2.0, 3.1 and 4.0.
* Improved customization of properties, types and methods.
* Customizable serialization processor.
* Better separation of encoded and literal styles.
* Type improvements: merged content and arrays without wrapper element.
* Clean attributes and support more existing attributes from System.Xml.Serialization namespace.

#### 1.0.0-alpha020 - January 26 2016
* Improve protocol v4.0 generated service description.
* Bug fix: add X-Road namespace to imports when title element is used in schema.

#### 1.0.0-alpha019 - January 25 2016
* Remove XRoadParameterAttribute in favor of XmlElementAttribute
* Rename default handlers.
* More customizations for collections.
* Add description for enumeration types.

#### 1.0.0-alpha018 - January 21 2016
* Bug: anonymous type name in service description.

#### 1.0.0-alpha017 - January 21 2016
* Add support for anonymous types.
* Fix error messages.

#### 1.0.0-alpha016 - January 20 2016
* Add basic handlers for data and service description requests.
* Fix bugs of previous releases.

#### 1.0.0-alpha015 - January 19 2016
* Refactor to use XName instead of XmlQualifiedName.
* Add deserializer for X-Road standard non-technical fault.
* Add deserializer for standard SOAP fault.
* Parameter name customization for contract parameters only.
* Remove protocol dependency from parameter name selection.
* Remove XRoadSerializer since it's buggy and is not needed by the library.
* Add helper method which deserializes SOAP message and handles faults when present.

#### 1.0.0-alpha014 - January 18 2016
* Add support for X-Road v6 and X-Road messaging protcol v4.0.
* Redesign support for different protocol versions.
* Add interfaces to override default type options.
* Unhandled SOAP header elements as XElement instead of string representation.

#### 1.0.0-alpha013 - January 14 2016
* More annotation elements to types and elements.
* Accept XmlElementAttribute.DataType for parameter elements.
* Type content particle uses always sequence, but deserialization mode may vary.

#### 1.0.0-alpha012 - January 12 2016
* Fix strict parameter deserialization when xml template is not used.
* General request handler for X-Road data requests.
* Refactored serializer cache to deal with one assembly only.
* Bug fixes.

#### 1.0.0-alpha011 - January 11 2016
* Fix service root element name.

#### 1.0.0-alpha010 - January 11 2016
* Fix bug with custom operation type name format.

#### 1.0.0-alpha009 - January 11 2016
* Bug fix.

#### 1.0.0-alpha008 - January 11 2016
* Use `all` particle for non-strict operation types.
* Make `XRoadImportAttribute` protocol specific.
* Common interface for appliable attributes.
* Customize parameter names with global name provider.

#### 1.0.0-alpha007 - January 11 2016
* Allow protocol specific differences for schema import.

#### 1.0.0-alpha006 - January 11 2016
* Customize schema import namespaces and location through attribute parameter.

#### 1.0.0-alpha005 - January 11 2016
* Fix wrong version number in generated service description.
* Add properties to test of operation version limits are specified.

#### 1.0.0-alpha004 - January 11 2016
* Restore separate producer name attribute.
* Hide complexity of operation declarations in producer definition.

#### 1.0.0-alpha003 - January 11 2016
* More configuration options through contract assembly attributes.
* Layout attribute to control content particle and order of elements.

#### 1.0.0-alpha002 - January 7 2016
* Add SOAP specific helper methods.
* Add helper methods to resolve operation contracts.

#### 1.0.0-alpha001 - January 7 2016
* Initial release
* Extract common logic into separate project.
