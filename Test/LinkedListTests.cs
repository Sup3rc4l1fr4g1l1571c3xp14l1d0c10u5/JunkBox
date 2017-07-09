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
    public class LinkedListTests
    {
        [TestMethod()]
        public void Extendのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            Assert.AreEqual(l2.Value, 1);
            Assert.AreEqual(l2.Next, l1);
            Assert.AreEqual(l3.Value, 2);
            Assert.AreEqual(l3.Next, l2);
            Assert.AreEqual(l4.Value, 3);
            Assert.AreEqual(l4.Next, l3);
            Assert.AreEqual(l5.Value, 4);
            Assert.AreEqual(l5.Next, l4);
        }

        [TestMethod()]
        public void Firstのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            Assert.AreEqual(LinkedList.First((x) => (x % 2) == 0, l1), default(int));
            Assert.AreEqual(LinkedList.First((x) => (x % 2) == 0, l2), default(int));
            Assert.AreEqual(LinkedList.First((x) => (x % 2) == 0, l3),  2);
            Assert.AreEqual(LinkedList.First((x) => (x % 2) == 0, l4),  2);
            Assert.AreEqual(LinkedList.First((x) => (x % 2) == 0, l5),  4);

        }

        [TestMethod()]
        public void FirstIndexのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l1), -1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l2), 0);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l3), 1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l4), 2);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l5), 3);
        }

        [TestMethod()]
        public void Reverseのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            var l6 = LinkedList.Reverse(l5);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l6), 0);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 2, l6), 1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 3, l6), 2);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 4, l6), 3);

        }

        [TestMethod()]
        public void Mapのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            var l6 = LinkedList.Map(x => x*2, l5);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1 * 2, l6), 3);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 2 * 2, l6), 2);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 3 * 2, l6), 1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 4 * 2, l6), 0);

            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l5), 3);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 2, l5), 2);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 3, l5), 1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 4, l5), 0);
        }

        [TestMethod()]
        public void FoldLeftのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            var lst = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, l5, new List<int>());
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l5), 3);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 2, l5), 2);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 3, l5), 1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 4, l5), 0);
            Assert.IsTrue(lst.SequenceEqual(new [] {4,3,2,1}));
        }

        [TestMethod()]
        public void FoldRightのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            var lst = LinkedList.FoldRight((s, x) => { s.Add(x); return s; }, l5, new List<int>());
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 1, l5), 3);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 2, l5), 2);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 3, l5), 1);
            Assert.AreEqual(LinkedList.FirstIndex((x) => x == 4, l5), 0);
            Assert.IsTrue(lst.SequenceEqual(new[] { 1, 2, 3, 4 }));
        }

        [TestMethod()]
        public void Atのテスト()
        {
            var l1 = LinkedList<int>.Empty;
            var l2 = LinkedList.Extend(1, l1);
            var l3 = LinkedList.Extend(2, l2);
            var l4 = LinkedList.Extend(3, l3);
            var l5 = LinkedList.Extend(4, l4);

            Assert.AreEqual(LinkedList.At(3, l5), 1);
            Assert.AreEqual(LinkedList.At(2, l5), 2);
            Assert.AreEqual(LinkedList.At(1, l5), 3);
            Assert.AreEqual(LinkedList.At(0, l5), 4);
            TestHelper.GetException<ArgumentOutOfRangeException>(() => LinkedList.At(-1, l5));
            TestHelper.GetException<ArgumentOutOfRangeException>(() => LinkedList.At(4, l5));
            TestHelper.GetException<ArgumentOutOfRangeException>(() => LinkedList.At(5, l5));
        }

        [TestMethod()]
        public void Createのテスト()
        {
            var l1 = LinkedList.Create<int>();
            Assert.AreEqual(l1, LinkedList<int>.Empty);

            var l2 = LinkedList.Create(1);
            Assert.AreEqual(l2.Value, 1);
            Assert.AreEqual(l2.Next, LinkedList<int>.Empty);

            var l3 = LinkedList.Create(1, 2);
            Assert.AreEqual(l3.Value, 1);
            Assert.AreEqual(l3.Next.Value, 2);
            Assert.AreEqual(l3.Next.Next, LinkedList<int>.Empty);

            var l4 = LinkedList.Create(1, 2, 3);
            Assert.AreEqual(l4.Value, 1);
            Assert.AreEqual(l4.Next.Value, 2);
            Assert.AreEqual(l4.Next.Next.Value, 3);
            Assert.AreEqual(l4.Next.Next.Next, LinkedList<int>.Empty);
        }

        [TestMethod()]
        public void Concatのテスト()
        {
            var l1 = LinkedList.Create(1, 2, 3, 4, 5);
            var l2 = LinkedList.Create(6, 7, 8, 9, 10);
            var l3 = LinkedList.Concat(l1, l2);
            var lst1 = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, l3, new List<int>());
            Assert.IsTrue(lst1.SequenceEqual(Enumerable.Range(1, 10)));

            var l4 = LinkedList.Create<int>();
            var l5 = LinkedList.Create(1, 2, 3, 4, 5);
            var l6 = LinkedList.Concat(l4, l5);
            var lst2 = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, l6, new List<int>());
            Assert.IsTrue(lst2.SequenceEqual(Enumerable.Range(1, 5)));

            var l7 = LinkedList.Create(1, 2, 3, 4, 5);
            var l8 = LinkedList.Create<int>();
            var l9 = LinkedList.Concat(l7, l8);
            var lst3 = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, l9, new List<int>());
            Assert.IsTrue(lst3.SequenceEqual(Enumerable.Range(1, 5)));

            var l10 = LinkedList.Create<int>();
            var l11 = LinkedList.Create<int>();
            var l12 = LinkedList.Concat(l10, l11);
            var lst4 = LinkedList.FoldLeft((s, x) => { s.Add(x); return s; }, l12, new List<int>());
            Assert.IsTrue(lst4.SequenceEqual(Enumerable.Range(0, 0)));
        }
    }
}