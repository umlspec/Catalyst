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

using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <summary>
    /// The response handler for removing a peer
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class RemovePeerResponseObserver
        : ResponseObserverBase<RemovePeerResponse>,
            IRpcResponseObserver
    {
        private readonly IUserOutput _userOutput;

        public RemovePeerResponseObserver(IUserOutput userOutput,
            ILogger logger) : base(logger)
        {
            _userOutput = userOutput;
        }

        protected override void HandleResponse(RemovePeerResponse removePeerResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(removePeerResponse, nameof(removePeerResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug($@"Handling Remove Peer Response");
            
            var deletedCount = removePeerResponse.DeletedCount;

            _userOutput.WriteLine($@"Deleted {deletedCount.ToString()} peers");
        }
    }
}