using System;
using System.IO;

using JetBrains.Annotations;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using RaidRecord.Core.Systems;

namespace RaidRecord.Tests.Core.Systems;

[TestClass]
[TestSubject(typeof(RecordManager))]
public class RecordManagerTest
{
    /// <summary>
    /// 测试 IsSubFilePath 在当前平台下的通用行为。
    /// 所有路径均通过 <see cref="Path.Combine" /> 构造, 因此 Windows 与 Linux/macOS 上的语义一致。
    /// </summary>
    [TestMethod]
    public void TestFuncIsSubFilePath()
    {
        // 测试 IsSubFilePath
        // 1. 正常情况：子文件在父目录下
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "RaidRecordTest"));
        string parent = Path.Combine(root, "RecordDb");
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, Path.Combine(parent, "data.json")), "直接子文件应该返回 true");

        // 2. 深层子文件
        string deepSubFile = Path.Combine(parent, "SubFolder", "Nested", "data.json");
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, deepSubFile), "深层子文件应该返回 true");

        // 3. 不在父目录下
        string outsideFile = Path.Combine(root, "Other", "data.json");
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, outsideFile), "不在父目录下的文件应该返回 false");

        // 4. 路径包含相对路径 (..)
        string relativePathFile = Path.Combine(parent, "..", "Other", "data.json");
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, relativePathFile), "使用 .. 跳出目录应该返回 false");

        // 5. 路径包含相对路径 (在同一目录内)
        string sameDirRelative = Path.Combine(parent, ".", "data.json");
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, sameDirRelative), "使用 . 在同一目录内应该返回 true");

        // 6. 路径末尾带或不带路径分隔符
        string parentWithSlash = parent + Path.DirectorySeparatorChar;
        string subFileWithParentSlash = Path.Combine(parent, "data.json");
        Assert.IsTrue(RecordManager.IsSubFilePath(parentWithSlash, subFileWithParentSlash), "父目录带结尾分隔符应该正常工作");

        // 7. 父目录是子文件路径的前缀但实际不是子目录
        string similarFile = parent + "Backup" + Path.DirectorySeparatorChar + "data.json";
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, similarFile), "仅是前缀但不是子目录应该返回 false");

        // 8. 父目录和子文件完全相同（外部验证了文件不是目录）
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, parent), "文件路径不能是父目录本身");

        // 9. 空路径或 null
        Assert.IsFalse(RecordManager.IsSubFilePath("", Path.Combine(root, "data.json")), "父目录为空字符串应该返回 false");
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, ""), "子文件为空字符串应该返回 false");
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, null!), "子文件为 null 应该返回 false");

        // 10. 相对路径父目录
        string relativeParent = Path.Combine("RecordDbTest", "RecordDb");
        string absoluteSubFile = Path.Combine(Path.GetFullPath(relativeParent), "data.json");
        Assert.IsTrue(RecordManager.IsSubFilePath(relativeParent, absoluteSubFile), "相对路径父目录应该能正常工作");

        Console.WriteLine("所有测试通过！");
    }

    /// <summary>
    /// 测试仅 Windows 才成立的大小写与分隔符语义（Linux 区分大小写, 且反斜杠是合法文件名字符）。
    /// </summary>
    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void TestFuncIsSubFilePathWindowsOnly()
    {
        const string parent = @"C:\Test\RecordDb";

        // 1. 大小写测试（Windows 不区分大小写）
        // 注意: 大小写必须作用在同一目录上, 形如 C:\TEST\record\data.json 指向的是兄弟目录 record
        const string subFileMixed = @"C:\TEST\RECORDDB\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, subFileMixed), "大小写不同应该返回 true");

        // 2. 混合使用 / 与 \ 分隔符
        const string mixedSeparatorFile = @"C:\Test/RecordDb\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(parent, mixedSeparatorFile), "混合分隔符应该返回 true");

        // 3. 无效路径字符
        const string invalidPath = @"C:\Invalid|\Path\data.json";
        Assert.IsFalse(RecordManager.IsSubFilePath(parent, invalidPath), "无效路径应该返回 false");

        // 4. UNC 路径
        const string uncParent = @"\\Server\Share\RecordDb";
        const string uncSubFile = @"\\Server\Share\RecordDb\data.json";
        Assert.IsTrue(RecordManager.IsSubFilePath(uncParent, uncSubFile), "UNC 路径应该正常工作");
    }
}