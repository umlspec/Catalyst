﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;

namespace MultiFormats
{
    /// <summary>
    ///   Metadata on an IPFS network address protocol.
    /// </summary>
    /// <remarks>
    ///   Protocols are defined at <see href="https://github.com/multiformats/multiaddr/blob/master/protocols.csv"/>.
    /// </remarks>
    /// <seealso cref="MultiFormats.MultiAddress"/>
    public abstract class NetworkProtocol
    {
        internal static Dictionary<string, Type> Names = new Dictionary<string, Type>();
        internal static Dictionary<uint, Type> Codes = new Dictionary<uint, Type>();

        /// <summary>
        ///   Registers the standard network protocols for IPFS.
        /// </summary>
        static NetworkProtocol()
        {
            NetworkProtocol.Register<Ipv4NetworkProtocol>();
            NetworkProtocol.Register<Ipv6NetworkProtocol>();
            NetworkProtocol.Register<TcpNetworkProtocol>();
            NetworkProtocol.Register<UdpNetworkProtocol>();
            NetworkProtocol.Register<P2PNetworkProtocol>();
            NetworkProtocol.RegisterAlias<IpfsNetworkProtocol>();
            NetworkProtocol.Register<QuicNetworkProtocol>();
            NetworkProtocol.Register<HttpNetworkProtocol>();
            NetworkProtocol.Register<HttpsNetworkProtocol>();
            NetworkProtocol.Register<DccpNetworkProtocol>();
            NetworkProtocol.Register<SctpNetworkProtocol>();
            NetworkProtocol.Register<WsNetworkProtocol>();
            NetworkProtocol.Register<Libp2PWebrtcStarNetworkProtocol>();
            NetworkProtocol.Register<UdtNetworkProtocol>();
            NetworkProtocol.Register<UtpNetworkProtocol>();
            NetworkProtocol.Register<OnionNetworkProtocol>();
            NetworkProtocol.Register<Libp2PWebrtcDirectNetworkProtocol>();
            NetworkProtocol.Register<P2PCircuitNetworkProtocol>();
            NetworkProtocol.Register<DnsNetworkProtocol>();
            NetworkProtocol.Register<Dns4NetworkProtocol>();
            NetworkProtocol.Register<Dns6NetworkProtocol>();
            NetworkProtocol.Register<DnsAddrNetworkProtocol>();
            NetworkProtocol.Register<WssNetworkProtocol>();
            NetworkProtocol.Register<IpcidrNetworkProtocol>();
        }

        /// <summary>
        ///   Register a network protocol for use.
        /// </summary>
        /// <typeparam name="T">
        ///   A <see cref="NetworkProtocol"/> to register.
        /// </typeparam>
        public static void Register<T>() where T : NetworkProtocol, new()
        {
            var protocol = new T();

            if (Names.ContainsKey(protocol.Name))
                throw new ArgumentException(string.Format("The IPFS network protocol '{0}' is already defined.",
                    protocol.Name));
            if (Codes.ContainsKey(protocol.Code))
                throw new ArgumentException(string.Format("The IPFS network protocol code ({0}) is already defined.",
                    protocol.Code));

            Names.Add(protocol.Name, typeof(T));
            Codes.Add(protocol.Code, typeof(T));
        }

        /// <summary>
        ///   Register an alias to another network protocol.
        /// </summary>
        /// <typeparam name="T">
        ///   A <see cref="NetworkProtocol"/> to register.
        /// </typeparam>
        public static void RegisterAlias<T>() where T : NetworkProtocol, new()
        {
            var protocol = new T();

            if (Names.ContainsKey(protocol.Name))
                throw new ArgumentException(string.Format("The IPFS network protocol '{0}' is already defined.",
                    protocol.Name));
            if (!Codes.ContainsKey(protocol.Code))
                throw new ArgumentException(string.Format("The IPFS network protocol code ({0}) is not defined.",
                    protocol.Code));

            Names.Add(protocol.Name, typeof(T));
        }

        /// <summary>
        ///   The name of the protocol.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///   The IPFS numeric code assigned to the network protocol.
        /// </summary>
        public abstract uint Code { get; }

        /// <summary>
        ///   The string value associated with the protocol.
        /// </summary>
        /// <remarks>
        ///   For tcp and udp this is the port number.  This can be <b>null</b> as is the case for http and https.
        /// </remarks>
        public string Value { get; set; }

        /// <summary>
        ///   Writes the binary representation to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Google.Protobuf.CodedOutputStream"/> to write to.
        /// </param>
        /// <remarks>
        ///   The binary representation of the <see cref="Value"/>.
        /// </remarks>
        public abstract void WriteValue(CodedOutputStream stream);

        /// <summary>
        ///   Writes the string representation to the specified <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="TextWriter"/> to write to.
        /// </param>
        /// <remarks>
        ///   The string representation of the optional <see cref="Value"/>.
        /// </remarks>
        public virtual void WriteValue(TextWriter stream)
        {
            if (Value != null)
            {
                stream.Write('/');
                stream.Write(Value);
            }
        }

