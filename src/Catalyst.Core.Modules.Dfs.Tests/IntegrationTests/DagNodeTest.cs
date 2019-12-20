using System;
using System.IO;
using System.Linq;
using System.Text;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Hashing;
using Google.Protobuf;
using MultiFormats;
using MultiFormats.Registry;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests
{
    public class DagNodeTest
    {
        private readonly IHashProvider _hashProvider;

        public DagNodeTest()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("sha2-256"));
        }

        [Fact]
        public void EmptyDAG()
        {
            var node = new DagNode((byte[]) null, _hashProvider);
            Assert.Equal(0, node.DataBytes.Length);
            Assert.Equal(0, node.Links.Count());
            Assert.Equal(0, node.Size);
            Assert.Equal("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n", (string) node.Id);

            RoundtripTest(node);
        }

        [Fact]
        public void DataOnlyDAG()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc, _hashProvider);
            
            Assert.Equal(abc, node.DataBytes);
            Assert.Equal(0, node.Links.Count());
            Assert.Equal("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", (string) node.Id);
            Assert.Equal(5, node.Size);

            RoundtripTest(node);
        }

        [Fact]
        public void LinkOnlyDAG()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);
            var alink = anode.ToLink("a");

            var node = new DagNode(null, _hashProvider, new[]
            {
                alink
            });
            Assert.Equal(0, node.DataBytes.Length);
            Assert.Equal(1, node.Links.Count());
            Assert.Equal("QmVdMJFGTqF2ghySAmivGiQvsr9ZH7ujnNGBkLNNCe4HUE", (string) node.Id);
            Assert.Equal(43, node.Size);

            RoundtripTest(node);
        }

        [Fact]
        public void MultipleLinksOnlyDAG()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b, _hashProvider);
            var blink = bnode.ToLink("b");

            var node = new DagNode(null, _hashProvider, new[]
            {
                alink, blink
            });
            Assert.Equal(0, node.DataBytes.Length);
            Assert.Equal(2, node.Links.Count());
            Assert.Equal("QmbNgNPPykP4YTuAeSa3DsnBJWLVxccrqLUZDPNQfizGKs", (string) node.Id);

            RoundtripTest(node);
        }

        [Fact]
        public void MultipleLinksDataDAG()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b, _hashProvider);
            var blink = bnode.ToLink("b");

            var ab = Encoding.UTF8.GetBytes("ab");
            var node = new DagNode(ab, _hashProvider, new[]
            {
                alink, blink
            });
            
            Assert.Equal(ab, node.DataBytes);
            Assert.Equal(2, node.Links.Count());
            Assert.Equal("Qma5sYpEc9hSYdkuXpMDJYem95Mj7hbEd9C412dEQ4ZkfP", (string) node.Id);

            RoundtripTest(node);
        }

        [Fact]
        public void Links_are_Sorted()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);
            var alink = anode.ToLink("a");

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b, _hashProvider);
            var blink = bnode.ToLink("b");

            var ab = Encoding.UTF8.GetBytes("ab");
            var node = new DagNode(ab, _hashProvider, new[]
            {
                blink, alink
            });
            Assert.Equal(ab, node.DataBytes);
            Assert.Equal(2, node.Links.Count());
            Assert.Equal("Qma5sYpEc9hSYdkuXpMDJYem95Mj7hbEd9C412dEQ4ZkfP", (string) node.Id);
        }

        [Fact]
        public void Hashing_Algorithm()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc, _hashProvider, null);
            Assert.Equal(abc, node.DataBytes);
            Assert.Equal(0, node.Links.Count());
            Assert.Equal("QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V", (string) node.Id);
            Assert.Equal(5, node.Size);

            RoundtripTest(node);
        }

        [Fact]
        public void ToLink()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc, _hashProvider);
            var link = node.ToLink();
            Assert.Equal("", link.Name);
            Assert.Equal(node.Id, link.Id);
            Assert.Equal(node.Size, link.Size);
        }

        [Fact]
        public void ToLink_With_Name()
        {
            var abc = Encoding.UTF8.GetBytes("abc");
            var node = new DagNode(abc, _hashProvider);
            var link = node.ToLink("abc");
            Assert.Equal("abc", link.Name);
            Assert.Equal(node.Id, link.Id);
            Assert.Equal(node.Size, link.Size);
        }

        [Fact]
        public void AddLink()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b, _hashProvider);

            var cnode = bnode.AddLink(anode.ToLink());
            Assert.False(Object.ReferenceEquals(bnode, cnode));
            Assert.Equal(1, cnode.DataBytes.Length);
            Assert.Equal(1, cnode.Links.Count());
            Assert.Equal((string) anode.Id, (string) cnode.Links.First().Id);
            Assert.Equal(anode.Size, cnode.Links.First().Size);

            RoundtripTest(cnode);
        }

        [Fact]
        public void RemoveLink()
        {
            var a = Encoding.UTF8.GetBytes("a");
            var anode = new DagNode(a, _hashProvider);

            var b = Encoding.UTF8.GetBytes("b");
            var bnode = new DagNode(b, _hashProvider);

            var c = Encoding.UTF8.GetBytes("c");
            var cnode = new DagNode(c, _hashProvider, new[]
            {
                anode.ToLink(), bnode.ToLink()
            });

            var dnode = cnode.RemoveLink(anode.ToLink());
            Assert.False(Object.ReferenceEquals(dnode, cnode));
            Assert.Equal(1, dnode.DataBytes.Length);
            Assert.Equal(1, dnode.Links.Count());
            Assert.Equal((string) bnode.Id, (string) dnode.Links.First().Id);
            Assert.Equal(bnode.Size, dnode.Links.First().Size);

            RoundtripTest(cnode);
        }

        [Fact]
        public void Null_Stream()
        {
            TestUtils.ExceptionAssert.Throws(() => new DagNode((CodedInputStream) null, _hashProvider));
            TestUtils.ExceptionAssert.Throws(() => new DagNode((Stream) null, _hashProvider));
        }

        [Fact]
        public void Link_With_CID_V1()
        {
            var data =
                "124F0A4401551340309ECC489C12D6EB4CC40F50C902F2B4D0ED77EE511A7C7A9BCD3CA86D4CD86F989DD35BC5FF499670DA34255B45B0CFD830E81F605DCF7DC5542E93AE9CD76F120568656C6C6F180B0A020801"
                   .ToHexBuffer();
            var ms = new MemoryStream(data, false);
            var node = new DagNode(ms, _hashProvider);
            Assert.Equal("0801", node.DataBytes.ToHexString());
            Assert.Equal(1, node.Links.Count());
            var link = node.Links.First();
            Assert.Equal("hello", link.Name);
            Assert.Equal(1, link.Id.Version);
            Assert.Equal("raw", link.Id.ContentType);
            Assert.Equal("sha2-512", link.Id.Hash.Algorithm.Name);
            Assert.Equal(11, link.Size);
        }

        [Fact]
        public void Setting_Id()
        {
            var a = new DagNode((byte[]) null, _hashProvider);
            var b = new DagNode((byte[]) null, _hashProvider)
            {
                // Wrong hash but allowed.
                Id = "QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1m"
            };
            Assert.Equal(a.DataBytes.Length, b.DataBytes.Length);
            Assert.Equal(a.Links.Count(), b.Links.Count());
            Assert.Equal(a.Size, b.Size);
            Assert.NotEqual(a.Id, b.Id);

            RoundtripTest(b);
        }

        void RoundtripTest(IDagNode a)
        {
            var ms = new MemoryStream();
            a.Write(ms);
            ms.Position = 0;
            var b = new DagNode(ms, _hashProvider);
            
            Assert.Equal(a.DataBytes, b.DataBytes);
            Assert.Equal(a.ToArray(), b.ToArray());
            Assert.Equal(a.Links.Count(), b.Links.Count());
            a.Links.Zip(b.Links, (first, second) =>
            {
                Assert.Equal(first.Id, second.Id);
                Assert.Equal(first.Name, second.Name);
                Assert.Equal(first.Size, second.Size);
                return first;
            }).ToArray();

            using (var first = a.DataStream)
            using (var second = b.DataStream)
            {
                Assert.Equal(first.Length, second.Length);
                for (int i = 0; i < first.Length; ++i)
                {
                    Assert.Equal(first.ReadByte(), second.ReadByte());
                }
            }
        }
    }
}
