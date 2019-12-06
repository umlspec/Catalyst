﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Lib.P2P;
using MultiFormats;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class BlockApiTest
    {
        IDfs ipfs = TestFixture.Ipfs;
        string id = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rAQ";
        byte[] blob = Encoding.UTF8.GetBytes("blorb");

        [Fact]
        public void Put_Bytes()
        {
            var cid = ipfs.Block.PutAsync(blob).Result;
            Assert.Equal(id, (string) cid);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_TooBig()
        {
            var data = new byte[ipfs.Options.Block.MaxBlockSize + 1];
            ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var cid = ipfs.Block.PutAsync(data).Result;
            });
        }

        [Fact]
        public void Put_Bytes_ContentType()
        {
            var cid = ipfs.Block.PutAsync(blob, contentType: "raw").Result;
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string) cid);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_Inline_Cid()
        {
            try
            {
                ipfs.Options.Block.AllowInlineCid = true;
                var cid = ipfs.Block.PutAsync(blob, contentType: "raw").Result;
                Assert.True(cid.Hash.IsIdentityHash);
                Assert.Equal("bafkqablcnrxxeyq", (string) cid);

                var data = ipfs.Block.GetAsync(cid).Result;
                Assert.Equal(blob.Length, data.Size);
                Assert.Equal(blob, data.DataBytes);

                var content = new byte[ipfs.Options.Block.InlineCidLimit];
                cid = ipfs.Block.PutAsync(content, contentType: "raw").Result;
                Assert.True(cid.Hash.IsIdentityHash);

                content = new byte[ipfs.Options.Block.InlineCidLimit + 1];
                cid = ipfs.Block.PutAsync(content, contentType: "raw").Result;
                Assert.False(cid.Hash.IsIdentityHash);
            }
            finally
            {
                ipfs.Options.Block.AllowInlineCid = false;
            }
        }

        [Fact]
        public void Put_Bytes_Hash()
        {
            var cid = ipfs.Block.PutAsync(blob, "raw", "sha2-512").Result;
            Assert.Equal(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                (string) cid);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Bytes_Cid_Encoding()
        {
            var cid = ipfs.Block.PutAsync(blob,
                contentType: "raw",
                encoding: "base32").Result;
            Assert.Equal(1, cid.Version);
            Assert.Equal("base32", cid.Encoding);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream()
        {
            var cid = ipfs.Block.PutAsync(new MemoryStream(blob)).Result;
            Assert.Equal(id, (string) cid);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream_ContentType()
        {
            var cid = ipfs.Block.PutAsync(new MemoryStream(blob), contentType: "raw").Result;
            Assert.Equal("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string) cid);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Put_Stream_Hash()
        {
            var cid = ipfs.Block.PutAsync(new MemoryStream(blob), "raw", "sha2-512").Result;
            Assert.Equal(
                "bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e",
                (string) cid);

            var data = ipfs.Block.GetAsync(cid).Result;
            Assert.Equal(blob.Length, data.Size);
            Assert.Equal(blob, data.DataBytes);
        }

        [Fact]
        public void Get()
        {
            var _ = ipfs.Block.PutAsync(blob).Result;
            var block = ipfs.Block.GetAsync(id).Result;
            Assert.Equal(id, (string) block.Id);
            Assert.Equal(blob, block.DataBytes);
            var blob1 = new byte[blob.Length];
            block.DataStream.Read(blob1, 0, blob1.Length);
            Assert.Equal(blob, blob1);
        }

        [Fact]
        public void Stat()
        {
            var _ = ipfs.Block.PutAsync(blob).Result;
            var info = ipfs.Block.StatAsync(id).Result;
            Assert.Equal(id, (string) info.Id);
            Assert.Equal(5, info.Size);
        }

        [Fact]
        public async Task Stat_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(blob, "identity")
            };
            var info = await ipfs.Block.StatAsync(cid, cts.Token);
            Assert.Equal(cid.Encode(), (string) info.Id);
            Assert.Equal(5, info.Size);
        }

        [Fact]
        public async Task Stat_Unknown()
        {
            var cid = "QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF";
            var block = await ipfs.Block.StatAsync(cid);
            Assert.Null(block);
        }

        [Fact]
        public async Task Remove()
        {
            var _ = ipfs.Block.PutAsync(blob).Result;
            var cid = await ipfs.Block.RemoveAsync(id);
            Assert.Equal(id, (string) cid);
        }

        [Fact]
        public async Task Remove_Inline_CID()
        {
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(blob, "identity")
            };
            var removedCid = await ipfs.Block.RemoveAsync(cid);
            Assert.Equal(cid.Encode(), removedCid.Encode());
        }

        [Fact]
        public void Remove_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").Result;
            });
        }

        [Fact]
        public async Task Remove_Unknown_OK()
        {
            var cid = await ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", true);
            Assert.Equal(null, cid);
        }

        [Fact]
        public async Task Get_Inline_CID()
        {
            var cts = new CancellationTokenSource(300);
            var cid = new Cid
            {
                ContentType = "raw",
                Hash = MultiHash.ComputeHash(blob, "identity")
            };
            var block = await ipfs.Block.GetAsync(cid, cts.Token);
            Assert.Equal(cid.Encode(), block.Id.Encode());
            Assert.Equal(blob.Length, block.Size);
            Assert.Equal(blob, block.DataBytes);
        }

        [Fact]
        public async Task Put_Informs_Bitswap()
        {
            var data = Guid.NewGuid().ToByteArray();
            var cid = new Cid {Hash = MultiHash.ComputeHash(data)};
            var wantTask = ipfs.Bitswap.GetAsync(cid);

            var cid1 = await ipfs.Block.PutAsync(data);
            Assert.Equal(cid, cid1);
            Assert.True(wantTask.IsCompleted);
            Assert.Equal(cid, wantTask.Result.Id);
            Assert.Equal(data.Length, wantTask.Result.Size);
            Assert.Equal(data, wantTask.Result.DataBytes);
        }

        [Fact]
        public async Task Put_Informs_Dht()
        {
            var data = Guid.NewGuid().ToByteArray();
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var self = await ipfs.LocalPeer;
                var cid = await ipfs.Block.PutAsync(data);
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var peers = await ipfs.Dht.FindProvidersAsync(cid, limit: 1, cancel: cts.Token);
                Assert.Equal(self, peers.First());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }
    }
}
