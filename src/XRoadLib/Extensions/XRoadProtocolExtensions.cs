﻿using System;
using System.Text.RegularExpressions;

namespace XRoadLib.Extensions
{
    public static class XRoadProtocolExtensions
    {
        private static readonly Regex namespacePatternV20 = new Regex(@"(http\:\/\/producers.\w.xtee.riik.ee/producer/\w)/.+");
        private static readonly Regex namespacePatternV31 = new Regex(@"(http://\w.x-road.ee/producer/).+");
        private static readonly Regex namespacePatternV40 = new Regex(@"(http://\w.x-road.eu)/.+");

        private static void ConstrainToDefinedValue(XRoadProtocol protocol)
        {
            if (!protocol.HasValue())
                throw new ArgumentOutOfRangeException(nameof(protocol));
        }

        public static bool HasDefinedValue(this XRoadProtocol protocol)
        {
            return Enum.IsDefined(typeof(XRoadProtocol), protocol);
        }

        public static bool HasValue(this XRoadProtocol protocol)
        {
            return protocol.HasDefinedValue() && protocol != XRoadProtocol.Undefined;
        }

        public static string GetNamespace(this XRoadProtocol protocol)
        {
            ConstrainToDefinedValue(protocol);

            switch (protocol)
            {
                case XRoadProtocol.Version20:
                    return NamespaceConstants.XTEE;

                case XRoadProtocol.Version31:
                    return NamespaceConstants.XROAD;

                case XRoadProtocol.Version40:
                    return NamespaceConstants.XROAD_V4;

                default:
                    throw new ArgumentException($"Unmapped X-Road protocol version `{protocol}`.", nameof(protocol));
            }
        }

        public static string GetProducerNamespace(this XRoadProtocol protocol, string producerName)
        {
            ConstrainToDefinedValue(protocol);

            switch (protocol)
            {
                case XRoadProtocol.Version20:
                    return $"http://producers.{producerName}.xtee.riik.ee/producer/{producerName}";

                case XRoadProtocol.Version31:
                    return $"http://{producerName}.x-road.ee/producer/";

                case XRoadProtocol.Version40:
                    return $"http://{producerName}.x-road.eu";

                default:
                    throw new ArgumentException($"Unmapped X-Road protocol version `{protocol}`.", nameof(protocol));
            }
        }

        public static string GetProducerNamespaceBase(this XRoadProtocol protocol, string ns)
        {
            ConstrainToDefinedValue(protocol);

            Regex regex;

            switch (protocol)
            {
                case XRoadProtocol.Version20:
                    regex = namespacePatternV20;
                    break;

                case XRoadProtocol.Version31:
                    regex = namespacePatternV31;
                    break;

                case XRoadProtocol.Version40:
                    regex = namespacePatternV40;
                    break;

                default:
                    throw new ArgumentException($"Unmapped X-Road protocol version `{protocol}`.", nameof(protocol));
            }

            var match = regex.Match(ns);

            return match.Success ? match.Groups[1].Value : null;
        }

        public static string GetPrefix(this XRoadProtocol protocol)
        {
            ConstrainToDefinedValue(protocol);

            switch (protocol)
            {
                case XRoadProtocol.Version20:
                    return PrefixConstants.XTEE;

                case XRoadProtocol.Version31:
                case XRoadProtocol.Version40:
                    return PrefixConstants.XROAD;

                default:
                    throw new ArgumentException($"Unmapped X-Road protocol version `{protocol}`.", nameof(protocol));
            }
        }
    }
}