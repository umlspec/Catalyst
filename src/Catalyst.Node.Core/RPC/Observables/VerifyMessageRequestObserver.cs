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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Cryptography.BulletProofs.Wrapper.Types;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Multiformats.Base;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class VerifyMessageRequestObserver
        : RequestObserverBase<VerifyMessageRequest, VerifyMessageResponse>,
            IRpcRequestObserver
    {
        private readonly IKeySigner _keySigner;

        private const string PublicKeyEncodingInvalid = "Invalid PublicKey encoding";
        private const string PublicKeyNotProvided = "PublicKey not provided";
        private const string SignatureEncodingInvalid = "Invalid Signature encoding";
        private const string SignatureNotProvided = "Signature not provided";
        private const string FailedToHandleMessage = "Failed to handle VerifyMessageRequest after receiving message";

        public VerifyMessageRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IKeySigner keySigner)
            : base(logger, peerIdentifier)
        {
            _keySigner = keySigner;
        }

        protected override IMessage<VerifyMessageResponse> HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type VerifyMessageRequest");
            
            var deserialised = messageDto.Payload.FromProtocolMessage<VerifyMessageRequest>();
            Guard.Argument(deserialised).NotNull("The verify message request cannot be null");

            var decodedMessage = RLP.Decode(deserialised.Message.ToByteArray()).RLPData.ToStringFromRLPDecoded();
            var publicKey = deserialised.PublicKey;
            var signature = deserialised.Signature;

            try
            {
                if (!Multibase.TryDecode(publicKey.ToStringUtf8(), out _, out var decodedPublicKey))
                {
                    Logger.Error($"{PublicKeyEncodingInvalid}");
                    return ReturnResponse(false);
                }

                if (decodedPublicKey.Length == 0)
                {
                    Logger.Error($"{PublicKeyNotProvided}");
                    return ReturnResponse(false);
                }
                
                if (!Multibase.TryDecode(signature.ToStringUtf8(), out _, out var decodedSignature))
                {
                    Logger.Error($"{SignatureEncodingInvalid}");
                    return ReturnResponse(false);
                }
                
                if (decodedSignature.Length == 0)
                {
                    Logger.Error($"{SignatureNotProvided}");
                    return ReturnResponse(false);
                }
                
                var pubKey = _keySigner.CryptoContext.ImportPublicKey(decodedPublicKey);

                Guard.Argument(pubKey).HasValue();

                var result = _keySigner.CryptoContext.Verify(pubKey, decodedMessage.ToBytesForRLPEncoding(),
                    new Signature(decodedSignature));

                Logger.Debug("message content is {0}", deserialised.Message);
                
                return ReturnResponse(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"{FailedToHandleMessage} {messageDto}");
                throw;
            } 
        }

        private IMessage<VerifyMessageResponse> ReturnResponse(bool result)
        {
            return new VerifyMessageResponse
            {
                IsSignedByKey = result
            };
        }
    }
}