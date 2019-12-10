﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.FileSystem;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class Pin
    {
        public Cid Id;
    }

    internal sealed class PinApi : IPinApi
    {
        private FileStore<Cid, Pin> _store;

        private readonly INameApi _nameApi;
        public IBlockApi BlockApi { get; set; }
        private readonly IObjectApi _objectApi;
        private readonly RepositoryOptions _repositoryOptions;

        public PinApi(INameApi nameApi,
            IObjectApi objectApi,
            RepositoryOptions options)
        {
            _nameApi = nameApi;
            _objectApi = objectApi;
            _repositoryOptions = options;
        }

        FileStore<Cid, Pin> Store
        {
            get
            {
                if (_store != null)
                {
                    return _store;
                }
                
                var folder = Path.Combine(_repositoryOptions.Folder, "pins");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                
                _store = new FileStore<Cid, Pin>
                {
                    Folder = folder,
                    NameToKey = cid => cid.Hash.ToBase32(),
                    KeyToName = key => new MultiHash(key.FromBase32())
                };

                return _store;
            }
        }

        public async Task<IEnumerable<Cid>> AddAsync(string path,
            bool recursive = true,
            CancellationToken cancel = default(CancellationToken))
        {
            var r = await _nameApi.ResolveAsync(path, cancel: cancel).ConfigureAwait(false);
            var id = Cid.Decode(r.Remove(0, 6));
            var todos = new Stack<Cid>();
            todos.Push(id);
            var dones = new List<Cid>();

            // The pin is added before the content is fetched, so that
            // garbage collection will not delete the newly pinned
            // content.

            while (todos.Count > 0)
            {
                var current = todos.Pop();

                // Add CID to PIN database.
                await Store.PutAsync(current, new Pin
                {
                    Id = current
                }, cancel).ConfigureAwait(false);

                // Make sure that the content is stored locally.
                await BlockApi.GetAsync(current, cancel).ConfigureAwait(false);

                // Recursively pin the links?
                if (recursive && current.ContentType == "dag-pb")
                {
                    var links = await _objectApi.LinksAsync(current, cancel);
                    foreach (var link in links)
                    {
                        todos.Push(link.Id);
                    }
                }

                dones.Add(current);
            }

            return dones;
        }

        public Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var cids = Store.Values.Select(pin => pin.Id);
            return Task.FromResult(cids);
        }

        public async Task<IEnumerable<Cid>> RemoveAsync(Cid id,
            bool recursive = true,
            CancellationToken cancel = default(CancellationToken))
        {
            var todos = new Stack<Cid>();
            todos.Push(id);
            var dones = new List<Cid>();

            while (todos.Count > 0)
            {
                var current = todos.Pop();
                await Store.RemoveAsync(current, cancel).ConfigureAwait(false);
                if (recursive)
                {
                    if (null != await BlockApi.StatAsync(current, cancel).ConfigureAwait(false))
                    {
                        try
                        {
                            var links = await _objectApi.LinksAsync(current, cancel).ConfigureAwait(false);
                            foreach (var link in links)
                            {
                                todos.Push(link.Id);
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }

                dones.Add(current);
            }

            return dones;
        }

        public async Task<bool> IsPinnedAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            return await Store.ExistsAsync(id, cancel).ConfigureAwait(false);
        }
    }
}
