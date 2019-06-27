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

using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Handlers
{
    public sealed class ProtocolMessageSignHandlerTest
    {
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IMessageDto _dto;
        private readonly IKeySigner _keySigner;

        public ProtocolMessageSignHandlerTest()
        {
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _keySigner = Substitute.For<IKeySigner>();

            _dto = new DtoFactory().GetDto(new PingRequest(),
                PeerIdentifierHelper.GetPeerIdentifier("recipient"), 
                PeerIdentifierHelper.GetPeerIdentifier("sender")
            );
        }

        [Fact]
        public void CantSignMessage()
        {
            var protocolMessageSignHandler = new ProtocolMessageSignHandler(_keySigner);

            protocolMessageSignHandler.WriteAsync(_fakeContext, new object());

            _keySigner.DidNotReceiveWithAnyArgs().Sign(Arg.Any<byte[]>());
            _fakeContext.ReceivedWithAnyArgs().WriteAsync(new object());
        }

        [Fact]
        public void CanWriteAsyncOnSigningMessage()
        {
            _keySigner.Sign(Arg.Any<byte[]>()).Returns(new Signature(ByteUtil.GenerateRandomByteArray(64)));

            var protocolMessageSignHandler = new ProtocolMessageSignHandler(_keySigner);

            protocolMessageSignHandler.WriteAsync(_fakeContext, _dto);
            
            _fakeContext.DidNotReceiveWithAnyArgs().WriteAndFlushAsync(new object());
            _fakeContext.ReceivedWithAnyArgs().WriteAsync(new object());
        }
    }
}