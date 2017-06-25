using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniMAL;

namespace MiniMALTest {
    [TestClass]
    public class SetTest {
        [TestMethod]
        public void ToStringメソッド() {
            var set1 = Set.from_list(LinkedList.Create(0, 1, 2, 3, 4));
            Assert.AreEqual(set1.ToString(), $"[{string.Join("; ", new[] { 0, 1, 2, 3, 4 }.Select(x => x.ToString()))}]");
            var set2 = Set.from_list(LinkedList.Create(100));
            Assert.AreEqual(set2.ToString(), $"[100]");
            Assert.AreEqual(Set<int>.Empty.ToString(), $"[]");
        }

        [TestMethod]
        public void Emptyメソッド() {
            var set = MiniMAL.Set<int>.Empty;
            Assert.AreEqual(set, MiniMAL.Set<int>.Empty);
            Assert.AreEqual(Set.Count(set), 0);
        }

        [TestMethod]
        public void Singletonメソッド() {
            var set = Set.singleton(1);
            Assert.AreNotEqual(set, MiniMAL.Set<int>.Empty);
            Assert.AreEqual(set.Value, 1);
            Assert.AreEqual(set.Next, MiniMAL.Set<int>.Empty);
            Assert.AreEqual(Set.Count(set), 1);
        }

        [TestMethod]
        public void FromListメソッド() {
            var set = Set.from_list(LinkedList.Create(0, 1, 2, 3, 4));
            Assert.AreNotEqual(set, MiniMAL.Set<int>.Empty);
            for (var i = 0; i < 5; i++) {
                Assert.IsTrue(Set.member(i, set));
            }
            Assert.AreEqual(Set.Count(set), 5);
        }

        [TestMethod]
        public void 要素追加テスト() {
            var firstset = MiniMAL.Set<int>.Empty;
            var set = firstset;
            for (var i = 0; i < 100; i++) {
                set = Set.insert(i, set);
            }
            Assert.AreEqual(Set.Count(firstset), 0);
            Assert.AreEqual(Set.Count(set), 100);
            Assert.AreNotEqual(set, firstset);
            Assert.AreEqual(firstset, MiniMAL.Set<int>.Empty);
            Assert.AreNotEqual(set, MiniMAL.Set<int>.Empty);
            for (var i = 0; i < 100; i++) {
                Assert.IsFalse(Set.member(i, firstset));
                Assert.IsTrue(Set.member(i, set));
            }
            var secondset = set;
            for (var i = 100; i < 200; i++) {
                set = Set.insert(i, set);
            }
            Assert.AreEqual(Set.Count(firstset), 0);
            Assert.AreEqual(Set.Count(secondset), 100);
            Assert.AreEqual(Set.Count(set), 200);
            for (var i = 0; i < 100; i++) {
                Assert.IsFalse(Set.member(i, firstset));
                Assert.IsTrue(Set.member(i, secondset));
                Assert.IsTrue(Set.member(i, set));
            }
            for (var i = 100; i < 200; i++) {
                Assert.IsFalse(Set.member(i, firstset));
                Assert.IsFalse(Set.member(i, secondset));
                Assert.IsTrue(Set.member(i, set));
            }
        }

        [TestMethod]
        public void 重複する要素の追加テスト() {
            var firstset = MiniMAL.Set<int>.Empty;
            var set = firstset;
            for (var i = 0; i < 100; i++) {
                set = Set.insert(i, set);
            }
            Assert.AreEqual(Set.Count(set), 100);
            for (var i = 0; i < 100; i++) {
                Assert.IsTrue(Set.member(i, set));
            }
            for (var i = 0; i < 100; i++) {
                set = Set.insert(i, set);
            }
            Assert.AreEqual(Set.Count(set), 100);
            for (var i = 0; i < 100; i++) {
                Assert.IsTrue(Set.member(i, set));
            }
        }

        [TestMethod]
        public void Unionのテスト() {
            var set1 = Set.from_list(LinkedList.Create(Enumerable.Range(0, 100).ToArray()));
            var set2 = Set.from_list(LinkedList.Create(Enumerable.Range(50, 100).ToArray()));
            var uni1 = Set.union(set1, set2);
            Assert.AreEqual(Set.Count(set1), 100);
            for (var i = 0; i < 100; i++) {
                Assert.IsTrue(Set.member(i, set1));
            }
            Assert.AreEqual(Set.Count(set2), 100);
            for (var i = 50; i < 150; i++) {
                Assert.IsTrue(Set.member(i, set2));
            }
            Assert.AreEqual(Set.Count(uni1), 150);
            for (var i = 0; i < 150; i++) {
                Assert.IsTrue(Set.member(i, uni1));
            }
        }

        [TestMethod]
        public void Removeのテスト() {
            var set1 = Set.from_list(LinkedList.Create(Enumerable.Range(0, 100).ToArray()));
            Assert.AreEqual(Set.Count(set1), 100);
            var set2 = set1;
            for (var i = 0; i < 50; i++) {
                Assert.AreEqual(Set.Count(set1), 100);
                var set3 = Set.remove(i * 2, set2);
                Assert.AreEqual(Set.Count(set2), 100 - (i));
                Assert.AreEqual(Set.Count(set3), 100 - (i + 1));
                var diff = Set.diff(set2, set3);
                Assert.AreEqual(Set.Count(diff), 1);
                Assert.IsTrue(Set.member(i * 2, diff));

                set2 = set3;
            }
        }

        [TestMethod]
        public void Foldのテスト() {
            var set1 = Set.from_list(LinkedList.Create(Enumerable.Range(0, 100).ToArray()));
            Assert.AreEqual(Set.Count(set1), 100);
            var ret = Set.Fold((s, x) => { s.Add(x * 2); return s; }, new HashSet<int>(), set1);
            Assert.IsTrue(ret.SetEquals(Enumerable.Range(0, 100).Select(x => x * 2).ToArray()));
        }

        [TestMethod]
        public void Mapのテスト() {
            var set1 = Set.from_list(LinkedList.Create(Enumerable.Range(0, 100).ToArray()));
            Assert.AreEqual(Set.Count(set1), 100);
            var set2 = Set.map(x => x * 3, set1);
            Assert.AreEqual(Set.Count(set2), 100);
            for (var i = 0; i < 100; i++) {
                Assert.IsTrue(Set.member(i, set1));
                Assert.IsTrue(Set.member(i * 3, set2));
            }
        }
    }
}
