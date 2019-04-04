#region LICENSE
/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
* 
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/
#endregion

using System;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Xunit;

namespace Catalyst.Node.Common.UnitTests.Helpers
{
    public class ProtobufExtensionsTests
    {
        [Fact]
        public static void ShortenedFullName_should_remove_namespace_start()
        {
            Transaction.Descriptor.FullName.Should().Be("Catalyst.Protocol.Transaction.Transaction");
            Transaction.Descriptor.ShortenedFullName().Should().Be("Transaction.Transaction");
        }

        [Fact]
        public static void ShortenedProtoFullName_should_remove_namespace_start()
        {
            PingRequest.Descriptor.FullName.Should().Be("Catalyst.Protocol.IPPN.PingRequest");
            typeof(PingRequest).ShortenedProtoFullName().Should().Be("IPPN.PingRequest");
        }

        [Theory]
        [InlineData("MyFunnyRequest", "MyFunnyResponse")]
        [InlineData("Request", "Response")]
        [InlineData("Some.Namespace.ClassRequest", "Some.Namespace.ClassResponse")]
        public static void GetResponseType_should_swap_request_suffix_for_response_suffix(string requestType, string responseType)
        {
            requestType.GetResponseType().Should().Be(responseType);
        }

        [Theory]
        [InlineData("MyFunnyResponse", "MyFunnyRequest")]
        [InlineData("Response", "Request")]
        [InlineData("Some.Namespace.ClassResponse", "Some.Namespace.ClassRequest")]
        public static void GetRequestType_should_swap_request_suffix_for_response_suffix(string responseType, string requestType)
        {
            responseType.GetRequestType().Should().Be(requestType);
        }

        [Fact]
        public static void ToAnySigned_should_happen_new_guid_to_request_if_not_specified()
        {
            //this ensures we won't get Guid.Empty and then a risk of mismatch;
            var wrapped = new PingRequest().ToAnySigned(PeerIdHelper.GetPeerId("you"));
            wrapped.CorrelationId.Should().NotBeEquivalentTo(Guid.Empty.ToByteString());
        }

        [Fact]
        public static void ToAnySigned_should_set_the_wrapper_fields()
        {
            var guid = Guid.NewGuid();
            var peerId = PeerIdHelper.GetPeerId("blablabla");
            var expectedContent = "content";
            var wrapped = new PeerInfoRequest {Ping = expectedContent}.ToAnySigned(peerId, guid);

            wrapped.CorrelationId.ToGuid().Should().Be(guid);
            wrapped.PeerId.Should().Be(peerId);
            wrapped.TypeUrl.Should().Be(PeerInfoRequest.Descriptor.ShortenedFullName());
            wrapped.FromAnySigned<PeerInfoRequest>().Ping.Should().Be(expectedContent);
        }

        [Fact]
        public static void ToAnySigned_should_fail_on_response_without_correlationId()
        {
            var peerId = PeerIdHelper.GetPeerId("someone");
            var expectedContent = "censored";
            var response = new PeerInfoResponse { Pong = expectedContent };
            new Action(() => response.ToAnySigned(peerId))
               .Should().Throw<ArgumentException>();
        }
    }
}