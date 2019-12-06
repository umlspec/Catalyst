﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Lib.P2P.Multiplex;
using Semver;

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///    A Stream Multiplexer protocol.
    /// </summary>
    /// <seealso href="https://github.com/libp2p/mplex"/>
    public class Mplex67 : IPeerProtocol
    {
        private static ILog log = LogManager.GetLogger(typeof(Mplex67));

        /// <inheritdoc />
        public string Name { get; } = "mplex";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(6, 7);

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default(CancellationToken))
        {
            log.Debug("start processing requests from " + connection.RemoteAddress);
            var muxer = new Muxer
            {
                Channel = stream,
                Connection = connection,
                Receiver = true
            };
            muxer.SubstreamCreated += (s, e) => _ = connection.ReadMessagesAsync(e, CancellationToken.None);

            // Attach muxer to the connection.  It now becomes the message reader.
            connection.MuxerEstablished.SetResult(muxer);
            await muxer.ProcessRequestsAsync().ConfigureAwait(false);

            log.Debug("stop processing from " + connection.RemoteAddress);
        }

        /// <inheritdoc />
        public Task ProcessResponseAsync(PeerConnection connection,
            CancellationToken cancel = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}