        /// <summary>
        ///   Reads the binary representation from the specified <see cref="Google.Protobuf.CodedInputStream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Google.Protobuf.CodedOutputStream"/> to read from.
        /// </param>
        /// <remarks>
        ///   The binary representation is an option <see cref="Value"/>.
        /// </remarks>
        public abstract void ReadValue(CodedInputStream stream);

        /// <summary>
        ///   Reads the string representation from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="TextReader"/> to read from
        /// </param>
        /// <remarks>
        ///   The string representation is "/<see cref="Name"/>" followed by 
        ///   an optional "/<see cref="Value"/>".
        /// </remarks>
        public virtual void ReadValue(TextReader stream)
        {
            Value = string.Empty;
            int c;
            while (-1 != (c = stream.Read()) && c != '/')
            {
                Value += (char) c;
            }
        }

        /// <summary>
        ///   The <see cref="Name"/> and optional <see cref="Value"/> of the network protocol.
        /// </summary>
        public override string ToString()
        {
            using (var s = new StringWriter())
            {
                s.Write('/');
                s.Write(Name);
                WriteValue(s);
                return s.ToString();
            }
        }
    }

    class TcpNetworkProtocol : NetworkProtocol
    {
        public UInt16 Port { get; set; }
        public override string Name { get { return "tcp"; } }
        public override uint Code { get { return 6; } }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            try
            {
                Port = UInt16.Parse(Value);
            }
            catch (Exception e)
            {
                throw new FormatException(string.Format("'{0}' is not a valid port number.", Value), e);
            }
        }

