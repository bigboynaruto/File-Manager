using NUnit.Framework;
using System;
using Gtk;
using System.Diagnostics;

namespace Tests
{
	[TestFixture]
	public class TreeElemUnitTest
	{
		[Test]
		public static void TestDirPath() {
			TreeElem te = new FileTreeElem(TreeIter.Zero, "/folder/folder2/file");
			Assert.AreEqual(te.DirPath, "/folder/folder2/");
		}

		[Test]
		public static void TestName() {
			TreeElem te = new FileTreeElem(TreeIter.Zero, "/folder/folder2/file");
			Assert.AreEqual(te.Name, "file");
		}
	}

	[TestFixture]
	public class MyTestEditorUnitTest
	{
		MyStringComparer msc;

		[TestFixtureSetUp] 
		public void Init() {
			msc = new MyStringComparer();
		}

		[Test]
		public void TestMyStringComparer1() {
			Assert.AreEqual(msc.Compare("123", "1234"), -1);
		}

		[Test]
		public void TestMyStringComparer2() {
			Assert.AreEqual(msc.Compare("12345", "1234"), 1);
		}

		[Test]
		public void TestMyStringComparer3() {
			Assert.AreEqual(msc.Compare("1234", "1234"), 0);
		}
	}

	[TestFixture]
	public class MyFileSystemUnitTest : MyFileSystem
	{
		[Test]
		public void TestToShortPath() {
			Assert.AreEqual(MyFileSystem.ToShortPath("/home/sakura/Downloads/folder/file.txt"), "/h/s/D/f/file.txt");
		}

		[Test]
		public void TestToFileSize() {
			Assert.AreEqual(MyFileSystem.ToFileSize(1028000), "1,004 KB");
		}

		[Test]
		public void TestThreeNonZeroDigits1() {
			Assert.AreEqual(MyFileSystem.ThreeNonZeroDigits(1000), "1,000");
		}

		[Test]
		public void TestThreeNonZeroDigits2() {
			Assert.AreEqual(MyFileSystem.ThreeNonZeroDigits(2.001), "2.00");
		}

		[Test]
		public void TestThreeNonZeroDigits3() {
			Assert.AreEqual(MyFileSystem.ThreeNonZeroDigits(10.1234), "10.1");
		}
	}
}

