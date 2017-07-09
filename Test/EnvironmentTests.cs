using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniMAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMAL.Test
{
    [TestClass()]
    public class EnvironmentTests
    {

        [TestMethod()]
        public void 環境拡張と検索のテスト()
        {
            var e1 = Environment<int>.Empty;
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("bar", e1));
            var e2 = Environment.Extend("bar", 1, e1);
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("bar", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e2));
            Assert.AreEqual(Environment.LookUp("bar", e2), 1);
            var e3 = Environment.Extend("foo", 2, e2);
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("bar", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e2));
            Assert.AreEqual(Environment.LookUp("bar", e2), 1);
            Assert.AreEqual(Environment.LookUp("foo", e3), 2);
            Assert.AreEqual(Environment.LookUp("bar", e3), 1);
            var e4 = Environment.Extend("bar", 3, e3);
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("bar", e1));
            TestHelper.GetException<Exception.NotBound>(() => Environment.LookUp("foo", e2));
            Assert.AreEqual(Environment.LookUp("bar", e2), 1);
            Assert.AreEqual(Environment.LookUp("foo", e3), 2);
            Assert.AreEqual(Environment.LookUp("bar", e3), 1);
            Assert.AreEqual(Environment.LookUp("foo", e4), 2);
            Assert.AreEqual(Environment.LookUp("bar", e4), 3);
        }

        [TestMethod()]
        public void 環境拡張と内包判定()
        {
            var e1 = Environment<int>.Empty;
            Assert.IsFalse(Environment.Contains("foo", e1));
            Assert.IsFalse(Environment.Contains("bar", e1));
            var e2 = Environment.Extend("bar", 1, e1);
            Assert.IsFalse(Environment.Contains("foo", e1));
            Assert.IsFalse(Environment.Contains("bar", e1));
            Assert.IsFalse(Environment.Contains("foo", e2));
            Assert.IsTrue(Environment.Contains("bar", e2));
            var e3 = Environment.Extend("foo", 2, e2);
            Assert.IsFalse(Environment.Contains("foo", e1));
            Assert.IsFalse(Environment.Contains("bar", e1));
            Assert.IsFalse(Environment.Contains("foo", e2));
            Assert.IsTrue(Environment.Contains("bar", e2));
            Assert.IsTrue(Environment.Contains("foo", e3));
            Assert.IsTrue(Environment.Contains("bar", e3));
            var e4 = Environment.Extend("bar", 3, e3);
            Assert.IsFalse(Environment.Contains("foo", e1));
            Assert.IsFalse(Environment.Contains("bar", e1));
            Assert.IsFalse(Environment.Contains("foo", e2));
            Assert.IsTrue(Environment.Contains("bar", e2));
            Assert.IsTrue(Environment.Contains("foo", e3));
            Assert.IsTrue(Environment.Contains("bar", e3));
            Assert.IsTrue(Environment.Contains("foo", e4));
            Assert.IsTrue(Environment.Contains("bar", e4));
        }

        [TestMethod()]
        public void Mapのテスト()
        {
            var e1 = Environment<int>.Empty;
            var e2 = Environment.Extend("bar", 1, e1);
            var e3 = Environment.Extend("foo", 2, e2);
            var e4 = Environment.Extend("bar", 3, e3);
            var e5 = Environment.Map((x) => x * 2, e4);

            Assert.AreEqual(Environment.LookUp("bar", e2), 1);
            Assert.AreEqual(Environment.LookUp("foo", e3), 2);
            Assert.AreEqual(Environment.LookUp("bar", e4), 3);
            Assert.AreEqual(Environment.LookUp("foo", e5), 4);
            Assert.AreEqual(Environment.LookUp("bar", e5), 6);
        }

        [TestMethod()]
        public void FoldLeftのテスト()
        {
            var e1 = Environment<int>.Empty;
            var e2 = Environment.Extend("e1", 1, e1);
            var e3 = Environment.Extend("e2", 2, e2);
            var e4 = Environment.Extend("e3", 3, e3);
            var e5 = Environment.Extend("e2", 4, e4);
            var e6 = Environment.Extend("e1", 5, e5);

            Assert.AreEqual(Environment.LookUp("e1", e6), 5);
            Assert.AreEqual(Environment.LookUp("e2", e6), 4);
            Assert.AreEqual(Environment.LookUp("e3", e6), 3);

            var ret = Environment.FoldLeft((s, x) => {s.Add(x); return s;} , e6, new List<int>());
            Assert.IsTrue(ret.SequenceEqual(new[] {5, 4, 3, 2, 1}));
        }

        [TestMethod()]
        public void FoldRightのテスト()
        {
            var e1 = Environment<int>.Empty;
            var e2 = Environment.Extend("e1", 1, e1);
            var e3 = Environment.Extend("e2", 2, e2);
            var e4 = Environment.Extend("e3", 3, e3);
            var e5 = Environment.Extend("e2", 4, e4);
            var e6 = Environment.Extend("e1", 5, e5);

            Assert.AreEqual(Environment.LookUp("e1", e6), 5);
            Assert.AreEqual(Environment.LookUp("e2", e6), 4);
            Assert.AreEqual(Environment.LookUp("e3", e6), 3);

            var ret = Environment.FoldRight((s, x) => { s.Add(x); return s; }, e6, new List<int>());
            Assert.IsTrue(ret.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
        }

        [TestMethod()]
        public void Keysのテスト()
        {
            var e1 = Environment<int>.Empty;
            var e2 = Environment.Extend("e1", 1, e1);
            var e3 = Environment.Extend("e2", 2, e2);
            var e4 = Environment.Extend("e3", 3, e3);
            var e5 = Environment.Extend("e1", 4, e4);
            var e6 = Environment.Extend("e2", 5, e5);

            var keys1 = Environment.Keys(e1);
            Assert.AreEqual(keys1, LinkedList<string>.Empty);
            var keys2 = Environment.Keys(e6);
            var ret1 = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, keys2, new List<string>());
            Assert.IsTrue(ret1.SequenceEqual(new[] { "e2", "e1", "e3", "e2", "e1" }));
        }

        [TestMethod()]
        public void Valuesのテスト()
        {
            var e1 = Environment<int>.Empty;
            var e2 = Environment.Extend("e1", 1, e1);
            var e3 = Environment.Extend("e2", 2, e2);
            var e4 = Environment.Extend("e3", 3, e3);
            var e5 = Environment.Extend("e1", 4, e4);
            var e6 = Environment.Extend("e2", 5, e5);

            var keys1 = Environment.Values(e1);
            Assert.AreEqual(keys1, LinkedList<int>.Empty);
            var keys2 = Environment.Values(e6);
            var ret = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, keys2, new List<int>());
            Assert.IsTrue(ret.SequenceEqual(new[] { 5,4,3,2,1 }));
        }
    }
}