        public override void ReadValue(CodedInputStream stream)
        {
            var bytes = stream.ReadSomeBytes(2);
            Port = (UInt16) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 0));
            Value = Port.ToString(CultureInfo.InvariantCulture);
        }

        public override void WriteValue(CodedOutputStream stream)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16) Port));
            stream.WriteSomeBytes(bytes);
        }
    }

    class UdpNetworkProtocol : TcpNetworkProtocol
    {
        public override string Name { get { return "udp"; } }
        public override uint Code { get { return 273; } }
    }

    class DccpNetworkProtocol : TcpNetworkProtocol
    {
        public override string Name { get { return "dccp"; } }
        public override uint Code { get { return 33; } }
    }

    class SctpNetworkProtocol : TcpNetworkProtocol
    {
        public override string Name { get { return "sctp"; } }
        public override uint Code { get { return 132; } }
    }

    abstract class IpNetworkProtocol : NetworkProtocol
    {
        public IPAddress Address { get; set; }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            try
            {
                // Remove the scope id.
                int i = Value.LastIndexOf('%');
                if (i != -1)
                    Value = Value.Substring(0, i);

                Address = IPAddress.Parse(Value);
            }
            catch (Exception e)
            {
                throw new FormatException(string.Format("'{0}' is not a valid IP address.", Value), e);
            }
        }

        public override void WriteValue(TextWriter stream)
        {
            stream.Write('/');
            stream.Write(Address.ToString());
        }

        public override void WriteValue(CodedOutputStream stream)
        {
            var ip = Address.GetAddressBytes();
            stream.WriteSomeBytes(ip);
        }
    }

    class Ipv4NetworkProtocol : IpNetworkProtocol
    {
        static int _addressSize = IPAddress.Any.GetAddressBytes().Length;

        public override string Name { get { return "ip4"; } }
        public override uint Code { get { return 4; } }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            if (Address.AddressFamily != AddressFamily.InterNetwork)
                throw new FormatException(string.Format("'{0}' is not a valid IPv4 address.", Value));
        }

        public override void ReadValue(CodedInputStream stream)
        {
            var a = stream.ReadSomeBytes(_addressSize);
            Address = new IPAddress(a);
            Value = Address.ToString();
        }
    }

    class Ipv6NetworkProtocol : IpNetworkProtocol
    {
        static int _addressSize = IPAddress.IPv6Any.GetAddressBytes().Length;

        public override string Name { get { return "ip6"; } }
        public override uint Code { get { return 41; } }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            if (Address.AddressFamily != AddressFamily.InterNetworkV6)
                throw new FormatException(string.Format("'{0}' is not a valid IPv6 address.", Value));
        }

        public override void ReadValue(CodedInputStream stream)
        {
            var a = stream.ReadSomeBytes(_addressSize);
            Address = new IPAddress(a);
            Value = Address.ToString();
        }
    }

    class P2PNetworkProtocol : NetworkProtocol
    {
        public MultiHash MultiHash { get; private set; }
        public override string Name { get { return "p2p"; } }
        public override uint Code { get { return 421; } }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            MultiHash = new MultiHash(Value);
        }

        public override void ReadValue(CodedInputStream stream)
        {
            stream.ReadLength();
            MultiHash = new MultiHash(stream);
            Value = MultiHash.ToBase58();
        }

        public override void WriteValue(CodedOutputStream stream)
        {
            var bytes = MultiHash.ToArray();
            stream.WriteLength(bytes.Length);
            stream.WriteSomeBytes(bytes);
        }
    }

    class IpfsNetworkProtocol : P2PNetworkProtocol
    {
        public override string Name { get { return "ipfs"; } }
    }

    class OnionNetworkProtocol : NetworkProtocol
    {
        public byte[] Address { get; private set; }
        public UInt16 Port { get; private set; }
        public override string Name { get { return "onion"; } }
        public override uint Code { get { return 444; } }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            var parts = Value.Split(':');
            if (parts.Length != 2)
                throw new FormatException(string.Format("'{0}' is not a valid onion address, missing the port number.",
                    Value));
            if (parts[0].Length != 16)
                throw new FormatException(string.Format("'{0}' is not a valid onion address.", Value));
            try
            {
                Port = UInt16.Parse(parts[1]);
            }
            catch (Exception e)
            {
                throw new FormatException(
                    string.Format("'{0}' is not a valid onion address, invalid port number.", Value), e);
            }

            if (Port < 1)
                throw new FormatException(string.Format("'{0}' is not a valid onion address, invalid port number.",
                    Value));
            Address = parts[0].ToUpperInvariant().FromBase32();
        }

        public override void ReadValue(CodedInputStream stream)
        {
            Address = stream.ReadSomeBytes(10);
            var bytes = stream.ReadSomeBytes(2);
            Port = (UInt16) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 0));
            Value = Address.ToBase32().ToLowerInvariant() + ":" + Port.ToString(CultureInfo.InvariantCulture);
        }

        public override void WriteValue(CodedOutputStream stream)
        {
            stream.WriteSomeBytes(Address);
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16) Port));
            stream.WriteSomeBytes(bytes);
        }
    }

    abstract class ValuelessNetworkProtocol : NetworkProtocol
    {
        public override void ReadValue(CodedInputStream stream)
        {
            // No value to read 
        }

        public override void ReadValue(TextReader stream)
        {
            // No value to read 
        }

        public override void WriteValue(CodedOutputStream stream)
        {
            // No value to write
        }
    }

    class QuicNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "quic"; } }
        public override uint Code { get { return 460; } }
    }

    class HttpNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "http"; } }
        public override uint Code { get { return 480; } }
    }

    class HttpsNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "https"; } }
        public override uint Code { get { return 443; } }
    }

    class WsNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "ws"; } }
        public override uint Code { get { return 477; } }
    }

    class WssNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "wss"; } }
        public override uint Code { get { return 478; } }
    }

    class Libp2PWebrtcStarNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "libp2p-webrtc-star"; } }
        public override uint Code { get { return 275; } }
    }

    class Libp2PWebrtcDirectNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "libp2p-webrtc-direct"; } }
        public override uint Code { get { return 276; } }
    }

    class UdtNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "udt"; } }
        public override uint Code { get { return 301; } }
    }

    class UtpNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "utp"; } }
        public override uint Code { get { return 302; } }
    }

    class P2PCircuitNetworkProtocol : ValuelessNetworkProtocol
    {
        public override string Name { get { return "p2p-circuit"; } }
        public override uint Code { get { return 290; } }
    }

    abstract class DomainNameNetworkProtocol : NetworkProtocol
    {
        public string DomainName { get; set; }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            DomainName = Value;
        }

        public override void ReadValue(CodedInputStream stream)
        {
            Value = stream.ReadString();
            DomainName = Value;
        }

        public override void WriteValue(TextWriter stream)
        {
            stream.Write('/');
            stream.Write(DomainName.ToString());
        }

        public override void WriteValue(CodedOutputStream stream) { stream.WriteString(DomainName); }
    }

    class DnsNetworkProtocol : DomainNameNetworkProtocol
    {
        public override string Name { get { return "dns"; } }
        public override uint Code { get { return 53; } }
    }

    class DnsAddrNetworkProtocol : DomainNameNetworkProtocol
    {
        public override string Name { get { return "dnsaddr"; } }
        public override uint Code { get { return 56; } }
    }

    class Dns4NetworkProtocol : DomainNameNetworkProtocol
    {
        public override string Name { get { return "dns4"; } }
        public override uint Code { get { return 54; } }
    }

    class Dns6NetworkProtocol : DomainNameNetworkProtocol
    {
        public override string Name { get { return "dns6"; } }
        public override uint Code { get { return 55; } }
    }

    class IpcidrNetworkProtocol : NetworkProtocol
    {
        public UInt16 RoutingPrefix { get; set; }

        public override string Name { get { return "ipcidr"; } }

        // TODO: https://github.com/multiformats/multiaddr/issues/60
        public override uint Code { get { return 999; } }

        public override void ReadValue(TextReader stream)
        {
            base.ReadValue(stream);
            try
            {
                RoutingPrefix = UInt16.Parse(Value);
            }
            catch (Exception e)
            {
                throw new FormatException(string.Format("'{0}' is not a valid routing prefix.", Value), e);
            }
        }

        public override void ReadValue(CodedInputStream stream)
        {
            var bytes = stream.ReadSomeBytes(2);
            RoutingPrefix = (UInt16) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, 0));
            Value = RoutingPrefix.ToString(CultureInfo.InvariantCulture);
        }

        public override void WriteValue(CodedOutputStream stream)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((Int16) RoutingPrefix));
            stream.WriteSomeBytes(bytes);
        }
    }
}
