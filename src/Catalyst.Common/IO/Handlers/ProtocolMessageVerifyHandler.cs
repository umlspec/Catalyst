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

using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    public sealed class ProtocolMessageVerifyHandler : InboundChannelHandlerBase<ProtocolMessageSigned>
    {
        private readonly IKeySigner _keySigner;

        public ProtocolMessageVerifyHandler(IKeySigner keySigner, ILogger logger) : base(logger)
        {
            _keySigner = keySigner;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessageSigned signedMessage)
        {
            if (_keySigner.Verify(
                new PublicKey(signedMessage.Message.PeerId.PublicKey.ToByteArray()),
                signedMessage.Message.ToByteString().ToByteArray(),
                new Signature(signedMessage.Signature.ToByteArray()))
            )
            {
                ctx.FireChannelRead(signedMessage.Message);
            }
        }
    }
}
